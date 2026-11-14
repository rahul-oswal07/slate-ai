import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../i18n'
import { fakeApi, renderApp, teacher } from '../test/renderApp'

const i18n = initI18n()

const signedIn = { 'GET /api/me': () => ({ status: 200, body: teacher }) }

async function openMenu() {
  const button = await screen.findByRole('button', { name: 'Profile and settings' })
  await userEvent.click(button)
  return { button, menu: screen.getByRole('menu', { name: 'Profile and settings' }) }
}

describe('ProfileMenu (T-109)', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('opens from the header with Profile, Settings and Log out, focus on the first', async () => {
    fakeApi(signedIn)
    renderApp('/')

    const { button, menu } = await openMenu()

    expect(button).toHaveAttribute('aria-expanded', 'true')
    expect(within(menu).getAllByRole('menuitem').map((item) => item.textContent)).toEqual(['Profile', 'Settings', 'Log out'])
    expect(within(menu).getByRole('menuitem', { name: 'Profile' })).toHaveFocus()
    // Log out is no longer in the header itself.
    expect(screen.queryByRole('button', { name: 'Log out' })).not.toBeInTheDocument()
  })

  it('is on every teacher screen, not only Today', async () => {
    fakeApi(signedIn)
    renderApp('/week')

    expect(await screen.findByRole('button', { name: 'Profile and settings' })).toBeInTheDocument()
  })

  it('opens Profile and Settings, and closes after choosing', async () => {
    fakeApi(signedIn)
    renderApp('/')

    await userEvent.click(within((await openMenu()).menu).getByRole('menuitem', { name: 'Profile' }))
    expect(await screen.findByRole('heading', { level: 1, name: 'Profile' })).toBeInTheDocument()
    expect(screen.queryByRole('menu')).not.toBeInTheDocument()

    await userEvent.click(within((await openMenu()).menu).getByRole('menuitem', { name: 'Settings' }))
    expect(await screen.findByRole('heading', { level: 1, name: 'Settings' })).toBeInTheDocument()
    expect(screen.queryByRole('menu')).not.toBeInTheDocument()
  })

  it('moves between items with the arrow keys, Home and End', async () => {
    fakeApi(signedIn)
    renderApp('/')
    const { menu } = await openMenu()
    const item = (name: string) => within(menu).getByRole('menuitem', { name })

    await userEvent.keyboard('{ArrowDown}')
    expect(item('Settings')).toHaveFocus()
    await userEvent.keyboard('{ArrowDown}{ArrowDown}')
    expect(item('Profile')).toHaveFocus()
    await userEvent.keyboard('{ArrowUp}')
    expect(item('Log out')).toHaveFocus()
    await userEvent.keyboard('{Home}')
    expect(item('Profile')).toHaveFocus()
    await userEvent.keyboard('{End}')
    expect(item('Log out')).toHaveFocus()
  })

  it('Escape closes it and puts focus back on the button', async () => {
    fakeApi(signedIn)
    renderApp('/')
    const { button } = await openMenu()

    await userEvent.keyboard('{Escape}')

    expect(screen.queryByRole('menu')).not.toBeInTheDocument()
    expect(button).toHaveAttribute('aria-expanded', 'false')
    expect(button).toHaveFocus()
  })

  it('a tap outside closes it and puts focus back on the button', async () => {
    fakeApi(signedIn)
    renderApp('/')
    const { button } = await openMenu()

    await userEvent.click(screen.getByText('The missed lesson, at home'))

    expect(screen.queryByRole('menu')).not.toBeInTheDocument()
    expect(button).toHaveFocus()
  })

  it('Log out ends the session, forgets the cached teacher, keeps the remembered phone and opens sign-in', async () => {
    localStorage.setItem('slate.teacher', JSON.stringify({ phone: '9000000000', displayName: 'Shobha' }))
    localStorage.setItem('slate.me', JSON.stringify(teacher))
    let session = true
    const calls = fakeApi({
      'GET /api/me': () => (session ? { status: 200, body: teacher } : { status: 401 }),
      'POST /api/auth/logout': () => {
        session = false
        return { status: 204 }
      },
    })
    renderApp('/')

    await userEvent.click(within((await openMenu()).menu).getByRole('menuitem', { name: 'Log out' }))

    expect(await screen.findByRole('button', { name: 'Sign in' })).toBeInTheDocument()
    expect(calls.some((c) => c.key === 'POST /api/auth/logout')).toBe(true)
    expect(localStorage.getItem('slate.me')).toBeNull()
    expect(localStorage.getItem('slate.teacher')).not.toBeNull()
  })

  it('stays signed in when the network is down, and says so', async () => {
    fakeApi({ ...signedIn, 'POST /api/auth/logout': () => 'offline' })
    renderApp('/')

    await userEvent.click(within((await openMenu()).menu).getByRole('menuitem', { name: 'Log out' }))

    await waitFor(() => expect(screen.getByTestId('announcer')).toHaveTextContent('No network.'))
    const button = screen.getByRole('button', { name: 'Profile and settings' })
    await waitFor(() => expect(button).toBeEnabled())
    expect(button).toHaveFocus()
  })

  it('has only Log out before set-up is done', async () => {
    fakeApi({ 'GET /api/me': () => ({ status: 200, body: { ...teacher, setupComplete: false } }) })
    renderApp('/welcome')

    const { menu } = await openMenu()

    expect(within(menu).getAllByRole('menuitem').map((item) => item.textContent)).toEqual(['Log out'])
  })

  it('is not shown on the sign-in screen', async () => {
    fakeApi({ 'GET /api/me': () => ({ status: 401 }) })
    renderApp('/login')

    await screen.findByRole('button', { name: 'Sign in' })
    expect(screen.queryByRole('button', { name: 'Profile and settings' })).not.toBeInTheDocument()
    // Name, tagline and language only.
    expect(screen.getByRole('combobox', { name: 'Language' })).toBeInTheDocument()
  })
})
