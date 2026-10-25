import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { fakeApi, renderApp, teacher } from '../../test/renderApp'

const i18n = initI18n()
const firstRun = { ...teacher, setupComplete: false }

describe('WelcomeScreen', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('shows the three promises and moves focus to the heading', async () => {
    fakeApi({ 'GET /api/me': () => ({ status: 200, body: firstRun }) })

    renderApp('/welcome')

    const heading = await screen.findByRole('heading', { level: 1, name: 'Namaskara, Shobha ma’am' })
    expect(heading).toHaveFocus()
    expect(screen.getByText('Attendance comes from SATS. Just one voice note a day.')).toBeInTheDocument()
    expect(screen.getByText('Nothing reaches a parent until you approve it.')).toBeInTheDocument()
    expect(screen.getByText('Only you can see these records.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /back/i })).not.toBeInTheDocument()
  })

  it('Start opens the greeting step without finishing setup (the voice consent does that)', async () => {
    const calls = fakeApi({ 'GET /api/me': () => ({ status: 200, body: firstRun }) })
    renderApp('/welcome')

    await userEvent.click(await screen.findByRole('button', { name: 'Start' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'A 10-second greeting in your voice' })).toBeInTheDocument()
    expect(screen.getByText('Step 1 of 2')).toBeInTheDocument()
    expect(calls.some((c) => c.key === 'POST /api/me/welcome')).toBe(false)
  })

  it('a teacher who finished setup is sent to Today', async () => {
    fakeApi({ 'GET /api/me': () => ({ status: 200, body: teacher }) })

    renderApp('/welcome')

    expect(await screen.findByRole('heading', { level: 1, name: /^Today, / })).toBeInTheDocument()
  })
})
