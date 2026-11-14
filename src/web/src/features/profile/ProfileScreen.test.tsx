import { screen, within } from '@testing-library/react'
import { initI18n } from '../../i18n'
import { fakeApi, renderApp, teacher } from '../../test/renderApp'

const i18n = initI18n()

const profile = { ...teacher, phoneLast4: '3210', grade: 3, medium: 'kn' }

describe('ProfileScreen (T-109)', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('shows her name, the last 4 digits of her phone and her class, read only', async () => {
    fakeApi({ 'GET /api/me': () => ({ status: 200, body: profile }) })
    renderApp('/profile')

    const heading = await screen.findByRole('heading', { level: 1, name: 'Profile' })
    expect(heading).toHaveFocus()
    const details = within(screen.getByRole('region', { name: 'Profile' }))
    expect(details.getByText('Name').nextElementSibling).toHaveTextContent('Shobha')
    expect(details.getByText('Phone').nextElementSibling).toHaveTextContent('•••• 3210')
    expect(details.getByText('Class').nextElementSibling).toHaveTextContent('Grade 3 · ಕನ್ನಡ medium')
    expect(details.queryByRole('textbox')).not.toBeInTheDocument()
    expect(details.queryByRole('button')).not.toBeInTheDocument()
  })

  it('links back to Today and keeps Today as the current tab', async () => {
    fakeApi({ 'GET /api/me': () => ({ status: 200, body: profile }) })
    renderApp('/profile')

    await screen.findByRole('heading', { level: 1, name: 'Profile' })

    expect(screen.getByRole('link', { name: '‹ Today' })).toHaveAttribute('href', '/')
    expect(within(screen.getByRole('navigation')).getByRole('link', { current: 'page' })).toHaveAttribute('href', '/')
  })

  it('leaves out what the server did not send', async () => {
    // A `me` cached before this screen existed, or a teacher without a class.
    fakeApi({ 'GET /api/me': () => ({ status: 200, body: teacher }) })
    renderApp('/profile')

    await screen.findByRole('heading', { level: 1, name: 'Profile' })

    expect(screen.getByText('Name').nextElementSibling).toHaveTextContent('Shobha')
    expect(screen.queryByText('Phone')).not.toBeInTheDocument()
    expect(screen.queryByText('Class')).not.toBeInTheDocument()
  })

  it('shows the class in her language', async () => {
    await i18n.changeLanguage('kn')
    fakeApi({ 'GET /api/me': () => ({ status: 200, body: profile }) })
    renderApp('/profile')

    expect(await screen.findByText('3ನೇ ತರಗತಿ · ಕನ್ನಡ ಮಾಧ್ಯಮ')).toBeInTheDocument()
  })
})
