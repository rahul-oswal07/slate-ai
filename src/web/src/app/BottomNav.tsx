import { useTranslation } from 'react-i18next'
import { NavLink } from 'react-router'
import { Icon, type IconName } from '../components/Icon'

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

  return (
    <nav aria-label={t('nav.label')} className="fixed inset-x-0 bottom-0 z-10 border-t border-border bg-surface">
      <ul className="mx-auto grid max-w-md grid-cols-5">
        {tabs.map((tab) => (
          <li key={tab.key}>
            <NavLink
              to={tab.to}
              end={tab.to === '/'}
              className={({ isActive }) =>
                `relative flex min-h-[56px] flex-col items-center justify-center gap-0.5 px-1 text-center text-caption [word-break:keep-all] ${
                  isActive
                    ? "font-bold text-primary-strong before:absolute before:inset-x-3 before:top-0 before:h-[3px] before:bg-primary before:content-['']"
                    : 'text-ink-muted'
                }`
              }
            >
              <Icon name={tab.icon} size={22} />
              {t(`nav.${tab.key}`)}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  )
}
