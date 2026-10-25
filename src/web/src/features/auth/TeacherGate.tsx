import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Navigate, Outlet } from 'react-router'
import { useMe } from './useMe'

/**
 * Routes a teacher to the right place: signed out → /login; first run → /welcome (until
 * Teacher.ConsentAt is set); otherwise the app. `need` says which of the last two this route is.
 */
export function TeacherGate({ need }: { need: 'setup' | 'ready' }) {
  const { t } = useTranslation()
  const me = useMe()
  // Whether setup was already complete when this gate first had a teacher. Finishing setup on /welcome flips it while
  // the teacher is still here: the screen then navigates itself, and this gate must not race it to Today.
  const [completeOnArrival, setCompleteOnArrival] = useState<boolean | null>(null)
  if (completeOnArrival === null && me.data) {
    setCompleteOnArrival(me.data.setupComplete)
  }

  if (me.isPending) {
    return null
  }

  if (me.isError) {
    return (
      <div role="alert" className="rounded-card border-[1.5px] border-edge border-l-4 border-l-error bg-error-bg p-4">
        <p className="text-body">{t('app.loadError')}</p>
        <button
          type="button"
          onClick={() => void me.refetch()}
          className="mt-3 min-h-target w-full rounded-button border-2 border-border bg-surface text-label"
        >
          {t('common.tryAgain')}
        </button>
      </div>
    )
  }

  if (me.data === null) {
    return <Navigate to="/login" replace />
  }
  if (need === 'ready' && !me.data.setupComplete) {
    return <Navigate to="/welcome" replace />
  }
  if (need === 'setup' && me.data.setupComplete && completeOnArrival !== false) {
    return <Navigate to="/" replace />
  }
  return <Outlet />
}
