import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n, LANGUAGE_STORAGE_KEY } from '../i18n'
import { fakeApi, renderApp, teacher } from '../test/renderApp'

const i18n = initI18n()

describe('App shell', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('kn')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('sends a signed-out teacher to sign in, in Kannada by default', async () => {
    fakeApi({ 'GET /api/me': () => ({ status: 401 }) })

    renderApp('/')

    expect(await screen.findByRole('heading', { level: 1, name: 'ಸೈನ್ ಇನ್ ಮಾಡಿ' })).toBeInTheDocument()
    expect(screen.getByText('ತಪ್ಪಿದ ಪಾಠ')).toBeInTheDocument()
    expect(document.documentElement.lang).toBe('kn')
  })

  it('opens Today for a signed-in teacher who has finished setup', async () => {
    fakeApi({ 'GET /api/me': () => ({ status: 200, body: teacher }) })

    renderApp('/')

    expect(await screen.findByRole('heading', { level: 1, name: /^ಇಂದು, / })).toBeInTheDocument()
  })

  it('opens the welcome the first time', async () => {
    fakeApi({ 'GET /api/me': () => ({ status: 200, body: { ...teacher, setupComplete: false } }) })

    renderApp('/')

    expect(await screen.findByRole('heading', { level: 1, name: 'ನಮಸ್ಕಾರ, Shobha ಮೇಡಂ' })).toBeInTheDocument()
  })

  it('opens Today offline from the last known teacher', async () => {
    localStorage.setItem('slate.me', JSON.stringify(teacher))
    fakeApi({ 'GET /api/me': () => 'offline' })

    renderApp('/')

    expect(await screen.findByRole('heading', { level: 1, name: /^ಇಂದು, / })).toBeInTheDocument()
  })

  it('switches language, updates <html lang> and remembers the choice', async () => {
    fakeApi({ 'GET /api/me': () => ({ status: 401 }) })
    renderApp('/login')

    await userEvent.click(await screen.findByRole('button', { name: 'English' }))

    expect(screen.getByText('Tappida Paatha')).toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Sign in')
    expect(screen.getByRole('button', { name: 'English' })).toHaveAttribute('aria-pressed', 'true')
    expect(document.documentElement.lang).toBe('en')
    expect(localStorage.getItem(LANGUAGE_STORAGE_KEY)).toBe('en')
  })

  it('shows the not-found page for unknown routes', () => {
    fakeApi({})

    renderApp('/no-such-page')

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('ಪುಟ ಸಿಗಲಿಲ್ಲ')
    expect(screen.getByRole('link', { name: 'ಇಂದಿನ ಪುಟಕ್ಕೆ ಹೋಗಿ' })).toHaveAttribute('href', '/')
  })
})
