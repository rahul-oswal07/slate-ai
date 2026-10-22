import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { findLesson, lessonName } from '../lessons/lessonText'
import type { Chapter } from '../lessons/types'

/**
 * "Added to missed lessons: N children" after the lesson is confirmed (T1 `done`, D-33). N is the absent
 * children for the day, counted in code; not shown when nobody is absent (D-54).
 */
export function AddedCard({ absent, lessonIds, chapters }: { absent: number; lessonIds: string[]; chapters: Chapter[] }) {
  const { t, i18n } = useTranslation()
  const names = lessonIds.map((id) => {
    const found = findLesson(chapters, id)
    return found ? lessonName(found.lesson, i18n.language) : id
  })

  return (
    <section aria-labelledby="added-title" className="rounded-md border border-border bg-surface p-[14px]">
      <h2 id="added-title" className="text-label">
        {t('today.added.title', { n: absent })}
      </h2>
      <p className="mt-1 text-body text-ink-muted">{t('today.added.sub', { lessons: names.join(' + ') })}</p>
      <Link
        to="/missed"
        className="mt-2 flex min-h-target w-full items-center justify-center text-label text-primary-strong"
      >
        {t('today.added.see', { n: absent })}
      </Link>
    </section>
  )
}
