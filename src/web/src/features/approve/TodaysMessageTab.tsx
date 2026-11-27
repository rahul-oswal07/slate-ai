import { type ReactNode, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { Icon } from '../../components/Icon'
import { useAnnounce } from '../../components/useAnnounce'
import { findLesson, lessonName, pageText } from '../lessons/lessonText'
import { useCurriculum } from '../lessons/useLessons'
import { useSchoolToday } from '../today/schoolDate'
import { SECTION_ORDER } from './sectionKinds'
import { type EditField, SectionActions, SectionCard, SectionEditForm } from './Sections'
import type { TodaysMessage, TodaysSectionKind } from './todaysTypes'
import {
  editProblem,
  useApproveTodays,
  useEditSection,
  useRetryTodays,
  useSetPicture,
  useSetRemoved,
  useTodaysMessage,
} from './useTodaysMessage'

type Editable = 'concept' | 'activity' | 'note'
const SECTIONS: TodaysSectionKind[] = SECTION_ORDER
const PROMISED: TodaysSectionKind[] = ['concept', 'activity', 'picture']

/**
 * The sections this message has: the video only when its lesson has a link (T-94), the note only once she adds one, the
 * picture question only when the lesson has the book's exercises (T-139).
 */
const present = (message: TodaysMessage) =>
  SECTIONS.filter(
    (kind) =>
      (kind !== 'video' || message.video !== null) &&
      (kind !== 'note' || message.note !== null) &&
      (kind !== 'picture' || message.picture !== null),
  )

/**
 * Approve → Today's message (T-89, D-91, D-92): the day's message as sections, one open at a time. She reads and edits
 * the Kannada; every save is a new version that runs the checks again and clears her tick. The numbers come from code:
 * she can only choose another pair from the book. Nothing goes until she ticks and approves this version (guardrail 3).
 * After Approve she is back on Today (T-108).
 */
export function TodaysMessageTab({ classId }: { classId: string }) {
  const { t } = useTranslation()
  const date = useSchoolToday()
  const query = useTodaysMessage(classId, date)
  const retry = useRetryTodays(classId, date)

  if (query.isPending) {
    return <div aria-hidden="true" className="h-40 rounded-button border-2 border-border bg-surface" />
  }

  if (query.isError) {
    return (
      <div role="alert" className="rounded-card border-[1.5px] border-edge border-l-4 border-l-error bg-error-bg p-4">
        <p className="text-body">{t('app.loadError')}</p>
        <button type="button" onClick={() => void query.refetch()} className="btn-secondary mt-3">
          {t('common.tryAgain')}
        </button>
      </div>
    )
  }

  const message = query.data
  if (!message) {
    return (
      <div className="card flex flex-col gap-3 p-[14px]">
        <p className="text-body">{t('todays.none')}</p>
        <Link to="/" className="btn-secondary flex items-center justify-center">
          {t('todays.toToday')}
        </Link>
      </div>
    )
  }

  if (message.status === 'generating') {
    return (
      <output className="card flex items-center gap-3 p-[14px] text-body">
        <Icon name="clock" size={20} className="shrink-0 text-ink-muted" />
        {message.version === 0 ? t('todays.writing') : t('todays.rewriting')}
      </output>
    )
  }

  if (message.status === 'failed' && message.languages.length === 0) {
    return (
      <div role="alert" className="flex flex-col gap-3 rounded-card border-[1.5px] border-edge border-l-4 border-l-error bg-error-bg p-4">
        <p className="text-body">{t('todays.failed')}</p>
        <button type="button" disabled={retry.isPending} onClick={() => retry.mutate()} className="btn-secondary">
          {t('common.tryAgain')}
        </button>
      </div>
    )
  }

  return <Editor key={message.date} classId={classId} date={date} message={message} />
}

function Editor({ classId, date, message }: { classId: string; date: string; message: TodaysMessage }) {
  const { t, i18n } = useTranslation()
  const announce = useAnnounce()
  const navigate = useNavigate()
  const chapters = useCurriculum().data?.chapters ?? []
  const approve = useApproveTodays(classId, date)
  const retry = useRetryTodays(classId, date)
  const [open, setOpen] = useState<TodaysSectionKind | null>('concept')
  // The tick belongs to the version she read: a new version (any edit, D-92) shows it unticked.
  const [tickedVersion, setTickedVersion] = useState<number | null>(null)
  const ticked = tickedVersion === message.version
  const readOnly = message.sent || message.status === 'approved'
  const kn = message.languages.find((l) => l.language === 'kn')
  // T-117: "Approve and send now" runs the evening send at once; the second button leaves it for 6 pm. Either way she is
  // back on Today (T-108). If send now couldn't go (attendance or the evening wording missing), Today says what's missing.
  const approveThis = (send: boolean) =>
    approve.mutate(
      { version: message.version, send },
      {
        onSuccess: (latest) => {
          announce(t(latest.sent ? 'todays.sent' : send ? 'todays.notSentYet' : 'todays.approved'))
          void navigate('/')
        },
      },
    )
  const others = message.languages.filter((l) => l.language !== 'kn' && l.families > 0)
  const lessons = message.competencyIds
    .map((id) => findLesson(chapters, id))
    .filter((l): l is NonNullable<typeof l> => l !== null)

  return (
    <section aria-labelledby="todays-title" className="flex flex-col gap-3">
      <div className="card flex flex-col gap-1 p-[14px]">
        <h2 id="todays-title" className="text-label">
          {lessons.map((l) => lessonName(l.lesson, i18n.language)).join(' + ') || t('todays.title')}
        </h2>
        {lessons[0] && <p className="text-caption text-ink-muted">{pageText(t, lessons[0].lesson.textbookPage)}</p>}
        <p className="text-caption text-ink-muted">
          {t('todays.version', { n: message.version })}
          {message.edited && ` · ${t('todays.edited')}`}
        </p>
      </div>

      {message.status === 'failed' && (
        <div role="alert" className="flex flex-col gap-2 rounded-card border-[1.5px] border-edge border-l-4 border-l-error bg-error-bg p-3">
          <p className="text-body">{t('todays.failedEdit')}</p>
          <button type="button" disabled={retry.isPending} onClick={() => retry.mutate()} className="btn-secondary">
            {t('common.tryAgain')}
          </button>
        </div>
      )}

      <ul className="flex flex-col gap-2">
        {SECTIONS.filter((kind) => (kind !== 'video' || message.video !== null) && (kind !== 'picture' || message.picture !== null)).map((kind) => (
          <li key={kind}>
            <Section
              kind={kind}
              classId={classId}
              date={date}
              message={message}
              readOnly={readOnly || message.status !== 'ready'}
              open={open === kind}
              onToggle={() => setOpen(open === kind ? null : kind)}
            >
              {kind === 'concept' && kn && (
                <>
                  <p className="text-body">{kn.concept}</p>
                  {kn.conceptClipUrl && (
                    // eslint-disable-next-line jsx-a11y/media-has-caption -- the words are shown just above
                    <audio controls preload="none" src={kn.conceptClipUrl} className="mt-2 w-full" aria-label={t('todays.listen')} />
                  )}
                </>
              )}
              {kind === 'activity' && kn && (
                <>
                  <ol className="flex list-decimal flex-col gap-1 pl-5 text-body">
                    {kn.steps.map((step) => (
                      <li key={step}>{step}</li>
                    ))}
                  </ol>
                  {kn.activityClipUrl && (
                    // eslint-disable-next-line jsx-a11y/media-has-caption -- the steps are shown just above
                    <audio controls preload="none" src={kn.activityClipUrl} className="mt-2 w-full" aria-label={t('todays.listenActivity')} />
                  )}
                </>
              )}
              {kind === 'picture' && message.picture && (
                <p className="text-body">
                  {message.picture.kind === 'compare'
                    ? t('todays.pictureQuestion', { a: message.picture.a, b: message.picture.b })
                    : t('todays.pictureSum', {
                        sum: sumText(message.picture.kind, message.picture.a, message.picture.b),
                        choices: (message.picture.options ?? []).join(' · '),
                      })}{' '}
                  <span className="text-caption text-ink-muted">{t('todays.pictureAnswer', { answer: message.picture.answer })}</span>
                </p>
              )}
              {kind === 'video' && message.video && (
                // Chosen by code from the admin page's link for the lesson (T-94, D-106); she can remove it.
                <p className="flex flex-col gap-1">
                  <a href={message.video.url} target="_blank" rel="noopener noreferrer" className="btn-link w-fit">
                    {t('todays.video.open')}
                  </a>
                  <span className="break-all text-caption text-ink-muted">{message.video.url}</span>
                </p>
              )}
              {kind === 'note' && message.note && <NoteText note={message.note} />}
            </Section>
          </li>
        ))}
      </ul>

      {others.length > 0 && (
        <p className="text-caption text-ink-muted">
          {others.map((l) => t('todays.alsoIn', { language: t(`missed.language.${l.language}`), count: l.families })).join(' · ')}
        </p>
      )}

      {message.sent ? (
        <output className="flex items-start gap-2 rounded-card border-[1.5px] border-edge border-l-4 border-l-primary bg-surface p-[14px] text-body">
          <Icon name="send" size={20} className="mt-0.5 shrink-0 text-primary-strong" />
          {t('todays.sent')}
        </output>
      ) : message.status === 'approved' ? (
        <output className="rounded-card border-[1.5px] border-edge border-l-4 border-l-primary bg-surface p-[14px] text-body">
          {t('todays.approved')}
        </output>
      ) : (
        message.status === 'ready' && (
          <div className="flex flex-col gap-2 border-t border-divider pt-3">
            <label className="flex min-h-target items-center gap-3 text-body">
              <input
                type="checkbox"
                className="size-6 accent-primary"
                checked={ticked}
                onChange={(e) => setTickedVersion(e.target.checked ? message.version : null)}
              />
              {t('todays.tick')}
            </label>
            {approve.isError && (
              <p role="alert" className="text-caption text-error">
                {t('todays.changed')}
              </p>
            )}
            <button
              type="button"
              disabled={!ticked || approve.isPending}
              onClick={() => approveThis(true)}
              className="min-h-target-primary w-full rounded-button bg-primary font-extrabold text-label text-on-primary disabled:opacity-50"
            >
              {t('todays.approve')}
            </button>
            <button type="button" disabled={!ticked || approve.isPending} onClick={() => approveThis(false)} className="btn-secondary">
              {t('todays.approveLater')}
            </button>
          </div>
        )
      )}
    </section>
  )
}

/**
 * Her note as she typed it (any language, T-119), and what Kannada families read when that is a translation. Every other
 * family reads it in their own language.
 */
function NoteText({ note }: { note: NonNullable<TodaysMessage['note']> }) {
  const { t } = useTranslation()
  const typed = note.original ?? note.text.kn
  const kannada = note.text.kn
  return (
    <>
      {typed && <p className="text-body">{typed}</p>}
      {kannada && kannada !== typed && (
        <p className="text-caption text-ink-muted">
          {t('todays.noteKannada')} <span lang="kn">{kannada}</span>
        </p>
      )}
    </>
  )
}

/** One section: the shared card (T-103), its content, and Edit / Remove (or Choose another pair) while she can change it. */
function Section({
  kind,
  classId,
  date,
  message,
  readOnly,
  open,
  onToggle,
  children,
}: {
  kind: TodaysSectionKind
  classId: string
  date: string
  message: TodaysMessage
  readOnly: boolean
  open: boolean
  onToggle: () => void
  children: ReactNode
}) {
  const { t } = useTranslation()
  const setRemoved = useSetRemoved(classId, date)
  const [editing, setEditing] = useState(false)
  const removed = message.removed.includes(kind)
  const emptyNote = kind === 'note' && !message.note
  // The WhatsApp text promises the concept (with its audio), the activity and the picture question, so those stay
  // (D-108); she can still edit them. At least one section must still go: count what this message has.
  const showing = present(message).filter((k) => !message.removed.includes(k)).length
  const canRemove = !PROMISED.includes(kind) && !emptyNote && (removed || showing > 1)

  return (
    <SectionCard kind={kind} removed={removed} open={open} onToggle={onToggle}>
      {editing && kind !== 'picture' ? (
        <EditForm kind={kind as Editable} classId={classId} date={date} message={message} onDone={() => setEditing(false)} />
      ) : (
        <>
          {emptyNote ? <p className="text-caption text-ink-muted">{t('todays.noNote')}</p> : children}
          {!readOnly && kind === 'picture' && <PairPicker classId={classId} date={date} message={message} />}
          {!readOnly && (
            <SectionActions
              editLabel={kind !== 'picture' && kind !== 'video' && !removed ? (emptyNote ? t('todays.addNote') : t('todays.edit')) : null}
              onEdit={() => setEditing(true)}
              remove={
                canRemove
                  ? { removed, pending: setRemoved.isPending, onToggle: () => setRemoved.mutate({ kind, removed: !removed }) }
                  : null
              }
            />
          )}
        </>
      )}
    </SectionCard>
  )
}

/** Her Kannada for one section (D-92). The API checks it as it checks A9; a refusal says why and nothing is saved. */
function EditForm({
  kind,
  classId,
  date,
  message,
  onDone,
}: {
  kind: Editable
  classId: string
  date: string
  message: TodaysMessage
  onDone: () => void
}) {
  const { t } = useTranslation()
  const edit = useEditSection(classId, date)
  const kn = message.languages.find((l) => l.language === 'kn')
  const problem = editProblem(edit.error)
  const step = (n: number) => t('todays.step', { n })
  const fields: EditField[] =
    kind === 'activity'
      ? (kn?.steps.length ? kn.steps : ['']).map((s, i) => ({ label: step(i + 1), value: s, placeholder: t('todays.stepPlaceholder'), rows: 2 }))
      : [
          {
            label: t(kind === 'note' ? 'todays.noteLabel' : 'todays.conceptLabel'),
            // Her note starts from what she typed, in whatever language (T-119).
            value: kind === 'concept' ? (kn?.concept ?? '') : (message.note?.original ?? message.note?.text.kn ?? ''),
            placeholder: t(kind === 'note' ? 'todays.notePlaceholder' : 'todays.conceptPlaceholder'),
          },
        ]

  return (
    <SectionEditForm
      fields={fields}
      more={
        kind === 'activity'
          ? { max: 3, label: step, placeholder: t('todays.stepPlaceholder'), add: t('todays.addStep') }
          : undefined
      }
      hint={t('todays.saveHint')}
      problem={
        problem &&
        t(`todays.problem.${problem.problem}`, { defaultValue: t('todays.problem.other') }) +
          (problem.problem === 'numbers' ? ` ${t('todays.allowed', { numbers: problem.allowed.filter((n) => n.length > 1).join(', ') })}` : '')
      }
      conflict={edit.isError}
      pending={edit.isPending}
      onSave={(values) =>
        edit.mutate(
          kind === 'activity'
            ? { kind, version: message.version, steps: values.filter((s) => s.trim() !== '') }
            : { kind, version: message.version, text: values[0] ?? '' },
          { onSuccess: onDone },
        )
      }
      onCancel={onDone}
    />
  )
}

/** Another pair from the book's list (D-92): she picks, code works out the answer. She can't type numbers here. */
function PairPicker({ classId, date, message }: { classId: string; date: string; message: TodaysMessage }) {
  const { t } = useTranslation()
  const setPicture = useSetPicture(classId, date)
  const [picking, setPicking] = useState(false)

  if (!picking) {
    return (
      <button type="button" onClick={() => setPicking(true)} className="btn-secondary w-auto self-start">
        {t('todays.otherPair')}
      </button>
    )
  }

  return (
    <fieldset className="flex flex-col gap-2">
      <legend className="text-caption">{t('todays.pairsFromBook')}</legend>
      <div className="flex flex-wrap gap-2">
        {message.pairs.map(([a, b], index) => (
          <button
            key={`${a}-${b}-${index}`}
            type="button"
            aria-pressed={index === message.pictureIndex}
            disabled={setPicture.isPending}
            onClick={() =>
              setPicture.mutate({ version: message.version, index }, { onSuccess: () => setPicking(false) })
            }
            className={`min-h-target rounded-button border-2 px-3 text-label ${
              index === message.pictureIndex ? 'border-primary bg-primary text-on-primary' : 'border-border bg-surface text-ink'
            }`}
          >
            {message.picture && message.picture.kind !== 'compare' ? sumText(message.picture.kind, a, b) : t('todays.pair', { a, b })}
          </button>
        ))}
      </div>
    </fieldset>
  )
}

/** A sum as the book writes it (D-116): "248 + 438" or "872 − 586". Numbers and signs only, so no translation. */
function sumText(kind: 'add' | 'sub', a: number, b: number) {
  return `${a} ${kind === 'add' ? '+' : '−'} ${b}`
}
