import { type ReactNode, useId, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { Icon } from '../../components/Icon'
import { useAnnounce } from '../../components/useAnnounce'
import { findLesson, lessonName, pageText } from '../lessons/lessonText'
import { useCurriculum } from '../lessons/useLessons'
import { istToday } from '../today/schoolTime'
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
const SECTIONS: TodaysSectionKind[] = ['concept', 'activity', 'picture', 'note']

/**
 * Approve → Today's message (T-89, D-91, D-92): the day's message as sections, one open at a time. She reads and edits
 * the Kannada; every save is a new version that runs the checks again and clears her tick. The numbers come from code:
 * she can only choose another pair from the book. Nothing goes until she ticks and approves this version (guardrail 3).
 */
export function TodaysMessageTab({ classId }: { classId: string }) {
  const { t } = useTranslation()
  const date = istToday()
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
  const chapters = useCurriculum().data?.chapters ?? []
  const approve = useApproveTodays(classId, date)
  const retry = useRetryTodays(classId, date)
  const [open, setOpen] = useState<TodaysSectionKind | null>('concept')
  // The tick belongs to the version she read: a new version (any edit, D-92) shows it unticked.
  const [tickedVersion, setTickedVersion] = useState<number | null>(null)
  const ticked = tickedVersion === message.version
  const readOnly = message.sent || message.status === 'approved'
  const kn = message.languages.find((l) => l.language === 'kn')
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
        {SECTIONS.map((kind) => (
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
                <ol className="flex list-decimal flex-col gap-1 pl-5 text-body">
                  {kn.steps.map((step) => (
                    <li key={step}>{step}</li>
                  ))}
                </ol>
              )}
              {kind === 'picture' && message.picture && (
                <p className="text-body">
                  {t('todays.pictureQuestion', { a: message.picture.a, b: message.picture.b })}{' '}
                  <span className="text-caption text-ink-muted">{t('todays.pictureAnswer', { answer: message.picture.answer })}</span>
                </p>
              )}
              {kind === 'note' && (message.note?.text.kn ? <p className="text-body">{message.note.text.kn}</p> : null)}
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
              onClick={() =>
                approve.mutate(message.version, {
                  onSuccess: (latest) => announce(t(latest.sent ? 'todays.sent' : 'todays.approved')),
                })
              }
              className="min-h-target-primary w-full rounded-button bg-primary font-extrabold text-label text-on-primary disabled:opacity-50"
            >
              {t('todays.approve')}
            </button>
          </div>
        )
      )}
    </section>
  )
}

