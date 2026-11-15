import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { NavLink } from 'react-router'
import { ADMIN_PAGES as PAGES } from './adminPages'

const NAV = [{ path: '/admin', label: 'admin.nav.home' }, ...PAGES] as const

/**
 * The frame of every admin page (D-83): title, the links between them and the page. Linked from nowhere in the app;
 * no key, so anyone with the address can open it (D-112).
 */
export function AdminPage({ title, children }: { title: string; children: ReactNode }) {
  const { t } = useTranslation()

  return (
    <main className="mx-auto flex min-h-dvh max-w-xl flex-col gap-4 bg-canvas px-4 py-6 text-ink">
      <nav aria-label={t('admin.nav.label')} className="flex flex-wrap gap-x-4">
        {NAV.map((page) => (
          <NavLink
            key={page.path}
            to={page.path}
            end
            className={({ isActive }) =>
              `inline-flex min-h-target items-center text-label ${isActive ? 'text-ink underline' : 'text-primary-strong'}`
            }
          >
            {t(page.label)}
          </NavLink>
        ))}
      </nav>
      <h1 className="text-title">{title}</h1>
      {children}
    </main>
  )
}

/** Loading and failures of an admin query; else its content. */
export function AdminQuery<T>({
  query,
  children,
}: {
  query: { data: T | undefined; isError: boolean; refetch: () => unknown }
  children: (data: T) => ReactNode
}) {
  const { t } = useTranslation()
  if (query.isError) {
    return (
      <div role="alert" className="flex flex-col gap-3 border-l-4 border-l-error bg-error-bg p-3 text-body">
        {t('admin.failed')}
        <button type="button" onClick={() => void query.refetch()} className="btn-secondary w-auto self-start">
          {t('common.tryAgain')}
        </button>
      </div>
    )
  }
  if (query.data === undefined) {
    return <output className="block text-body text-ink-muted">{t('admin.loading')}</output>
  }
  return <>{children(query.data)}</>
}
