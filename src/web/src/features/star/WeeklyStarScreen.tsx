import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { ApiError, NetworkError } from '../../api/client'
import { Icon } from '../../components/Icon'
import { StarGlyph } from '../../components/StarGlyph'
import { useFocusOnMount } from '../../components/useFocusOnMount'
import { useClassId } from '../auth/useCurrentClass'
import { MessageText } from '../evening/MessageText'
import { useApproveWeeklyStar, useWeeklyStarWording } from './useWeeklyStar'

/**
 * T0 step 3 and Settings (T-125, D-118): the weekly star families get on Saturday for a child present every school
 * day, shown filled with a child from her class: the WhatsApp text and the page's two screens. She approves it once;
 * nothing goes until she does (guardrail 3). Shown in her app language, with the Kannada page beside it.
 */
export function WeeklyStarScreen({ mode = 'setup' }: { mode?: 'setup' | 'settings' }) {
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
  const headingRef = useFocusOnMount<HTMLHeadingElement>()
  const classId = useClassId()
  const wording = useWeeklyStarWording(classId)
  const approve = useApproveWeeklyStar(classId)
  const languages = wording.data?.languages ?? []
  const example = languages.find((l) => l.language === i18n.language) ?? languages.find((l) => l.language === 'en')
  const kannada = languages.find((l) => l.language === 'kn')
  const settings = mode === 'settings'
  const done = settings ? '/settings' : '/'

  function approveError() {
    const error = approve.error
    if (error instanceof NetworkError) return t('setup.template.offline')
    if (error instanceof ApiError && error.status === 409) return t('common.conflict')
    return t('setup.template.error')
  }

  return (
    <section aria-labelledby="star-title" className="mx-auto flex max-w-md flex-col gap-3">
      {!settings && <p className="mt-4 text-caption text-ink-muted">{t('setup.step3.progress')}</p>}
      <h1 id="star-title" ref={headingRef} tabIndex={-1} className={`text-title ${settings ? 'mt-4' : ''}`}>
        {t('setup.star.heading')}
      </h1>
      <p className="text-body text-ink-muted">{t('setup.star.lead')}</p>

      {wording.isPending && <div aria-hidden="true" className="h-40 rounded-button border-2 border-border bg-surface" />}

      {wording.isError && !wording.data && (
        <div role="alert" className="rounded-card border-[1.5px] border-edge border-l-4 border-l-error bg-error-bg p-4">
          <p className="text-body">{t('app.loadError')}</p>
          <button
            type="button"
            onClick={() => void wording.refetch()}
            className="mt-3 min-h-target w-full rounded-button border-2 border-border bg-surface text-label"
          >
            {t('common.tryAgain')}
          </button>
        </div>
      )}

      {example && (
        <>
          <div className="card flex flex-col gap-2 p-[14px]">
            <h2 className="flex items-center gap-2 text-label">
              <Icon name="messageCircle" size={18} className="text-primary-strong" />
              {t('setup.star.whatsApp')}
            </h2>
            <MessageText parts={example.whatsApp} lang={example.language} />
          </div>
          <div className="card flex flex-col gap-2 p-[14px]">
            <h2 className="text-label">{t('setup.star.page')}</h2>
            <MessageText parts={example.message} lang={example.language} />
            <p className="flex items-center gap-2 text-caption text-ink-muted">
              <StarGlyph size={16} />
              {t('setup.star.thenBadge')}
            </p>
            <MessageText parts={example.badge} lang={example.language} />
          </div>
        </>
      )}

      {kannada && kannada !== example && (
        <div className="card flex flex-col gap-2 p-[14px]">
          <h2 className="text-label">{t('setup.star.kannadaVersion')}</h2>
          <MessageText parts={kannada.message} lang="kn" />
          <MessageText parts={kannada.badge} lang="kn" />
        </div>
      )}

      {wording.data && (
        <div className="sticky bottom-0 mt-1 flex flex-col gap-2 border-t border-divider bg-canvas pb-3 pt-3">
          {approve.isError && (
            <p role="alert" className="flex items-start gap-2 text-caption text-error">
              <Icon name="alert" size={18} className="mt-0.5 shrink-0" />
              {approveError()}
            </p>
          )}
          {wording.data.approved ? (
            <>
              <output className="flex items-center gap-2 text-body text-ink">
                <Icon name="checkCircle" className="shrink-0 text-primary-strong" />
                {t('setup.star.approved')}
              </output>
              <button
                type="button"
                onClick={() => void navigate(done, { replace: true })}
                className="min-h-target-primary w-full rounded-button bg-primary font-extrabold text-label text-on-primary"
              >
                {t(settings ? 'settings.backToSettings' : 'common.goToday')}
              </button>
            </>
          ) : (
            <>
              <button
                type="button"
                onClick={() => approve.mutate(wording.data.version)}
                disabled={approve.isPending}
                className="min-h-target-primary w-full rounded-button bg-primary font-extrabold text-label text-on-primary disabled:opacity-70"
              >
                {t('setup.star.approve')}
              </button>
              <button
                type="button"
                onClick={() => void navigate(done, { replace: true })}
                className="min-h-target w-full text-label text-primary-strong"
              >
                {t('setup.template.notNow')}
              </button>
            </>
          )}
        </div>
      )}
    </section>
  )
}
