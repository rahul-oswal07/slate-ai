import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { NavLink, useLocation } from 'react-router'
import { ApiError } from '../../api/client'
import { adminKey } from './useAdmin'

const PAGES = [
  { path: '/admin/greetings', label: 'admin.nav.greetings' },
  { path: '/admin/lesson-audio', label: 'admin.nav.lessonAudio' },
] as const

/**
 * The frame of every admin page (D-83): title, the links between them (each keeps `#key=…`) and the "needs its full
 * link" state when there is no key. Linked from nowhere in the app.
 */
export function AdminPage({ title, children }: { title: string; children: (key: string) => ReactNode }) {
  const { t } = useTranslation()
  const { hash } = useLocation()
  const key = adminKey(hash)

  return (
    <main className="mx-auto flex min-h-dvh max-w-xl flex-col gap-4 bg-canvas px-4 py-6 text-ink">
      {key !== null && (
        <nav aria-label={t('admin.nav.label')} className="flex gap-4">
          {PAGES.map((page) => (
            <NavLink
              key={page.path}
              to={{ pathname: page.path, hash }}
              className={({ isActive }) =>
                `inline-flex min-h-target items-center text-label ${isActive ? 'text-ink underline' : 'text-primary-strong'}`
              }
            >
              {t(page.label)}
            </NavLink>
          ))}
        </nav>
      )}
      <h1 className="text-title">{title}</h1>
      {key === null ? <NeedsLink /> : children(key)}
    </main>
  )
}

/** Loading, a wrong key (404, shown like no key) and other failures of an admin query; else its content. */
export function AdminQuery<T>({
  query,
  children,
}: {
  query: { data: T | undefined; error: unknown; isError: boolean; refetch: () => unknown }
  children: (data: T) => ReactNode
}) {
  const { t } = useTranslation()
  if (query.error instanceof ApiError && query.error.status === 404) {
    return <NeedsLink />
  }
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

function NeedsLink() {
  const { t } = useTranslation()
  return (
    <p role="alert" className="border-l-4 border-l-warning bg-surface p-3 text-body">
      {t('admin.needsLink')}
    </p>
  )
}
