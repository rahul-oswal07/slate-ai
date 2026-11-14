import { useQueryClient } from '@tanstack/react-query'
import { useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { Outlet } from 'react-router'
import { AnnouncerProvider } from '../components/Announcer'
import { LanguageSwitcher } from '../components/LanguageSwitcher'
import { LogoutButton } from '../features/auth/LogoutButton'
import { useOutboxSync } from '../offline/outbox'

export function AppShell() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  // Once queued writes reach the server, show the server's view of them.
  const refresh = useCallback(() => {
    void queryClient.invalidateQueries({ queryKey: ['attendance'] })
    void queryClient.invalidateQueries({ queryKey: ['lesson'] })
  }, [queryClient])
  useOutboxSync(refresh)

  return (
    <AnnouncerProvider>
      <div className="min-h-dvh font-sans text-ink">
        <a
          href="#main"
          className="sr-only focus:not-sr-only focus:absolute focus:left-2 focus:top-2 focus:rounded-md focus:bg-primary focus:p-3 focus:text-on-primary"
        >
          {t('a11y.skipToContent')}
        </a>
        <header className="border-b border-edge px-4 py-3 print:hidden">
          {/* Name and Log out, then the tagline beside the language dropdown: at 360 px a third control in the
              first row squeezes the name to ~90 px and breaks it in Tamil (T-107). */}
          <div className="flex items-start justify-between gap-3">
            <p className="min-w-0 font-heading text-title font-extrabold">{t('app.name')}</p>
            <LogoutButton />
          </div>
          <div className="flex items-center justify-between gap-3">
            <p className="min-w-0 text-caption text-ink-muted">{t('app.tagline')}</p>
            <LanguageSwitcher />
          </div>
        </header>
        <main id="main" className="px-4 py-6">
          <Outlet />
        </main>
      </div>
    </AnnouncerProvider>
  )
}
