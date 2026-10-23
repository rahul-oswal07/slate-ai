import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { ParentHeader, StopControl, StoppedView } from './ParentParts'
import { ReasonView } from './ReasonView'
import type { ParentEvening } from './types'

/**
 * An evening-message link opened on its own (the demo chat shows it as a bubble, T-18): the text, "Is everything OK?
 * Tell us" (FR-21) and the stop link. The reason page is `?view=reason`, so the phone's Back returns here.
 */
export function EveningView({ token, message }: { token: string; message: ParentEvening }) {
  const { t } = useTranslation()
  const [params, setParams] = useSearchParams()

  if (message.stopped) {
    return <StoppedView token={token} childName={message.childName} />
  }

  if (params.get('view') === 'reason') {
    return <ReasonView token={token} message={message} onDone={() => setParams({}, { replace: true })} />
  }

  const openReason = () => setParams({ view: 'reason' })
  return (
    <>
      <ParentHeader title={t('parent.evening.title', { teacher: message.teacherName })} />
      <div className="flex flex-col gap-4 px-4 py-4">
        <p lang={message.language} className="rounded-md border border-border bg-surface p-4 text-body">
          {message.text}
        </p>
        {message.reason ? (
          <p className="flex items-center justify-between gap-3 text-body">
            {t('parent.reason.given')}
            <button
              type="button"
              onClick={openReason}
              className="min-h-target rounded-md px-3 text-label text-primary-strong underline"
            >
              {t('parent.reason.change')}
            </button>
          </p>
        ) : (
          <button
            type="button"
            onClick={openReason}
            className="min-h-target w-full rounded-md border border-primary bg-panel px-4 text-label text-primary-strong"
          >
            {t('parent.reason.entry')}
          </button>
        )}
        <StopControl token={token} childName={message.childName} />
      </div>
    </>
  )
}
