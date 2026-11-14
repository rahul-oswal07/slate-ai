import { useTranslation } from 'react-i18next'
import { isLanguage, SUPPORTED_LANGUAGES } from '../i18n'
import { Icon } from './Icon'

/**
 * One small language dropdown for the header (T-107, D-96). A native select: the phone's own picker, keyboard and
 * screen reader work as they do everywhere. Each language is written in its own script; the globe says "language"
 * to someone who can't read the current one. `i18n` sets `<html lang>` and remembers the choice.
 *
 * `compact` (T-109, D-101) is for the header beside the profile button: the closed dropdown shows a short label
 * ("ಕ", "En") so the app name keeps one line at 360 px in every language, Tamil included. The select itself lies
 * invisible over that label, so the picker still lists the full names and a screen reader still reads the language.
 */
export function LanguageSwitcher({ compact = false }: { compact?: boolean }) {
  const { t, i18n } = useTranslation()
  const current = i18n.resolvedLanguage

  const select = (className: string) => (
    <select
      lang={current}
      value={current}
      onChange={(event) => {
        const code = event.target.value
        if (isLanguage(code)) {
          void i18n.changeLanguage(code)
        }
      }}
      className={className}
    >
      {SUPPORTED_LANGUAGES.map((code) => (
        <option key={code} value={code} lang={code}>
          {t(`language.${code}`)}
        </option>
      ))}
    </select>
  )

  if (compact) {
    return (
      <label className="relative inline-flex min-h-target shrink-0 items-center gap-1 rounded-button border-2 border-border bg-surface px-2.5 text-label font-bold text-ink has-[:focus-visible]:outline-3 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-focus">
        <span className="sr-only">{t('language.label')}</span>
        <Icon name="globe" size={18} />
        <span aria-hidden="true" lang={current}>
          {current ? t(`language.short.${current}`) : ''}
        </span>
        <Icon name="chevronDown" size={18} />
        {select('absolute inset-0 size-full cursor-pointer appearance-none opacity-0')}
      </label>
    )
  }

  return (
    <label className="relative inline-flex shrink-0 items-center text-ink">
      <span className="sr-only">{t('language.label')}</span>
      <Icon name="globe" size={18} className="pointer-events-none absolute left-2.5" />
      {select('min-h-target appearance-none rounded-button border-2 border-border bg-surface pl-8 pr-8 text-label font-bold text-ink')}
      <Icon name="chevronDown" size={18} className="pointer-events-none absolute right-2" />
    </label>
  )
}
