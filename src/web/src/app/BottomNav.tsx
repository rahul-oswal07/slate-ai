import { useTranslation } from 'react-i18next'
import { NavLink, useLocation } from 'react-router'
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
  // T2 Lesson confirm is a sub-screen of Today (T1 card §8).
  const onLessonConfirm = useLocation().pathname === '/lesson'
  // Catch-ups waiting for her (ready or needs review); neutral badge, never error-coloured (T3).
  const waiting = waitingCount(usePackages(useMe().data?.classId ?? '').data)

  return (
    <nav aria-label={t('nav.label')} className="fixed inset-x-0 bottom-0 z-10 border-t border-border bg-surface">
      <ul className="mx-auto grid max-w-md grid-cols-5">
        {tabs.map((tab) => (
          <li key={tab.key}>
            <NavLink
              to={tab.to}
              end={tab.to === '/'}
              aria-current={tab.to === '/' && onLessonConfirm ? 'page' : undefined}
              className={({ isActive: matched }) => {
                const isActive = matched || (tab.to === '/' && onLessonConfirm)
                return (
                `relative flex min-h-[56px] flex-col items-center justify-center gap-0.5 px-1 text-center text-caption [word-break:keep-all] ${
                  isActive
                    ? "font-bold text-primary-strong before:absolute before:inset-x-3 before:top-0 before:h-[3px] before:bg-primary before:content-['']"
                    : 'text-ink-muted'
                }`
                )
              }}
            >
              <span className="relative">
                <Icon name={tab.icon} size={22} />
                {tab.key === 'approve' && waiting > 0 && (
                  <span
                    aria-hidden="true"
                    className="absolute -right-3 -top-1 min-w-[18px] rounded-full bg-ink px-1 text-center text-[12px] font-bold leading-[18px] text-surface"
                  >
                    {waiting}
                  </span>
                )}
              </span>
              {t(`nav.${tab.key}`)}
              {tab.key === 'approve' && waiting > 0 && <span className="sr-only">{t('nav.approveWaiting', { n: waiting })}</span>}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  )
}
