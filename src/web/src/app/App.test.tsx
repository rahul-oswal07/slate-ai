import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { initI18n, LANGUAGE_STORAGE_KEY } from '../i18n'
import { App } from './App'

const i18n = initI18n()

function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>,
  )
}

describe('App shell', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('kn')
  })

  it('renders in Kannada by default', () => {
    renderAt('/')

    expect(screen.getByText('ತಪ್ಪಿದ ಪಾಠ')).toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('ಇಂದು')
    expect(document.documentElement.lang).toBe('kn')
  })

  it('switches language, updates <html lang> and remembers the choice', async () => {
    renderAt('/')

    await userEvent.click(screen.getByRole('button', { name: 'English' }))

    expect(screen.getByText('Tappida Paatha')).toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Today')
    expect(screen.getByRole('button', { name: 'English' })).toHaveAttribute('aria-pressed', 'true')
    expect(document.documentElement.lang).toBe('en')
    expect(localStorage.getItem(LANGUAGE_STORAGE_KEY)).toBe('en')
  })

  it('shows the not-found page for unknown routes', () => {
    renderAt('/no-such-page')

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('ಪುಟ ಸಿಗಲಿಲ್ಲ')
    expect(screen.getByRole('link', { name: 'ಇಂದಿನ ಪುಟಕ್ಕೆ ಹೋಗಿ' })).toHaveAttribute('href', '/')
  })
})
