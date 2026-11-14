import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import type { TodaysMessage } from '../approve/todaysTypes'

/**
 * Today's message on Today (T-108): until she approves it, it is waiting for her, with the one button that takes her to
 * Approve (also while it is being written, or when writing it failed: Try again is there). Once approved it says so.
 * After the send nothing shows here: the answers card takes over (T-92).
 */
export function TodaysMessageCard({ message }: { message: TodaysMessage }) {
  const { t } = useTranslation()
  if (message.sent) {
    return null
  }

  const approved = message.status === 'approved'
  return (
    <section aria-labelledby="todays-card-title" className="border-t-2 border-dashed border-chalk pt-3">
      <h2 id="todays-card-title" className="text-label text-primary-strong">
        {t(approved ? 'today.todays.approved' : 'today.todays.waiting')}
      </h2>
      {approved ? (
        <Link to="/approve" className="btn-link mt-1">
          {t('today.todays.see')}
        </Link>
      ) : (
        <>
          <p className="mt-1 text-body text-ink">{t('today.todays.waitingHint')}</p>
          <Link to="/approve" className="btn-primary mt-3 flex items-center justify-center">
            {t('today.todays.check')}
          </Link>
        </>
      )}
    </section>
  )
}
