/**
 * Client for the OnkyoIn.Web HTTP API.
 * In development Vite proxies /api to the backend (see vite.config.ts).
 */

export interface DiscoverResponse {
  ipAddress: string
}

/** Shape of an ASP.NET Core ProblemDetails error response. */
interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
}

/** Error carrying a user-friendly message and the HTTP status code. */
export class ApiError extends Error {
  readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

// Turns a failed response into an ApiError, preferring ProblemDetails.detail.
async function toApiError(response: Response, fallback: string): Promise<ApiError> {
  let message = fallback
  try {
    const problem = (await response.json()) as ProblemDetails
    message = problem.detail ?? problem.title ?? fallback
  } catch {
    // Body was not JSON; keep the fallback message.
  }
  return new ApiError(message, response.status)
}

async function request(path: string, init?: RequestInit): Promise<Response> {
  let response: Response
  try {
    response = await fetch(path, init)
  } catch {
    throw new ApiError('Could not reach the server. Check your connection.', 0)
  }

  if (!response.ok) {
    throw await toApiError(response, `Request failed (${response.status}).`)
  }

  return response
}

export async function discover(): Promise<string> {
  const response = await request('/api/device/discover')
  const data = (await response.json()) as DiscoverResponse
  return data.ipAddress
}

function post(path: string, ipAddress: string): Promise<Response> {
  return request(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ ipAddress }),
  })
}

export const powerOn = (ipAddress: string) => post('/api/device/power-on', ipAddress).then(() => undefined)
export const volumeUp = (ipAddress: string) => post('/api/device/volume/up', ipAddress).then(() => undefined)
export const volumeDown = (ipAddress: string) => post('/api/device/volume/down', ipAddress).then(() => undefined)
