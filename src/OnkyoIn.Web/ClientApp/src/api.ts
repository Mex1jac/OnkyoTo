/**
 * Client for the OnkyoIn.Web HTTP API.
 * In development Vite proxies /api to the backend (see vite.config.ts).
 */

export interface DiscoverResponse {
  ipAddress: string
}

async function post(path: string, ipAddress: string): Promise<void> {
  const response = await fetch(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ ipAddress }),
  })

  if (!response.ok) {
    throw new Error(`Request failed: ${response.status} ${response.statusText}`)
  }
}

export async function discover(): Promise<string> {
  const response = await fetch('/api/device/discover')
  if (!response.ok) {
    throw new Error(`Discovery failed: ${response.status} ${response.statusText}`)
  }
  const data = (await response.json()) as DiscoverResponse
  return data.ipAddress
}

export const powerOn = (ipAddress: string) => post('/api/device/power-on', ipAddress)
export const volumeUp = (ipAddress: string) => post('/api/device/volume/up', ipAddress)
export const volumeDown = (ipAddress: string) => post('/api/device/volume/down', ipAddress)
