import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { ApiError, NetworkError } from '../../api/client'
import { Icon } from '../../components/Icon'
import { useFocusOnMount } from '../../components/useFocusOnMount'
import { useMe } from '../auth/useMe'
import { MessageText } from '../evening/MessageText'
import { useApproveTemplate, useMessageTemplate } from '../evening/useEvening'

/**
 * T0 step 2 (docs/design/T0-setup.md, state `2`): the evening message on a day of absence, shown filled with an
 * example. She approves it once (FR-20, D-21); "Not now" saves nothing and no message is sent until she approves.
 * The Telugu version is shown as text: its audio is stitched clips, which are roadmap (D-70).
 */
export function EveningTemplateScreen() {
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
  const me = useMe()
  const headingRef = useFocusOnMount<HTMLHeadingElement>()
  const classId = me.data?.classId ?? ''
  const template = useMessageTemplate(classId)
  const approve = useApproveTemplate(classId)
  const languages = template.data?.languages ?? []
  const uiLanguage = i18n.language === 'kn' ? 'kn' : 'en'
  const example = languages.find((l) => l.language === uiLanguage) ?? languages.find((l) => l.language === 'kn')
  const telugu = languages.find((l) => l.language === 'te')
  const approved = template.data?.status === 'approved'

  function approveError() {
    const error = approve.error
    if (error instanceof NetworkError) return t('setup.template.offline')
    if (error instanceof ApiError && error.status === 409) return t('common.conflict')
    return t('setup.template.error')
  }

  return (
    <section aria-labelledby="template-title" className="mx-auto flex max-w-md flex-col gap-3">
      <p className="mt-4 text-caption text-ink-muted">{t('setup.step2.progress')}</p>
      <h1 id="template-title" ref={headingRef} tabIndex={-1} className="text-title">
        {t('setup.template.heading')}
      </h1>
      <p className="text-body text-ink-muted">{t('setup.template.lead')}</p>

      {template.isPending && <div aria-hidden="true" className="h-40 rounded-button border-2 border-border bg-surface" />}

      {template.isError && !template.data && (
        <div role="alert" className="rounded-card border-[1.5px] border-edge border-l-4 border-l-error bg-error-bg p-4">
          <p className="text-body">{t('app.loadError')}</p>
          <button
            type="button"
            onClick={() => void template.refetch()}
            className="mt-3 min-h-target w-full rounded-button border-2 border-border bg-surface text-label"
          >
            {t('common.tryAgain')}
          </button>
        </div>
      )}

      {example && (
        <div className="flex flex-col gap-3 rounded-card border-[1.5px] border-edge bg-surface p-[14px]">
          <span className="inline-flex items-center gap-2 self-start rounded-full border border-border bg-panel px-3 py-1 text-caption text-primary-strong">
            <Icon name="mic" size={16} />
            {t('setup.template.greetingChip')}
          </span>
          <MessageText parts={example.example} lang={example.language} />
          <p className="text-caption text-ink-muted">{t('setup.template.audioNote')}</p>
        </div>
      )}

      {telugu && telugu !== example && (
        <div className="rounded-card border-[1.5px] border-edge bg-surface p-[14px]">
          <h2 className="mb-2 text-label">{t('setup.template.teluguVersion')}</h2>
          <MessageText parts={telugu.example} lang="te" />
        </div>
      )}

      {template.data && (
        <div className="sticky bottom-0 mt-1 flex flex-col gap-2 border-t border-divider bg-canvas pb-3 pt-3">
          {approve.isError && (
            <p role="alert" className="flex items-start gap-2 text-caption text-error">
              <Icon name="alert" size={18} className="mt-0.5 shrink-0" />
              {approveError()}
            </p>
          )}
          {approved ? (
            <>
              <output className="flex items-center gap-2 text-body text-ink">
                <Icon name="checkCircle" className="shrink-0 text-primary-strong" />
                {t('setup.template.approved')}
              </output>
              <button
                type="button"
                onClick={() => void navigate('/', { replace: true })}
                className="min-h-target-primary w-full rounded-button bg-primary font-extrabold text-label text-on-primary"
              >
                {t('common.goToday')}
              </button>
            </>
          ) : (
            <>
              <button
                type="button"
                onClick={() => approve.mutate(template.data.version)}
                disabled={approve.isPending}
                className="min-h-target-primary w-full rounded-button bg-primary font-extrabold text-label text-on-primary disabled:opacity-70"
              >
                {t('setup.template.approve')}
              </button>
              <button
                type="button"
                onClick={() => void navigate('/', { replace: true })}
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
