import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'

export function NotFound() {
  const { t } = useTranslation()

  return (
    <section>
      <h1 className="text-2xl font-semibold">{t('notFound.title')}</h1>
      <Link to="/" className="mt-4 inline-flex min-h-12 items-center text-primary-strong underline">
        {t('notFound.link')}
      </Link>
    </section>
  )
}
