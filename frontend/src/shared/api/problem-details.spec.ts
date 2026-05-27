import { describe, expect, it } from 'vitest';
import { normalizeProblemDetails } from './problem-details';

describe('normalizeProblemDetails', () => {
  it('normalizes a 400 body with a field errors map', () => {
    const body = {
      type: 'https://httpstatuses.io/400',
      title: 'Validation failed',
      status: 400,
      detail: 'One or more validation errors occurred.',
      instance: '/api/v1/users',
      errors: {
        loginName: ['loginName must be between 3 and 50 characters.'],
        email: ['email must be a valid email address.'],
      },
      extensions: { traceId: '00-abc-01' },
    };

    const result = normalizeProblemDetails(body, 400);

    expect(result.status).toBe(400);
    expect(result.title).toBe('Validation failed');
    expect(result.detail).toBe('One or more validation errors occurred.');
    expect(result.errors).toEqual({
      loginName: ['loginName must be between 3 and 50 characters.'],
      email: ['email must be a valid email address.'],
    });
    expect(result.traceId).toBe('00-abc-01');
  });

  it('normalizes a 409 conflict body without an errors map', () => {
    const body = {
      title: 'Conflict',
      status: 409,
      detail: 'Email already registered.',
    };

    const result = normalizeProblemDetails(body, 409);

    expect(result.status).toBe(409);
    expect(result.title).toBe('Conflict');
    expect(result.errors).toBeUndefined();
  });

  it('normalizes a 500 with a non-problem-json string body to a generic problem', () => {
    const result = normalizeProblemDetails('Internal Server Error', 500);

    expect(result.status).toBe(500);
    expect(result.title).toBe('Internal Server Error');
    expect(result.errors).toBeUndefined();
  });

  it('falls back to a generic title when the body is null', () => {
    const result = normalizeProblemDetails(null, 503);

    expect(result.status).toBe(503);
    expect(result.title).toBe('Something went wrong');
  });

  it('lifts traceId and code from extensions when not at top level', () => {
    const body = {
      title: 'Generation timed out',
      status: 504,
      extensions: { traceId: 'trace-xyz', code: 'generation_timeout' },
    };

    const result = normalizeProblemDetails(body, 504);

    expect(result.traceId).toBe('trace-xyz');
    expect(result.code).toBe('generation_timeout');
  });

  it('coerces a single-string errors value into a one-element array', () => {
    const body = {
      title: 'Validation failed',
      status: 400,
      errors: { password: 'password must be at least 8 characters long.' },
    };

    const result = normalizeProblemDetails(body, 400);

    expect(result.errors).toEqual({
      password: ['password must be at least 8 characters long.'],
    });
  });

  it('uses status 0 when the request never reached the server (status NaN)', () => {
    const result = normalizeProblemDetails(null, Number.NaN);

    expect(result.status).toBe(0);
    expect(result.title).toBe('Something went wrong');
  });
});
