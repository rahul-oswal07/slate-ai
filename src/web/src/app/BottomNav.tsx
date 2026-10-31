import { useTranslation } from 'react-i18next'
import { Link, useLocation } from 'react-router'
import { Icon, type IconName } from '../components/Icon'
import { useMe } from '../features/auth/useMe'
import { usePackages, waitingCount } from '../features/approve/usePackages'

const tabs: { to: string; key: 'today' | 'approve' | 'missed' | 'back' | 'week'; icon: IconName }[] = [
  { to: '/', key: 'today', icon: 'house' },
  { to: '/approve', key: 'approve', icon: 'check' },
  { to: '/missed', key: 'missed', icon: 'list' },
  { to: '/back', key: 'back', icon: 'users' },
  { to: '/week', key: 'week', icon: 'chartColumn' },
]

/** Teacher bottom nav (T1 card §8): five equal tabs, active tab marked with aria-current. Hidden during setup. */
export function BottomNav() {
  const { t } = useTranslation()
  // T2 Lesson confirm and Settings (T-71, opened from Today's gear) are sub-screens of Today (T1 card §8, T10).
  // A plain Link, not NavLink: NavLink can't mark Today current on its sub-screens for screen readers.
  const pathname = useLocation().pathname
  const onTodaySubScreen = pathname === '/lesson' || pathname.startsWith('/settings')
  const active = (to: string) =>
    to === '/' ? pathname === '/' || onTodaySubScreen : pathname === to || pathname.startsWith(`${to}/`)
  // Catch-ups waiting for her (ready or needs review); neutral badge, never error-coloured (T3).
  const waiting = waitingCount(usePackages(useMe().data?.classId ?? '').data)

  return (
    <nav aria-label={t('nav.label')} className="fixed inset-x-0 bottom-0 z-10 border-t border-edge bg-surface print:hidden">
      <ul className="mx-auto grid max-w-md grid-cols-5">
        {tabs.map((tab) => (
          <li key={tab.key}>
            <Link
              to={tab.to}
              aria-current={active(tab.to) ? 'page' : undefined}
              className={`relative flex min-h-[60px] flex-col items-center justify-center gap-0.5 px-1 text-center text-caption [word-break:keep-all] ${
                active(tab.to)
                  ? "font-extrabold text-primary-strong before:absolute before:inset-x-3 before:top-0 before:h-1 before:rounded-b-sm before:bg-primary before:content-['']"
                  : 'text-ink-muted'
              }`}
            >
              <span className="relative">
                <Icon name={tab.icon} size={24} />
                {tab.key === 'approve' && waiting > 0 && (
                  <span
                    aria-hidden="true"
                    className="absolute -right-3.5 -top-1.5 min-w-5 rounded-pill bg-ink px-1 text-center text-[13px] font-bold leading-5 text-surface"
                  >
                    {waiting}
                  </span>
                )}
              </span>
              {t(`nav.${tab.key}`)}
              {tab.key === 'approve' && waiting > 0 && <span className="sr-only">{t('nav.approveWaiting', { n: waiting })}</span>}
            </Link>
          </li>
        ))}
      </ul>
    </nav>
  )
}
