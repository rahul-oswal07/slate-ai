import { useTranslation } from 'react-i18next'
import { Icon } from '../../components/Icon'
import { useOutboxCount } from '../../offline/outbox'

/** Real outbox state: "Saved" or "{n} waiting to send" (A-11, NFR-02). */
export function SyncBadge() {
  const { t } = useTranslation()
  const waiting = useOutboxCount()

  return waiting === 0 ? (
    <span className="inline-flex items-center gap-1 rounded-sm bg-status-listened-bg px-2 py-1 text-label text-status-listened">
      <Icon name="check" size={16} />
      {t('sync.saved')}
    </span>
  ) : (
    <span className="inline-flex items-center gap-1 rounded-sm bg-status-queued-bg px-2 py-1 text-label text-status-queued">
      <Icon name="cloudOff" size={16} />
      {t('sync.waiting', { n: waiting })}
    </span>
  )
}
