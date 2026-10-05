import { useState } from 'react'
import { discover, powerOn, volumeUp, volumeDown } from './api'
import './App.css'

type MessageKind = 'info' | 'success' | 'error'

interface Feedback {
  kind: MessageKind
  text: string
}

function App() {
  const [ipAddress, setIpAddress] = useState('')
  const [feedback, setFeedback] = useState<Feedback | null>(null)
  const [busy, setBusy] = useState(false)

  // Runs an async action while showing status, and reports success or failure.
  async function run(label: string, action: () => Promise<void>) {
    setBusy(true)
    setFeedback({ kind: 'info', text: `${label}...` })
    try {
      await action()
      setFeedback({ kind: 'success', text: `${label}: done` })
    } catch (error) {
      setFeedback({ kind: 'error', text: (error as Error).message })
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

      {feedback && (
        <p className={`status ${feedback.kind}`} role={feedback.kind === 'error' ? 'alert' : 'status'}>
          {feedback.text}
        </p>
      )}
    </main>
  )
}

export default App
