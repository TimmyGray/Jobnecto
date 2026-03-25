#!/usr/bin/env bash

set -u
set -o pipefail

BASE_REF="${1:-origin/master}"
SOLUTION_PATH="backend/JobNecto.slnx"
REPORT_DIR="${2:-/tmp}"

if [ ! -f "${SOLUTION_PATH}" ]; then
  echo "Expected solution not found at ${SOLUTION_PATH}. Run from repository root." >&2
  exit 2
fi

mkdir -p "${REPORT_DIR}"
TIMESTAMP="$(date +%Y%m%d_%H%M%S)"
REPORT_PATH="${REPORT_DIR}/code_review_report_${TIMESTAMP}.md"

CHECK_FAILURES=0
BUILD_FAILED=0
TEST_FAILED=0
RELEASE_BUILD_FAILED=0
RELEASE_TEST_FAILED=0

run_check() {
  local title="$1"
  shift
  local output_file
  output_file="$(mktemp)"
  local status="PASS"

  if "$@" >"${output_file}" 2>&1; then
    status="PASS"
  else
    status="FAIL"
    CHECK_FAILURES=$((CHECK_FAILURES + 1))
    case "${title}" in
      "Build (Debug)") BUILD_FAILED=1 ;;
      "Test (Debug)") TEST_FAILED=1 ;;
      "Build (Release, warnaserror)") RELEASE_BUILD_FAILED=1 ;;
      "Test (Release, no-build, warnaserror)") RELEASE_TEST_FAILED=1 ;;
    esac
  fi

  {
    echo "### ${title}: ${status}"
    echo
    echo '```'
    sed -n '1,200p' "${output_file}"
    echo '```'
    echo
  } >> "${REPORT_PATH}"

  rm -f "${output_file}"
}

if git rev-parse --verify "${BASE_REF}" >/dev/null 2>&1; then
  DIFF_RANGE="${BASE_REF}...HEAD"
else
  DIFF_RANGE="$(git merge-base origin/master HEAD)...HEAD"
fi

CHANGED_FILES="$(git diff --name-only "${DIFF_RANGE}")"
CHANGED_COUNT="$(printf "%s\n" "${CHANGED_FILES}" | awk 'NF { c++ } END { print c + 0 }')"
SOURCE_COUNT="$(printf "%s\n" "${CHANGED_FILES}" | awk '/^backend\/src\// { c++ } END { print c + 0 }')"
TEST_COUNT="$(printf "%s\n" "${CHANGED_FILES}" | awk '/^backend\/tests\// { c++ } END { print c + 0 }')"

{
  echo "# Code Reviewer Report"
  echo
  echo "- Generated: $(date -u '+%Y-%m-%dT%H:%M:%SZ')"
  echo "- Branch: $(git branch --show-current)"
  echo "- Diff range: ${DIFF_RANGE}"
  echo
  echo "## Changed files"
  echo
  if [ -n "${CHANGED_FILES}" ]; then
    printf "%s\n" "${CHANGED_FILES}" | sed 's/^/- /'
  else
    echo "- No changed files detected."
  fi
  echo
  echo "## Risk notes"
  echo
  echo "- Heuristic risk assessment is computed after checks."
  echo
  echo "## Checks"
  echo
} > "${REPORT_PATH}"

run_check "Build (Debug)" dotnet build "${SOLUTION_PATH}"
run_check "Test (Debug)" dotnet test "${SOLUTION_PATH}"
run_check "Build (Release, warnaserror)" dotnet build "${SOLUTION_PATH}" --configuration Release --warnaserror
run_check "Test (Release, no-build, warnaserror)" dotnet test "${SOLUTION_PATH}" --configuration Release --no-build --warnaserror

RISK_SCORE=1
RISK_NOTES=()

if [ "${CHANGED_COUNT}" -gt 50 ]; then
  RISK_SCORE=$((RISK_SCORE + 3))
  RISK_NOTES+=("Large diff (${CHANGED_COUNT} files changed).")
elif [ "${CHANGED_COUNT}" -gt 20 ]; then
  RISK_SCORE=$((RISK_SCORE + 2))
  RISK_NOTES+=("Medium-large diff (${CHANGED_COUNT} files changed).")
elif [ "${CHANGED_COUNT}" -gt 10 ]; then
  RISK_SCORE=$((RISK_SCORE + 1))
  RISK_NOTES+=("Moderate diff size (${CHANGED_COUNT} files changed).")
fi

if printf "%s\n" "${CHANGED_FILES}" | rg 'backend/src/JobNecto\.(Domain|Infrastructure|API)/' >/dev/null 2>&1; then
  RISK_SCORE=$((RISK_SCORE + 2))
  RISK_NOTES+=("Core API/Domain/Infrastructure areas changed.")
fi

if [ "${SOURCE_COUNT}" -gt 0 ] && [ "${TEST_COUNT}" -eq 0 ]; then
  RISK_SCORE=$((RISK_SCORE + 2))
  RISK_NOTES+=("Source files changed without test file updates.")
fi

if [ "${BUILD_FAILED}" -eq 1 ]; then
  RISK_SCORE=$((RISK_SCORE + 3))
  RISK_NOTES+=("Debug build failed.")
fi
if [ "${TEST_FAILED}" -eq 1 ]; then
  RISK_SCORE=$((RISK_SCORE + 3))
  RISK_NOTES+=("Debug test run failed.")
fi
if [ "${RELEASE_BUILD_FAILED}" -eq 1 ]; then
  RISK_SCORE=$((RISK_SCORE + 2))
  RISK_NOTES+=("Release build with warnings-as-errors failed.")
fi
if [ "${RELEASE_TEST_FAILED}" -eq 1 ]; then
  RISK_SCORE=$((RISK_SCORE + 2))
  RISK_NOTES+=("Release no-build test run failed.")
fi

if [ "${RISK_SCORE}" -gt 10 ]; then
  RISK_SCORE=10
fi

{
  echo "## Heuristic risk assessment"
  echo
  echo "- risk_score: ${RISK_SCORE}/10"
  if [ "${#RISK_NOTES[@]}" -gt 0 ]; then
    for note in "${RISK_NOTES[@]}"; do
      echo "- ${note}"
    done
  else
    echo "- No additional heuristic risk triggers."
  fi
  echo
} >> "${REPORT_PATH}"

echo "Code reviewer report generated: ${REPORT_PATH}"

if [ "${CHECK_FAILURES}" -gt 0 ]; then
  echo "One or more checks failed. See report for details."
  exit 1
fi

echo "All checks passed."
exit 0
