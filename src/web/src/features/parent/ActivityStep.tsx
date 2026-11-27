import { useTranslation } from 'react-i18next'
import { HomeworkCard, PrimaryButton } from './ParentParts'
import { Sticks } from './Sticks'
import { sticksText } from './sticksText'
import type { ParentCatchUp } from './types'

/**
 * P2 Activity at home: the number as sticks (drawn by code, D-07), A3's instruction and household-object option,
 * then the homework page (activity first, C-17). Next never depends on audio (C-05).
 */
export function ActivityStep({ page, onNext }: { page: ParentCatchUp; onNext: () => void }) {
  const { t } = useTranslation()
  const activity = page.activity
  return (
    <div className="flex flex-col gap-5">
      {activity && (
        <section className="card flex flex-col items-center gap-3 text-center">
          <p className="text-display font-extrabold">{activity.number}</p>
          <Sticks n={activity.number} />
          <p className="text-caption text-ink-muted">{sticksText(t, activity.number)}</p>
          {activity.instruction && (
            <p lang={page.language} className="text-body font-semibold">
              {activity.instruction}
            </p>
          )}
          {activity.objectOption && (
            <p lang={page.language} className="text-caption text-ink-muted">
              {activity.objectOption}
            </p>
          )}
        </section>
      )}
      {page.videoUrl && (
        // The lesson's video from the admin page, put in by code (T-103, D-106); optional, so it is a link, not a step.
        <section className="flex flex-col gap-2">
          <p className="text-body">{t('parent.todays.videoIntro')}</p>
          <a href={page.videoUrl} target="_blank" rel="noopener noreferrer" className="btn-secondary flex items-center justify-center">
            {t('parent.todays.watch')}
          </a>
          <p className="text-caption text-ink-muted">{t('parent.todays.videoData')}</p>
        </section>
      )}
      <PrimaryButton onClick={onNext}>{t('parent.activity.next')}</PrimaryButton>
      {page.homework && <HomeworkCard homework={page.homework} teacher={page.teacherName} />}
    </div>
  )
}
