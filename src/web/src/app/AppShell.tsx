import { useQueryClient } from '@tanstack/react-query'
import { useCallback, useEffect, useRef } from 'react'
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
  const headerRef = useStickyHeaderPadding()
  const tagline = t('app.tagline')

  return (
    <AnnouncerProvider>
      <div className="min-h-dvh font-sans text-ink">
        <a
          href="#main"
          className="sr-only focus:not-sr-only focus:absolute focus:left-2 focus:top-2 focus:rounded-md focus:bg-primary focus:p-3 focus:text-on-primary"
        >
          {t('a11y.skipToContent')}
        </a>
        {/*
          One row (T-109, D-101): the name centred with language and the profile menu, the tagline under the name
          (T-114). It stays at the top while she scrolls; family pages (/p/…) don't use this shell.
        */}
        <header
          ref={headerRef}
          className="sticky top-0 z-20 grid grid-cols-[minmax(0,1fr)_auto] items-center gap-x-2 border-b border-edge bg-canvas px-4 py-3 print:static"
        >
          <p className="min-w-0 font-heading text-title font-extrabold">{t('app.name')}</p>
          <div className="flex shrink-0 items-center gap-2">
            <LanguageSwitcher compact={!signIn} />
            <ProfileMenu />
          </div>
          {/* Kannada has none: the name already says "the missed lesson". */}
          {tagline && <p className="text-caption text-ink-muted">{tagline}</p>}
        </header>
        <main id="main" className="px-4 py-6">
          <Outlet />
        </main>
      </div>
    </AnnouncerProvider>
  )
}

/**
 * Keeps scrolled-to targets (the skip link's #main, a focused heading) clear of the sticky header by setting the
 * page's scroll padding to its height, while the teacher shell is on screen.
 */
function useStickyHeaderPadding() {
  const ref = useRef<HTMLElement>(null)
  useEffect(() => {
    const header = ref.current
    const root = document.documentElement
    if (!header) {
      return
    }
    const update = () => {
      root.style.scrollPaddingTop = `${header.offsetHeight}px`
    }
    update()
    const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(update)
    observer?.observe(header)
    return () => {
      observer?.disconnect()
      root.style.scrollPaddingTop = ''
    }
  }, [])
  return ref
}