/** One section: a header that opens it, its content, and Edit / Remove (or Choose another pair) while she can change it. */
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
  const id = useId()
  const setRemoved = useSetRemoved(classId, date)
  const [editing, setEditing] = useState(false)
  const removed = message.removed.includes(kind)
  const emptyNote = kind === 'note' && !message.note
  const canRemove = !emptyNote && (removed || message.removed.length < 3)

  return (
    <div className={`rounded-card border-[1.5px] border-edge bg-surface ${removed ? 'opacity-70' : ''}`}>
      <h3>
        <button
          type="button"
          aria-expanded={open}
          aria-controls={`${id}-body`}
          onClick={onToggle}
          className="flex min-h-target w-full items-center justify-between gap-2 px-[14px] text-left text-label"
        >
          <span>
            {t(`todays.section.${kind}`)}
            {removed && <span className="ml-2 text-caption text-ink-muted">{t('todays.removedTag')}</span>}
          </span>
          <Icon name={open ? 'chevronUp' : 'chevronDown'} size={20} className="shrink-0 text-ink-muted" />
        </button>
      </h3>
      {open && (
        <div id={`${id}-body`} className="flex flex-col gap-3 border-t border-divider p-[14px]">
          {editing && kind !== 'picture' ? (
            <EditForm kind={kind as Editable} classId={classId} date={date} message={message} onDone={() => setEditing(false)} />
          ) : (
            <>
              {emptyNote ? <p className="text-caption text-ink-muted">{t('todays.noNote')}</p> : children}
              {!readOnly && kind === 'picture' && <PairPicker classId={classId} date={date} message={message} />}
              {!readOnly && (
                <div className="flex flex-wrap gap-2">
                  {kind !== 'picture' && !removed && (
                    <button type="button" onClick={() => setEditing(true)} className="btn-secondary w-auto">
                      {emptyNote ? t('todays.addNote') : t('todays.edit')}
                    </button>
                  )}
                  {canRemove && (
                    <button
                      type="button"
                      disabled={setRemoved.isPending}
                      onClick={() => setRemoved.mutate({ kind, removed: !removed })}
                      className="btn-link"
                    >
                      {removed ? t('todays.restore') : t('todays.remove')}
                    </button>
                  )}
                </div>
              )}
            </>
          )}
        </div>
      )}
    </div>
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
  const [text, setText] = useState(kind === 'concept' ? (kn?.concept ?? '') : (message.note?.text.kn ?? ''))
  const [steps, setSteps] = useState<string[]>(kn?.steps.length ? kn.steps : [''])
  const problem = editProblem(edit.error)
  const errorId = useId()

  const save = () =>
    edit.mutate(
      kind === 'activity'
        ? { kind, version: message.version, steps: steps.filter((s) => s.trim() !== '') }
        : { kind, version: message.version, text },
      { onSuccess: onDone },
    )

  return (
    <form
      className="flex flex-col gap-2"
      onSubmit={(e) => {
        e.preventDefault()
        save()
      }}
    >
      {kind === 'activity' ? (
        steps.map((step, i) => (
          <label key={i} className="flex flex-col gap-1 text-caption">
            {t('todays.step', { n: i + 1 })}
            <textarea
              value={step}
              rows={2}
              aria-describedby={problem ? errorId : undefined}
              onChange={(e) => setSteps(steps.map((s, j) => (j === i ? e.target.value : s)))}
              className="rounded-button border-2 border-border bg-surface p-2 text-body text-ink"
            />
          </label>
        ))
      ) : (
        <label className="flex flex-col gap-1 text-caption">
          {t(kind === 'note' ? 'todays.noteLabel' : 'todays.conceptLabel')}
          <textarea
            value={text}
            rows={4}
            placeholder={kind === 'note' ? t('todays.notePlaceholder') : undefined}
            aria-describedby={problem ? errorId : undefined}
            onChange={(e) => setText(e.target.value)}
            className="rounded-button border-2 border-border bg-surface p-2 text-body text-ink"
          />
        </label>
      )}
      {kind === 'activity' && steps.length < 3 && (
        <button type="button" onClick={() => setSteps([...steps, ''])} className="btn-link self-start">
          {t('todays.addStep')}
        </button>
      )}
      {problem && (
        <p id={errorId} role="alert" className="text-caption text-error">
          {t(`todays.problem.${problem.problem}`, { defaultValue: t('todays.problem.other') })}
          {problem.problem === 'numbers' && ` ${t('todays.allowed', { numbers: problem.allowed.filter((n) => n.length > 1).join(', ') })}`}
        </p>
      )}
      {edit.isError && !problem && (
        <p role="alert" className="text-caption text-error">
          {t('todays.changed')}
        </p>
      )}
      <p className="text-caption text-ink-muted">{t('todays.saveHint')}</p>
      <div className="flex gap-2">
        <button type="submit" disabled={edit.isPending} className="btn-primary w-auto">
          {t('todays.save')}
        </button>
        <button type="button" onClick={onDone} className="btn-secondary w-auto">
          {t('common.cancel')}
        </button>
      </div>
    </form>
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
            {t('todays.pair', { a, b })}
          </button>
        ))}
      </div>
    </fieldset>
  )
}
