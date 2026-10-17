/**
 * Client for the OnkyoIn.Web HTTP API.
 * In development Vite proxies /api to the backend (see vite.config.ts).
 */

export interface DiscoverResponse {
  ipAddress: string
}

export interface DeviceState {
  ipAddress: string
  isPoweredOn: boolean
  volume: number
  currentInputId?: string
  currentInputName?: string
}

/** An input source, with a flag for the currently selected one. */
export interface InputSource {
  id: string
  name: string
  isCurrent: boolean
}

/** Tuner state: the active band ("am"/"fm") and the current preset. */
export interface TunerState {
  band: string | null
  preset: number | null
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

export async function getState(ipAddress: string): Promise<DeviceState> {
  const response = await request(`/api/device/state?ipAddress=${encodeURIComponent(ipAddress)}`)
  return (await response.json()) as DeviceState
}

export async function getInputs(ipAddress: string): Promise<InputSource[]> {
  const response = await request(`/api/device/inputs?ipAddress=${encodeURIComponent(ipAddress)}`)
  return (await response.json()) as InputSource[]
}

function post(path: string, body: unknown): Promise<Response> {
  return request(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

export async function powerToggle(ipAddress: string): Promise<DeviceState> {
  const response = await post('/api/device/power/toggle', { ipAddress })
  return (await response.json()) as DeviceState
}

export const powerOn = (ipAddress: string) => post('/api/device/power-on', { ipAddress }).then(() => undefined)
export const volumeUp = (ipAddress: string) => post('/api/device/volume/up', { ipAddress }).then(() => undefined)
export const volumeDown = (ipAddress: string) => post('/api/device/volume/down', { ipAddress }).then(() => undefined)
export const setInput = (ipAddress: string, inputId: string) =>
  post('/api/device/input', { ipAddress, inputId }).then(() => undefined)

export async function getTuner(ipAddress: string): Promise<TunerState | null> {
  const response = await request(`/api/device/tuner?ipAddress=${encodeURIComponent(ipAddress)}`)
  // 204 No Content: the current input is not AM/FM, so there is no tuner state.
  if (response.status === 204) return null
  return (await response.json()) as TunerState | null
}

export async function setTunerPreset(ipAddress: string, preset: number): Promise<TunerState> {
  const response = await post('/api/device/tuner/preset', { ipAddress, preset })
  return (await response.json()) as TunerState
}

export async function tunerPresetUp(ipAddress: string): Promise<TunerState> {
  const response = await post('/api/device/tuner/preset/up', { ipAddress })
  return (await response.json()) as TunerState
}

export async function tunerPresetDown(ipAddress: string): Promise<TunerState> {
  const response = await post('/api/device/tuner/preset/down', { ipAddress })
  return (await response.json()) as TunerState
}
