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

  it('Start finishes setup with one request ID, even when retried, then opens the evening-message step', async () => {
    let attempts = 0
    const calls = fakeApi({
      'GET /api/me': () => ({ status: 200, body: firstRun }),
      'POST /api/me/welcome': () => (++attempts === 1 ? 'offline' : { status: 200, body: teacher }),
      [`GET /api/classes/${teacher.classId}/message-template`]: () => ({
        status: 200,
        body: { version: '1', status: 'draft', approvedAt: null, languages: [] },
      }),
    })
    renderApp('/welcome')

    await userEvent.click(await screen.findByRole('button', { name: 'Start' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('No network. Connect and tap Start again.')

    await userEvent.click(screen.getByRole('button', { name: 'Start' }))

    expect(
      await screen.findByRole('heading', { level: 1, name: 'The evening message on a day of absence' }),
    ).toBeInTheDocument()
    const ids = calls.filter((c) => c.key === 'POST /api/me/welcome').map((c) => c.body)
    expect(ids).toHaveLength(2)
    expect(ids[0]).toEqual(ids[1])
    expect(JSON.parse(localStorage.getItem('slate.me') ?? 'null')).toMatchObject({ setupComplete: true })
  })

  it('a teacher who finished setup is sent to Today', async () => {
    fakeApi({ 'GET /api/me': () => ({ status: 200, body: teacher }) })

    renderApp('/welcome')

    expect(await screen.findByRole('heading', { level: 1, name: /^Today, / })).toBeInTheDocument()
  })
})
