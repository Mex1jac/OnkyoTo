import { useState } from 'react'
import { discover, powerOn, volumeUp, volumeDown } from './api'
import './App.css'

function App() {
  const [ipAddress, setIpAddress] = useState('')
  const [status, setStatus] = useState('')
  const [busy, setBusy] = useState(false)

  // Runs an async action while showing status and preventing double clicks.
  async function run(label: string, action: () => Promise<void>) {
    setBusy(true)
    setStatus(`${label}...`)
    try {
      await action()
      setStatus(`${label}: done`)
    } catch (error) {
      setStatus(`${label}: ${(error as Error).message}`)
    } finally {
      setBusy(false)
    }
  }

  function handleDiscover() {
    run('Discovering device', async () => {
      const ip = await discover()
      setIpAddress(ip)
    })
  }

  const hasDevice = ipAddress.trim().length > 0

  return (
    <main className="container">
      <h1>OnkyoIn</h1>
      <p className="subtitle">Control your Onkyo receiver</p>

      <section className="panel">
        <label htmlFor="ip">Device IP</label>
        <input
          id="ip"
          type="text"
          placeholder="192.168.1.100"
          value={ipAddress}
          onChange={(event) => setIpAddress(event.target.value)}
        />
        <button onClick={handleDiscover} disabled={busy}>
          Discover device
        </button>
      </section>

      <section className="panel controls">
        <button onClick={() => run('Power on', () => powerOn(ipAddress))} disabled={busy || !hasDevice}>
          Power On
        </button>
        <button onClick={() => run('Volume up', () => volumeUp(ipAddress))} disabled={busy || !hasDevice}>
          Volume +
        </button>
        <button onClick={() => run('Volume down', () => volumeDown(ipAddress))} disabled={busy || !hasDevice}>
          Volume −
        </button>
      </section>

      {status && <p className="status">{status}</p>}
    </main>
  )
}

export default App
