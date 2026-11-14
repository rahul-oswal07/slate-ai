import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { I18nextProvider } from 'react-i18next'
import { initI18n, LANGUAGE_STORAGE_KEY, SUPPORTED_LANGUAGES } from '../i18n'
import { LanguageSwitcher } from './LanguageSwitcher'

const i18n = initI18n()

function renderSwitcher(compact = false) {
  return render(
    <I18nextProvider i18n={i18n}>
      <LanguageSwitcher compact={compact} />
    </I18nextProvider>,
  )
}

describe('LanguageSwitcher (T-107)', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('kn')
  })

  it('is one dropdown named in the current language, showing the current language', () => {
    renderSwitcher()

    const select = screen.getByRole('combobox', { name: 'ಭಾಷೆ' })
    expect(select).toHaveValue('kn')
    expect(select).toHaveAttribute('lang', 'kn')
    expect(screen.queryAllByRole('button')).toHaveLength(0)
  })

  it('lists the five languages in order, each in its own script', () => {
    renderSwitcher()

    const options = within(screen.getByRole('combobox')).getAllByRole('option')
    expect(options.map((o) => o.getAttribute('value'))).toEqual([...SUPPORTED_LANGUAGES])
    expect(options.map((o) => o.getAttribute('lang'))).toEqual([...SUPPORTED_LANGUAGES])
    expect(options.map((o) => o.textContent)).toEqual(['ಕನ್ನಡ', 'English', 'తెలుగు', 'தமிழ்', 'हिन्दी'])
  })

  it('changes the language at once and remembers it', async () => {
    renderSwitcher()

    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'ಭಾಷೆ' }), 'ta')

    const select = screen.getByRole('combobox', { name: 'மொழி' })
    expect(select).toHaveValue('ta')
    expect(select).toHaveAttribute('lang', 'ta')
    expect(i18n.language).toBe('ta')
    expect(document.documentElement.lang).toBe('ta')
    expect(localStorage.getItem(LANGUAGE_STORAGE_KEY)).toBe('ta')
  })

  it('compact (T-109): shows a short label, and is still the same dropdown with the full names', async () => {
    renderSwitcher(true)

    const select = screen.getByRole('combobox', { name: 'ಭಾಷೆ' })
    expect(select).toHaveValue('kn')
    expect(screen.getByText('ಕ', { selector: 'span' })).toHaveAttribute('lang', 'kn')
    expect(within(select).getAllByRole('option').map((o) => o.textContent)).toEqual(['ಕನ್ನಡ', 'English', 'తెలుగు', 'தமிழ்', 'हिन्दी'])

    await userEvent.selectOptions(select, 'ta')

    expect(screen.getByRole('combobox', { name: 'மொழி' })).toHaveValue('ta')
    expect(screen.getByText('த', { selector: 'span' })).toHaveAttribute('lang', 'ta')
    expect(document.documentElement.lang).toBe('ta')
  })
})
