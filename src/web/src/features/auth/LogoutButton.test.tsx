import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { fakeApi, renderApp, teacher } from '../../test/renderApp'

const i18n = initI18n()

describe('LogoutButton', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('ends the session, forgets the cached teacher, keeps the remembered phone and opens sign-in', async () => {
    localStorage.setItem('slate.teacher', JSON.stringify({ phone: '9000000000', displayName: 'Shobha' }))
    localStorage.setItem('slate.me', JSON.stringify(teacher))
    let signedIn = true
    const calls = fakeApi({
      'GET /api/me': () => (signedIn ? { status: 200, body: teacher } : { status: 401 }),
      'POST /api/auth/logout': () => {
        signedIn = false
        return { status: 204 }
      },
    })
    renderApp('/')

    await userEvent.click(await screen.findByRole('button', { name: 'Log out' }))

    expect(await screen.findByRole('button', { name: 'Sign in' })).toBeInTheDocument()
    expect(calls.some((c) => c.key === 'POST /api/auth/logout')).toBe(true)
    expect(localStorage.getItem('slate.me')).toBeNull()
    expect(localStorage.getItem('slate.teacher')).not.toBeNull()
  })

  it('stays signed in when the network is down', async () => {
    fakeApi({
      'GET /api/me': () => ({ status: 200, body: teacher }),
      'POST /api/auth/logout': () => 'offline',
    })
    renderApp('/')

    await userEvent.click(await screen.findByRole('button', { name: 'Log out' }))

    await waitFor(() => expect(screen.getByTestId('announcer')).toHaveTextContent('No network.'))
    expect(screen.getByRole('button', { name: 'Log out' })).toBeEnabled()
  })

  it('is not shown on the sign-in screen', async () => {
    fakeApi({ 'GET /api/me': () => ({ status: 401 }) })
    renderApp('/login')

    await screen.findByRole('button', { name: 'Sign in' })
    expect(screen.queryByRole('button', { name: 'Log out' })).not.toBeInTheDocument()
  })
})
