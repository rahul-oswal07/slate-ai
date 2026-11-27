import type { IconName } from '../../components/Icon'
import type { Level } from './useTodaysAnswers'

/**
 * The look of each learner level (T-97, D-95, D-108): its own colour token, an icon and a word. The word and icon are in
 * the level's colour on its fill, with a border in the same colour; each pair is AAA in light and dark (tokens.test.ts).
 * `text` is the colour alone, for an icon or word straight on a card (also AAA there). Tailwind needs the class names
 * written out in full.
 */
export const LEVEL_LOOK: Record<Level, { icon: IconName; tag: string; text: string }> = {
  gotIt: {
    icon: 'checkCircle',
    tag: 'border-level-got-it bg-level-got-it-bg text-level-got-it',
    text: 'text-level-got-it',
  },
  needsHelp: {
    icon: 'helpCircle',
    tag: 'border-level-needs-help bg-level-needs-help-bg text-level-needs-help',
    text: 'text-level-needs-help',
  },
  notAnswered: {
    icon: 'clock',
    tag: 'border-level-not-answered bg-level-not-answered-bg text-level-not-answered',
    text: 'text-level-not-answered',
  },
}
