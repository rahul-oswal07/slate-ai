import { useTranslation } from 'react-i18next'
import { Icon, type IconName } from '../../components/Icon'
import type { Level } from './useTodaysAnswers'

// The word is always ink (AAA on the card); the colour is on the border and the icon only, so it is never the only signal.
const LOOK: Record<Level, { icon: IconName; border: string; iconClass: string }> = {
  gotIt: { icon: 'checkCircle', border: 'border-primary', iconClass: 'text-primary-strong' },
  gettingThere: { icon: 'star', border: 'border-warning', iconClass: 'text-warning' },
  needsHelp: { icon: 'helpCircle', border: 'border-status-error', iconClass: 'text-status-error' },
  notAnswered: { icon: 'clock', border: 'border-border', iconClass: 'text-ink-muted' },
}

/**
 * A learner level (D-95): an icon and a word, with a border colour; colour is never the only signal. T-97 gives the
 * four levels their own tokens; until then they reuse the status colours.
 */
export function LevelTag({ level }: { level: Level }) {
  const { t } = useTranslation()
  const look = LOOK[level]
  return (
    <span className={`inline-flex items-center gap-1 rounded-pill border-[1.5px] bg-surface px-2 py-0.5 text-caption font-bold text-ink ${look.border}`}>
      <Icon name={look.icon} size={14} className={`shrink-0 ${look.iconClass}`} />
      {t(`level.${level}`)}
    </span>
  )
}
