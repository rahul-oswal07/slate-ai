import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ApiError, NetworkError } from '../../api/client'
import { SegmentPlayer } from '../approve/SegmentPlayer'
import { StatusTag } from '../approve/StatusTag'
import type { EveningDay, LessonAudioItem } from '../evening/types'
import { useLessonAudio, useLessonAudioActions } from '../evening/useLessonAudio'

/**
 * "Also send today's lesson as audio" (T-62), under tonight's card. Unticked each day. Ticking makes one clip per
 * language of tonight's families; she listens to each and approves it (guardrail 3) before it goes with the 6 pm message.
 * The plain evening message is the same with or without it. Read-only once the messages have gone.
 */
export function LessonAudioCard({ classId, date, evening }: { classId: string; date: string; evening: EveningDay }) {
  const { t } = useTranslation()
  const offered = evening.templateApproved && evening.lessonConfirmed && evening.families > 0
  const audio = useLessonAudio(classId, date, offered || evening.sent > 0)
  const { tick, approve, regenerate } = useLessonAudioActions(classId, date)
  // Which clip she has listened to all the way through, per language: a new clip must be listened to again.
  const [listened, setListened] = useState<Record<string, string>>({})
  const [needsListen, setNeedsListen] = useState<string | null>(null)
  const day = audio.data
  if (!day || (!day.available && !day.enabled)) {
    return null
  }

  const error = tick.error ?? approve.error ?? regenerate.error
  const busy = tick.isPending || approve.isPending || regenerate.isPending

  function approveClip(item: LessonAudioItem) {
    if (!item.clipId) {
      return
    }
    if (listened[item.language] !== item.clipId) {
      setNeedsListen(item.language)
      return
    }
    setNeedsListen(null)
    approve.mutate({ language: item.language, clipId: item.clipId })
  }

  return (
    <section aria-label={t('today.tonight.audio.label')} className="border-t-2 border-dashed border-chalk pt-3">
      <label className="flex items-start gap-3 text-label">
        <input
          type="checkbox"
          checked={day.enabled}
          disabled={day.sent || busy}
          onChange={(event) => tick.mutate(event.target.checked)}
          aria-describedby="lesson-audio-hint"
          className="mt-0.5 size-6 shrink-0 accent-primary"
        />
        {t('today.tonight.audio.label')}
      </label>
      <p id="lesson-audio-hint" className="ml-9 text-caption text-ink-muted">
        {day.sent ? t('today.tonight.audio.sent') : t('today.tonight.audio.hint')}
      </p>

      {error && (
        <p role="alert" className="mt-2 text-caption text-error">
          {error instanceof NetworkError
            ? t('today.tonight.audio.offline')
            : error instanceof ApiError && error.status === 409
              ? t('common.conflict')
              : t('today.tonight.audio.error')}
        </p>
      )}

      {day.enabled && (
        <ul className="mt-3 flex flex-col gap-3">
          {day.items.map((item) => (
            <li key={item.language} className="rounded-card border-[1.5px] border-edge bg-surface p-3">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <p className="text-label">
                  {t(`approve.in.${item.language}`)}
                  <span className="ml-2 text-caption font-normal text-ink-muted">
                    {t('today.tonight.audio.families', { count: item.children })}
                  </span>
                </p>
                <StatusTag status={item.status} />
              </div>

              {item.status === 'generating' && (
                <output className="mt-2 block text-caption text-ink-muted">{t('today.tonight.audio.making')}</output>
              )}

              {item.status === 'failed' && (
                <>
                  <p role="alert" className="mt-2 text-caption text-error">
                    {t('today.tonight.audio.failed')}
                  </p>
                  <button
                    type="button"
                    disabled={busy || day.sent}
                    onClick={() => regenerate.mutate(item.language)}
                    className="btn-secondary mt-2 w-full"
                  >
                    {t('common.tryAgain')}
                  </button>
                </>
              )}

              {item.clipId && item.segments.length > 0 && (
                <div className="mt-2 flex flex-col gap-2">
                  <p className="text-caption text-ink-muted">{t('ai.label')}</p>
                  <SegmentPlayer
                    key={item.clipId}
                    segments={item.segments}
                    onAllEnded={() => {
                      setListened((current) => ({ ...current, [item.language]: item.clipId ?? '' }))
                      setNeedsListen(null)
                    }}
                  />
                  {item.transcript && (
                    <details>
                      <summary className="cursor-pointer text-caption font-bold text-primary-strong">
                        {t('today.tonight.audio.readWords')}
                      </summary>
                      <p lang={item.language} className="mt-1 text-body">
                        {item.transcript}
                      </p>
                    </details>
                  )}
                </div>
              )}

              {item.status === 'ready' && (
                <>
                  {needsListen === item.language && (
                    <p role="alert" className="mt-2 text-caption text-error">
                      {t('today.tonight.audio.listenFirst')}
                    </p>
                  )}
                  <button
                    type="button"
                    disabled={busy}
                    onClick={() => approveClip(item)}
                    className="min-h-target-primary mt-2 w-full rounded-button bg-primary font-extrabold text-label text-on-primary disabled:opacity-70"
                  >
                    {t('today.tonight.audio.approve')}
                  </button>
                  <button
                    type="button"
                    disabled={busy}
                    onClick={() => regenerate.mutate(item.language)}
                    className="btn-link mt-1"
                  >
                    {t('today.tonight.audio.newOne')}
                  </button>
                </>
              )}

              {item.status === 'approved' && (
                <p className="mt-2 text-caption text-ink">{t('today.tonight.audio.approved')}</p>
              )}
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
