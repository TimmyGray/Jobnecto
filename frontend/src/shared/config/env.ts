/**
 * Runtime/static environment configuration for the client.
 *
 * The API base URL defaults to the local dev backend host
 * (http://localhost:5000/api/v1) per the FE Guide §6 and the live backend
 * dev host documented in the story. Override per environment as the app grows.
 */
export const env = {
  /** Base URL for all API calls. The HTTP interceptor prefixes relative `/...` paths with this. */
  apiBaseUrl: 'http://localhost:5000/api/v1',
} as const;

export type Env = typeof env;
