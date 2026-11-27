import { useTranslation } from 'react-i18next'
import { Icon } from '../../components/Icon'
import { LEVEL_LOOK } from './levelLook'
import type { Level } from './useTodaysAnswers'

/**
 * A learner level, the same on Today, Missed lessons, the child view, Back today and Week (T-97, D-95, D-108): icon +
 * word in the level's own colour token, so colour is never the only signal. `label` puts a lesson name before the word
 * (one chip per lesson on the child view, T-98).
 */
export function LevelTag({ level, label }: { level: Level; label?: string }) {
  const { t } = useTranslation()
  const look = LEVEL_LOOK[level]
  const word = t(`level.${level}`)
  return (
    <span
      data-level={level}
      className={`inline-flex items-center gap-1 rounded-pill border-[1.5px] px-2 py-0.5 text-caption font-bold ${look.tag}`}
    >
      <Icon name={look.icon} size={14} className="shrink-0" />
      {label ? `${label} · ${word}` : word}
    </span>
  )
}
