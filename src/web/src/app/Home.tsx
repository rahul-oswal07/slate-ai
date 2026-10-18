import { useTranslation } from 'react-i18next'

/** Placeholder for the Today screen (T-12). */
export function Home() {
  const { t } = useTranslation()

  return (
    <section className="rounded-lg border border-border bg-surface p-4">
      <h1 className="text-2xl font-semibold">{t('home.title')}</h1>
      <p className="mt-2 text-ink-muted">{t('home.body')}</p>
    </section>
  )
}
