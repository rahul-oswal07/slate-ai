import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { istToday } from '../today/schoolTime'
import { AdminPage, AdminQuery } from './AdminPage'
import { ClipPlayer } from './ClipPlayer'
import { type AdminLessonAudio, adminTime, useAdminLessonAudio } from './useAdmin'

type Shown = 'sent' | 'approved' | 'ready' | 'generating' | 'failed'

const CHIP: Record<Shown, string> = {
  sent: 'bg-status-caught-up-bg text-status-caught-up',
  approved: 'bg-status-sent-bg text-status-sent',
  ready: 'bg-status-needs-buddy-bg text-status-needs-buddy',
  generating: 'bg-status-listened-bg text-status-listened',
  failed: 'bg-error-bg text-error',
}

/** What happened to it: sent to at least one family, or where it stopped. */
function shown(audio: AdminLessonAudio): Shown {
  return audio.families.length > 0 ? 'sent' : audio.status
}

/**
 * /admin/lesson-audio#key=… (T-67, D-83): one day's optional lesson audio (T-62): how many families got it, and for
 * each class and language the lesson, who approved it, the clip and its words, and the families it went to. Audio
 * that never went out is listed too, with where it stopped.
 */
export function LessonAudioAdminScreen() {
  const { t } = useTranslation()
  return <AdminPage title={t('admin.lessonAudio.title')}>{(key) => <LessonAudioDay adminKey={key} />}</AdminPage>
}

function LessonAudioDay({ adminKey: key }: { adminKey: string }) {
  const { t } = useTranslation()
  const dateId = useId()
  const [date, setDate] = useState(() => istToday())
  const day = useAdminLessonAudio(key, date)

  return (
    <>
      <div className="flex flex-col gap-1">
        <label htmlFor={dateId} className="text-label">
          {t('admin.lessonAudio.date')}
        </label>
        <input
          id={dateId}
          type="date"
          value={date}
          onChange={(event) => setDate(event.target.value)}
          className="min-h-target rounded-button border-2 border-border bg-surface px-3 text-body text-ink"
        />
      </div>
      {date !== '' && (
        <AdminQuery query={day}>
          {(data) => (
            <>
              <p className="text-body-lg font-semibold">{t('admin.lessonAudio.sent', { count: data.sentCount })}</p>
              {data.audios.length === 0 ? (
                <p className="text-body text-ink-muted">{t('admin.lessonAudio.empty')}</p>
              ) : (
                <ul className="flex flex-col gap-3">
                  {data.audios.map((audio) => (
                    <AudioCard key={audio.id} adminKey={key} audio={audio} />
                  ))}
                </ul>
              )}
            </>
          )}
        </AdminQuery>
      )}
    </>
  )
}

function AudioCard({ adminKey: key, audio }: { adminKey: string; audio: AdminLessonAudio }) {
  const { t, i18n } = useTranslation()
  const status = shown(audio)
  const language = t(`language.${audio.language}`)

  return (
    <li className="flex flex-col gap-1 rounded-card border-[1.5px] border-edge bg-surface p-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="text-label">
          {audio.classLabel} · {language}
        </p>
        <span className={`chip ${CHIP[status]}`}>{t(`admin.lessonAudio.status.${status}`)}</span>
      </div>
      {audio.lessons.length > 0 && <p className="text-body">{audio.lessons.join(', ')}</p>}
      {status === 'failed' && audio.verdict && (
        <p className="text-caption text-error">{t('admin.lessonAudio.reason', { reason: audio.verdict })}</p>
      )}
      {audio.approvedBy && audio.approvedAt && (
        <p className="text-caption text-ink-muted">
          {t('admin.lessonAudio.approved', { name: audio.approvedBy, date: adminTime(audio.approvedAt, i18n.language) })}
        </p>
      )}
      {audio.clipId && (
        <>
          <ClipPlayer
            adminKey={key}
            url={`/api/admin/lesson-audio/${audio.clipId}/audio`}
            playLabel={t('admin.lessonAudio.play', { language, seconds: audio.seconds })}
            playerLabel={t('admin.lessonAudio.player', { language })}
          />
          {audio.transcript && (
            <details className="mt-1">
              <summary className="inline-flex min-h-target cursor-pointer items-center text-label text-primary-strong">
                {t('admin.lessonAudio.words')}
              </summary>
              <p className="text-body whitespace-pre-line" lang={audio.language}>
                {audio.transcript}
              </p>
            </details>
          )}
        </>
      )}
      {audio.families.length > 0 && (
        <>
          <p className="mt-2 text-label">{t('admin.lessonAudio.families', { count: audio.families.length })}</p>
          <ul className="flex flex-col divide-y divide-divider">
            {audio.families.map((family) => (
              <li key={`${family.rollNo}-${family.sentAt}`} className="flex justify-between gap-2 py-1 text-body">
                <span>{t('admin.lessonAudio.family', { name: family.childName, rollNo: family.rollNo })}</span>
                <span className="text-caption text-ink-muted">{adminTime(family.sentAt, i18n.language)}</span>
              </li>
            ))}
          </ul>
        </>
      )}
    </li>
  )
}
