import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { AdminPage } from './AdminPage'
import { ADMIN_PAGES as PAGES } from './adminPages'

/**
 * /admin: every admin page with its address and what it is for. It asks the API nothing. No key (D-112).
 */
export function AdminIndexScreen() {
  const { t } = useTranslation()

  return (
    <AdminPage title={t('admin.index.title')}>
      <>
        <p className="text-body text-ink-muted">{t('admin.index.intro')}</p>
        <ul className="flex flex-col gap-3">
          {PAGES.map((page) => (
            <li key={page.path} className="rounded-card border-[1.5px] border-edge bg-surface p-3">
              <Link to={page.path} className="inline-flex min-h-target items-center text-label text-primary-strong underline">
                {t(page.label)}
              </Link>
              <p className="font-mono text-caption text-ink-muted">{page.path}</p>
              <p className="mt-1 text-body">{t(page.description)}</p>
            </li>
          ))}
        </ul>
      </>
    </AdminPage>
  )
}
