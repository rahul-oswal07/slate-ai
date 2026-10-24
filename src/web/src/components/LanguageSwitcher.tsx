import { useTranslation } from 'react-i18next'
import { SUPPORTED_LANGUAGES } from '../i18n'

export function LanguageSwitcher() {
  const { t, i18n } = useTranslation()

  return (
    <fieldset className="mt-3 flex flex-wrap gap-2">
      <legend className="sr-only">{t('language.label')}</legend>
      {SUPPORTED_LANGUAGES.map((code) => {
        const active = i18n.resolvedLanguage === code
        return (
          <button
            key={code}
            type="button"
            lang={code}
            aria-pressed={active}
            onClick={() => void i18n.changeLanguage(code)}
            className={`min-h-12 min-w-12 rounded-button border-2 px-3 font-bold ${
              active ? 'border-primary bg-primary text-on-primary' : 'border-border bg-surface text-ink'
            }`}
          >
            {t(`language.${code}`)}
          </button>
        )
      })}
    </fieldset>
  )
}
