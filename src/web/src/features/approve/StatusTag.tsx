import { useTranslation } from 'react-i18next'
import { Icon, type IconName } from '../../components/Icon'
import type { PackageStatus } from './types'

// D-35 tag colours; status is always icon + word, never colour alone (T3 acceptance 3).
const styles: Record<PackageStatus, { icon: IconName; className: string }> = {
  ready: { icon: 'checkCircle', className: 'bg-status-listened-bg text-status-listened' },
  needsReview: { icon: 'alert', className: 'bg-status-needs-ear-bg text-status-needs-ear' },
  approved: { icon: 'check', className: 'bg-status-caught-up-bg text-status-caught-up' },
  generating: { icon: 'clock', className: 'bg-status-queued-bg text-status-queued' },
  sent: { icon: 'send', className: 'bg-status-sent-bg text-status-sent' },
  failed: { icon: 'alert', className: 'bg-error-bg text-error' },
}

export function StatusTag({ status }: { status: PackageStatus }) {
  const { t } = useTranslation()
  const style = styles[status]
  return (
    <span className={`inline-flex min-h-8 items-center gap-1.5 rounded-pill px-3 py-1 text-caption font-bold ${style.className}`}>
      <Icon name={style.icon} size={16} />
      {t(`approve.status.${status}`)}
    </span>
  )
}
