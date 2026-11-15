import { useTranslation } from 'react-i18next'
import { Icon } from '../../components/Icon'
import type { CurriculumLesson } from './types'

/**
 * "Book exercises" (T-128): the lesson has the book's own exercise numbers (D-93). Shown in the picker, on the confirm
 * screen and on Today's lesson card. An icon and a word, ink on the card (AAA); nothing for a lesson without them.
 */
export function ExercisesTag({ lesson }: { lesson: Pick<CurriculumLesson, 'hasExercises'> }) {
  const { t } = useTranslation()
  if (!lesson.hasExercises) {
    return null
  }
  return (
    <span className="mt-1 inline-flex w-fit items-center gap-1 rounded-pill border-[1.5px] border-primary bg-surface px-2 py-0.5 text-caption font-bold text-ink">
      <Icon name="book" size={14} className="shrink-0 text-primary-strong" />
      {t('lesson.bookExercises')}
    </span>
  )
}
