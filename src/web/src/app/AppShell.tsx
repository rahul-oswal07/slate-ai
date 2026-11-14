import { useQueryClient } from '@tanstack/react-query'
import { useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { Outlet, useLocation } from 'react-router'
import { AnnouncerProvider } from '../components/Announcer'
import { LanguageSwitcher } from '../components/LanguageSwitcher'
import { useOutboxSync } from '../offline/outbox'
import { ProfileMenu } from './ProfileMenu'

export function AppShell() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  // Beside the profile button the language dropdown is short; sign-in has no profile button and room for the name.
  const signIn = useLocation().pathname === '/login'
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
        {/* One row (T-109, D-101): the name with the tagline under it, then language and the profile menu. */}
        <header className="flex items-center justify-between gap-2 border-b border-edge px-4 py-3 print:hidden">
          <div className="min-w-0">
            <p className="font-heading text-title font-extrabold">{t('app.name')}</p>
            <p className="text-caption text-ink-muted">{t('app.tagline')}</p>
          </div>
          <div className="flex shrink-0 items-center gap-2">
            <LanguageSwitcher compact={!signIn} />
            <ProfileMenu />
          </div>
        </header>
        <main id="main" className="px-4 py-6">
          <Outlet />
        </main>
      </div>
    </AnnouncerProvider>
  )
}
