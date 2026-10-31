import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { installFakeMedia } from '../../test/fakeMedia'
import { fakeApi, renderApp, teacher } from '../../test/renderApp'

vi.mock('./wav', () => ({ recordingToWav: () => Promise.resolve(new Blob(['wav'], { type: 'audio/wav' })) }))

const i18n = initI18n()
const firstRun = { ...teacher, setupComplete: false }
const withGreeting = { ...teacher, greeting: { url: '/api/audio/abc', seconds: 8.4 } }
const template = { status: 200, body: { version: '1', status: 'draft', approvedAt: null, languages: [] } }
const eveningRoute = { [`GET /api/classes/${teacher.classId}/message-template`]: () => template }

/** Taps the mic, lets 2 s pass on the (faked) clock and taps again, so a greeting is recorded. */
async function record() {
  await userEvent.click(screen.getByRole('button', { name: 'Record' }))
  vi.advanceTimersByTime(2000)
  await userEvent.click(await screen.findByRole('button', { name: 'Stop recording' }))
}

const saves = () =>
  vi.mocked(fetch).mock.calls.filter(([url]) => String(url) === '/api/me/greeting').map(([, init]) => init?.body as FormData)

describe('GreetingScreen (T0 step 1)', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
    installFakeMedia()
    // Only the clock the recorder measures with: userEvent and Testing Library keep real timers.
    vi.useFakeTimers({ toFake: ['performance'] })
    vi.stubGlobal('Audio', class { onended: (() => void) | null = null; play = () => Promise.resolve(); pause = () => {} })
    URL.createObjectURL = vi.fn<() => string>(() => 'blob:greeting')
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
  })

  it('suggests words with her name and says what is missing instead of disabling Save', async () => {
    fakeApi({ 'GET /api/me': () => ({ status: 200, body: firstRun }) })
    renderApp('/welcome/greeting')

    const heading = await screen.findByRole('heading', { level: 1, name: 'A 10-second greeting in your voice' })
    expect(heading).toHaveFocus()
    expect(screen.getByText(/this is Shobha teacher/)).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Save and continue' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('Record your greeting first.')

    await record()
    await userEvent.click(screen.getByRole('button', { name: 'Save and continue' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('Tick the box to agree.')
    expect(saves()).toHaveLength(0)
  })

  it('plays the recording back and lets her record again', async () => {
    fakeApi({ 'GET /api/me': () => ({ status: 200, body: firstRun }) })
    renderApp('/welcome/greeting')
    await screen.findByRole('heading', { level: 1 })

    await record()
    const play = await screen.findByRole('button', { name: 'Play your greeting' })
    await userEvent.click(play)
    expect(play).toHaveAttribute('aria-pressed', 'true')

    await userEvent.click(screen.getByRole('button', { name: 'Record again' }))
    expect(screen.getByRole('button', { name: 'Record' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Play your greeting' })).not.toBeInTheDocument()
  })

  it('saves the recording with her consent, retries with the same request ID, then opens the evening step', async () => {
    let attempts = 0
    fakeApi({
      'GET /api/me': () => ({ status: 200, body: firstRun }),
      'POST /api/me/greeting': () => (++attempts === 1 ? 'offline' : { status: 200, body: withGreeting }),
      ...eveningRoute,
    })
    renderApp('/welcome/greeting')
    await screen.findByRole('heading', { level: 1 })
    await record()
    await userEvent.click(screen.getByRole('checkbox'))

    await userEvent.click(screen.getByRole('button', { name: 'Save and continue' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('No network. Your greeting is not saved yet.')
    await userEvent.click(screen.getByRole('button', { name: 'Save and continue' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'The evening message on a day of absence' })).toBeInTheDocument()
    const [first, second] = saves()
    expect(first?.get('consent')).toBe('true')
    const file = first?.get('file') as File | null
    expect(file?.type).toBe('audio/wav')
    expect(first?.get('clientRequestId')).toBe(second?.get('clientRequestId'))
    expect(JSON.parse(localStorage.getItem('slate.me') ?? 'null')).toMatchObject({ setupComplete: true, greeting: { seconds: 8.4 } })
  })

  it('a new recording is a new request', async () => {
    fakeApi({
      'GET /api/me': () => ({ status: 200, body: firstRun }),
      'POST /api/me/greeting': () => ({ status: 500 }),
    })
    renderApp('/welcome/greeting')
    await screen.findByRole('heading', { level: 1 })
    await userEvent.click(screen.getByRole('checkbox'))

    await record()
    await userEvent.click(screen.getByRole('button', { name: 'Save and continue' }))
    await waitFor(() => expect(saves()).toHaveLength(1))
    await userEvent.click(screen.getByRole('button', { name: 'Record again' }))
    await record()
    await userEvent.click(screen.getByRole('button', { name: 'Save and continue' }))

    await waitFor(() => expect(saves()).toHaveLength(2))
    const [first, second] = saves()
    expect(first?.get('clientRequestId')).not.toBe(second?.get('clientRequestId'))
    expect(await screen.findByRole('alert')).toHaveTextContent('Could not save your greeting. Try again.')
  })

  it('with the microphone blocked she can continue without a greeting', async () => {
    installFakeMedia({ allow: false })
    const calls = fakeApi({
      'GET /api/me': () => ({ status: 200, body: firstRun }),
      'POST /api/me/welcome': () => ({ status: 200, body: teacher }),
      ...eveningRoute,
    })
    renderApp('/welcome/greeting')
    await userEvent.click(await screen.findByRole('button', { name: 'Record' }))

    expect(await screen.findByText(/The microphone is off for this app/)).toBeInTheDocument()
    expect(screen.queryByRole('checkbox')).not.toBeInTheDocument() // no voice, no voice consent
    await userEvent.click(screen.getByRole('button', { name: 'Continue without a greeting' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'The evening message on a day of absence' })).toBeInTheDocument()
    expect(calls.some((c) => c.key === 'POST /api/me/welcome')).toBe(true)
  })

  it('later, from Today: no step label, a Not now link, and Save returns to Today', async () => {
    fakeApi({
      'GET /api/me': () => ({ status: 200, body: teacher }),
      'POST /api/me/greeting': () => ({ status: 200, body: withGreeting }),
    })
    renderApp('/setup/greeting')
    await screen.findByRole('heading', { level: 1, name: 'A 10-second greeting in your voice' })
    expect(screen.queryByText('Step 1 of 2')).not.toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Not now' })).toHaveAttribute('href', '/')

    await record()
    await userEvent.click(screen.getByRole('checkbox'))
    await userEvent.click(screen.getByRole('button', { name: 'Save and continue' }))

    expect(await screen.findByRole('heading', { level: 1, name: /^Today, / })).toBeInTheDocument()
  })

  it('from Settings (T-71): Not now and Save both go back to Settings', async () => {
    fakeApi({
      'GET /api/me': () => ({ status: 200, body: withGreeting }),
      'POST /api/me/greeting': () => ({ status: 200, body: withGreeting }),
      ...eveningRoute,
    })
    renderApp('/settings/greeting')
    await screen.findByRole('heading', { level: 1, name: 'A 10-second greeting in your voice' })
    expect(screen.queryByText('Step 1 of 2')).not.toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Not now' })).toHaveAttribute('href', '/settings')

    await record()
    await userEvent.click(screen.getByRole('checkbox'))
    await userEvent.click(screen.getByRole('button', { name: 'Save and continue' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Settings' })).toBeInTheDocument()
    expect(saves()).toHaveLength(1)
  })
})
