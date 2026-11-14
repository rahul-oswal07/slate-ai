import { useTranslation } from 'react-i18next'
import { isLanguage, SUPPORTED_LANGUAGES } from '../i18n'
import { Icon } from './Icon'

/**
 * One small language dropdown for the header (T-107, D-96). A native select: the phone's own picker, keyboard and
 * screen reader work as they do everywhere. Each language is written in its own script; the globe says "language"
 * to someone who can't read the current one. `i18n` sets `<html lang>` and remembers the choice.
 */
export function LanguageSwitcher() {
  const { t, i18n } = useTranslation()
  const current = i18n.resolvedLanguage

  return (
    <label className="relative inline-flex shrink-0 items-center text-ink">
      <span className="sr-only">{t('language.label')}</span>
      <Icon name="globe" size={18} className="pointer-events-none absolute left-2.5" />
      <select
        lang={current}
        value={current}
        onChange={(event) => {
          const code = event.target.value
          if (isLanguage(code)) {
            void i18n.changeLanguage(code)
          }
        }}
        className="min-h-target appearance-none rounded-button border-2 border-border bg-surface pl-8 pr-8 text-label font-bold text-ink"
      >
        {SUPPORTED_LANGUAGES.map((code) => (
          <option key={code} value={code} lang={code}>
            {t(`language.${code}`)}
          </option>
        ))}
      </select>
      <Icon name="chevronDown" size={18} className="pointer-events-none absolute right-2" />
    </label>
  )
}
