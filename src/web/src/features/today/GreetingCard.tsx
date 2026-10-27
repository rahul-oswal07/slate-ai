import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { Icon } from '../../components/Icon'
import { useMe } from '../auth/useMe'

/**
 * Offered on Today until she has recorded her greeting (T0 step 1): families then hear her own voice first in every
 * message. For teachers who were set up before the greeting existed, or who continued without one (D-34).
 */
export function GreetingCard() {
  const { t } = useTranslation()
  const me = useMe()
  if (!me.data?.setupComplete || me.data.greeting) {
    return null
  }

  return (
    <section aria-labelledby="greeting-card-title" className="card flex flex-col gap-3 p-[14px]">
      <div className="flex items-start gap-3">
        <span aria-hidden="true" className="flex size-10 shrink-0 items-center justify-center rounded-full bg-panel text-primary-strong">
          <Icon name="mic" size={22} />
        </span>
        <div className="min-w-0 flex-1">
          <h2 id="greeting-card-title" className="text-label">
            {t('today.greeting.title')}
          </h2>
          <p className="text-caption text-ink-muted">{t('today.greeting.body')}</p>
        </div>
      </div>
      {/* btn-secondary is full width (w-full), so it sits under the text rather than beside it. */}
      <Link to="/setup/greeting" className="btn-secondary flex items-center justify-center">
        {t('today.greeting.action')}
      </Link>
    </section>
  )
}
