import { useEffect, useState, type ChangeEvent } from 'react'
import {
  discover,
  getState,
  getInputs,
  getTuner,
  powerToggle,
  setInput,
  setTunerPreset,
  tunerPresetDown,
  tunerPresetUp,
  volumeDown,
  volumeUp,
  type DeviceState,
  type InputSource,
  type TunerState,
} from './api'
import './App.css'

type MessageKind = 'info' | 'success' | 'error'

interface Feedback {
  kind: MessageKind
  text: string
}

function App() {
  const [ipAddress, setIpAddress] = useState(
    () => new URLSearchParams(window.location.search).get('ip') ?? '',
  )
  const [state, setState] = useState<DeviceState | null>(null)
  const [tuner, setTuner] = useState<TunerState | null>(null)
  const [sources, setSources] = useState<InputSource[]>([])
  const [feedback, setFeedback] = useState<Feedback | null>(null)
  const [busy, setBusy] = useState(false)

  // Keep the device IP in the URL so the state is deep-linkable and survives reloads.
  useEffect(() => {
    const url = new URL(window.location.href)
    if (ipAddress) {
      url.searchParams.set('ip', ipAddress)
    } else {
      url.searchParams.delete('ip')
    }
    window.history.replaceState(null, '', url)
  }, [ipAddress])

  // Runs an async action while showing status, and reports success or failure.
  async function run(label: string, action: () => Promise<void>) {
    setBusy(true)
    setFeedback({ kind: 'info', text: `${label}…` })
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

  // Re-reads the device state without showing user-facing feedback.
  async function refreshState() {
    setState(await getState(ipAddress))
  }

  function handlePowerToggle() {
    run(isPoweredOn ? 'Powering off' : 'Powering on', async () => {
      setState(await powerToggle(ipAddress))
    })
  }

  // Load the device state and the available sources whenever a device IP is set
  // (also from the URL), so the panels are populated without a manual refresh.
  useEffect(() => {
    if (!ipAddress.trim()) {
      setSources([])
      setState(null)
      return
    }

    let cancelled = false
    getState(ipAddress)
      .then((deviceState) => {
        if (!cancelled) setState(deviceState)
      })
      .catch(() => {
        // The device may be unreachable; keep the previous state.
      })

    getTuner(ipAddress)
      .then((deviceTuner) => {
        if (!cancelled) setTuner(deviceTuner)
      })
      .catch(() => {
        // The tuner state is optional; keep the previous one.
      })

    getInputs(ipAddress)
      .then((list) => {
        if (!cancelled) setSources(list)
      })
      .catch(() => {
        if (!cancelled) setSources([])
      })

    return () => {
      cancelled = true
    }
  }, [ipAddress])

  function handleSelectSource(event: ChangeEvent<HTMLSelectElement>) {
    const inputId = event.target.value
    run('Switching source', async () => {
      await setInput(ipAddress, inputId)
      await refreshState()
      setTuner(await getTuner(ipAddress))
      setSources(await getInputs(ipAddress))
    })
  }

  const hasDevice = ipAddress.trim().length > 0
  const isPoweredOn = state?.isPoweredOn ?? false
  const powerText = state ? (isPoweredOn ? 'On' : 'Off') : '—'
  const powerClass = state ? (isPoweredOn ? 'on' : 'off') : ''
  const tunerBand =
    state?.currentInputId === 'am' || state?.currentInputId === 'fm'
      ? state.currentInputId
      : null
  const currentInputId =
    sources.find((source) => source.isCurrent)?.id ?? state?.currentInputId ?? ''

  return (
    <main className="container">
      <h1>OnkyoIn</h1>
      <p className="subtitle">Control your Onkyo receiver</p>

      <section className="panel state" aria-label="Device state">
        <div className="state-controls">
          <button onClick={handleDiscover} disabled={busy}>
            Discover Device
          </button>
        </div>
        <div className="state-values">
          <div className="state-item">
            <span className="state-label">Power</span>
            <span className={`state-value ${powerClass}`}>{powerText}</span>
          </div>
          <div className="state-item">
            <span className="state-label">Volume</span>
            <span className="state-value">{state?.volume ?? '—'}</span>
          </div>
          <div className="state-item">
            <span className="state-label">Source</span>
            <span className="state-value">
              {state?.currentInputName ?? '—'}
            </span>
          </div>
          <div className="state-item">
            <span className="state-label">IP</span>
            <span className="state-value">{ipAddress || '—'}</span>
          </div>
        </div>
      </section>

      {tunerBand && (
        <section className="panel tuner" aria-label="Tuner presets">
          <label htmlFor="preset">
            Tuner preset ({tunerBand === 'am' ? 'AM' : 'FM'})
          </label>
          <div className="tuner-controls">
            <button
              onClick={() =>
                run('Preset down', async () => {
                  setTuner(await tunerPresetDown(ipAddress))
                })
              }
              disabled={busy}
              aria-label="Previous preset"
            >
              ◀
            </button>
            <select
              id="preset"
              value={tuner?.preset ?? ''}
              disabled={busy}
              onChange={(event) => {
                const preset = Number(event.target.value)
                run('Switching preset', async () => {
                  setTuner(await setTunerPreset(ipAddress, preset))
                })
              }}
            >
              {Array.from({ length: 40 }, (_, index) => index + 1).map((preset) => (
                <option key={preset} value={preset}>
                  {preset}
                </option>
              ))}
            </select>
            <button
              onClick={() =>
                run('Preset up', async () => {
                  setTuner(await tunerPresetUp(ipAddress))
                })
              }
              disabled={busy}
              aria-label="Next preset"
            >
              ▶
            </button>
          </div>
        </section>
      )}

      <section className="panel">
        <label htmlFor="source">Source</label>
        <select
          id="source"
          value={currentInputId}
          disabled={busy || !hasDevice || sources.length === 0}
          onChange={handleSelectSource}
        >
          {sources.length === 0 && <option value="">No sources available</option>}
          {sources.map((source) => (
            <option key={source.id} value={source.id}>
              {source.name}
            </option>
          ))}
        </select>
      </section>

      <section className="panel controls">
        <button onClick={handlePowerToggle} disabled={busy || !hasDevice}>
          {isPoweredOn ? 'Power Off' : 'Power On'}
        </button>
        <button
          onClick={() =>
            run('Volume up', async () => {
              await volumeUp(ipAddress)
              await refreshState()
            })
          }
          disabled={busy || !hasDevice}
        >
          Volume +
        </button>
        <button
          onClick={() =>
            run('Volume down', async () => {
              await volumeDown(ipAddress)
              await refreshState()
            })
          }
          disabled={busy || !hasDevice}
        >
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
