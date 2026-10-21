import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { fakeApi, renderApp, teacher } from '../../test/renderApp'

const i18n = initI18n()

describe('LoginScreen', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('first time: signs in with phone and PIN, remembers the phone, goes to the welcome', async () => {
    const calls = fakeApi({
      'GET /api/me': () => ({ status: 401 }),
      'POST /api/auth/pin': () => ({ status: 200, body: { ...teacher, setupComplete: false } }),
    })
    renderApp('/login')

    await userEvent.type(await screen.findByLabelText('Mobile number'), '+91 90000 00000')
    await userEvent.type(screen.getByLabelText('PIN'), '2468')
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Namaskara, Shobha ma’am' })).toBeInTheDocument()
    expect(calls.find((c) => c.key === 'POST /api/auth/pin')?.body).toEqual({ phone: '9000000000', pin: '2468' })
    expect(JSON.parse(localStorage.getItem('slate.teacher') ?? 'null')).toEqual({
      phone: '9000000000',
      displayName: 'Shobha',
    })
  })

  it('returning: asks only for the PIN, and "Change phone" asks for the number again', async () => {
    localStorage.setItem('slate.teacher', JSON.stringify({ phone: '9000000000', displayName: 'Shobha' }))
    const calls = fakeApi({
      'GET /api/me': () => ({ status: 401 }),
      'POST /api/auth/pin': () => ({ status: 200, body: teacher }),
    })
    renderApp('/login')

    expect(await screen.findByRole('heading', { level: 1, name: 'Namaskara, Shobha' })).toBeInTheDocument()
    expect(screen.queryByLabelText('Mobile number')).not.toBeInTheDocument()

    await userEvent.type(screen.getByLabelText('PIN'), '2468')
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('heading', { level: 1, name: /^Today, / })).toBeInTheDocument()
    expect(calls.find((c) => c.key === 'POST /api/auth/pin')?.body).toEqual({ phone: '9000000000', pin: '2468' })
  })

  it('"Change phone" forgets the remembered teacher', async () => {
    localStorage.setItem('slate.teacher', JSON.stringify({ phone: '9000000000', displayName: 'Shobha' }))
    fakeApi({ 'GET /api/me': () => ({ status: 401 }) })
    renderApp('/login')

    await userEvent.click(await screen.findByRole('button', { name: 'Not you? Change phone' }))

    expect(screen.getByLabelText('Mobile number')).toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 1, name: 'Sign in' })).toBeInTheDocument()
    expect(localStorage.getItem('slate.teacher')).toBeNull()
  })

  it('wrong PIN: says so, clears the PIN and stays on sign in', async () => {
    fakeApi({
      'GET /api/me': () => ({ status: 401 }),
      'POST /api/auth/pin': () => ({ status: 401, body: { title: 'Phone or PIN is wrong' } }),
    })
    renderApp('/login')

    await userEvent.type(await screen.findByLabelText('Mobile number'), '9000000000')
    await userEvent.type(screen.getByLabelText('PIN'), '1111')
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('The mobile number or PIN is wrong. Try again.')
    expect(screen.getByLabelText('PIN')).toHaveValue('')
    expect(screen.getByLabelText('PIN')).toHaveFocus()
    expect(localStorage.getItem('slate.teacher')).toBeNull()
  })

  it('locked out: says when to try again', async () => {
    fakeApi({
      'GET /api/me': () => ({ status: 401 }),
      'POST /api/auth/pin': () => ({ status: 429, body: { lockedUntil: '2026-10-21T04:15:00Z' } }),
    })
    renderApp('/login')

    await userEvent.type(await screen.findByLabelText('Mobile number'), '9000000000')
    await userEvent.type(screen.getByLabelText('PIN'), '1111')
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/^Too many wrong PINs\. Try again after .+\.$/)
  })

  it('checks the number and PIN before calling the server', async () => {
    const calls = fakeApi({ 'GET /api/me': () => ({ status: 401 }) })
    renderApp('/login')

    await userEvent.type(await screen.findByLabelText('Mobile number'), '12345')
    await userEvent.type(screen.getByLabelText('PIN'), '2468')
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))
    expect(screen.getByRole('alert')).toHaveTextContent('Enter a 10-digit mobile number.')
    expect(screen.getByLabelText('Mobile number')).toHaveAttribute('aria-invalid', 'true')

    await userEvent.clear(screen.getByLabelText('Mobile number'))
    await userEvent.type(screen.getByLabelText('Mobile number'), '9000000000')
    await userEvent.clear(screen.getByLabelText('PIN'))
    await userEvent.type(screen.getByLabelText('PIN'), '12')
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))
    expect(screen.getByRole('alert')).toHaveTextContent('The PIN is 4 to 6 digits.')

    expect(calls.some((c) => c.key === 'POST /api/auth/pin')).toBe(false)
  })

  it('offline: explains that sign-in needs the network', async () => {
    fakeApi({ 'GET /api/me': () => ({ status: 401 }), 'POST /api/auth/pin': () => 'offline' })
    renderApp('/login')

    await userEvent.type(await screen.findByLabelText('Mobile number'), '9000000000')
    await userEvent.type(screen.getByLabelText('PIN'), '2468')
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('No network. Connect to sign in.')
  })

  it('a signed-in teacher who opens /login goes to Today', async () => {
    fakeApi({ 'GET /api/me': () => ({ status: 200, body: teacher }) })

    renderApp('/login')

    expect(await screen.findByRole('heading', { level: 1, name: /^Today, / })).toBeInTheDocument()
  })
})
