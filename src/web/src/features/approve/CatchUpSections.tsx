import { type ReactNode, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useCurriculum } from '../lessons/useLessons'
import { sumQuestion } from '../parent/sumQuestion'
import { lessonNames } from './packageText'
import { SegmentPlayer } from './SegmentPlayer'
import { SECTION_ORDER, type SectionKind } from './sectionKinds'
import { type EditField, SectionActions, SectionCard, SectionEditForm } from './Sections'
import type { CatchUpPackage } from './types'
import { editProblem } from './useTodaysMessage'
import { useEditPackageSection, useSetPackageRemoved } from './usePackages'

type Editable = 'concept' | 'activity' | 'note'

/**
 * A catch-up as the same sections as today's message (T-103, D-129): Concept (the script with its audio and the Kannada
 * summary), Activity (A3's instruction around the numbers code chose), Video (the lesson's link, by code), Picture question
 * (the learning check, by code) and her note. One open at a time. She can edit the words while it is ready or needs
 * review; every save is a new version that clears her tick (guardrail 3).
 */
export function CatchUpSections({ pkg, classId, canChange }: { pkg: CatchUpPackage; classId: string; canChange: boolean }) {
  const [open, setOpen] = useState<SectionKind | null>('concept')
  const kinds = SECTION_ORDER.filter(
    (kind) => (kind !== 'video' || pkg.video !== null) && (kind !== 'picture' || pkg.check.options.length > 0),
  )

  return (
    <ul className="flex flex-col gap-2">
      {kinds.map((kind) => (
        <li key={kind}>
          <Section kind={kind} pkg={pkg} classId={classId} canChange={canChange} open={open === kind} onToggle={() => setOpen(open === kind ? null : kind)}>
            <Body kind={kind} pkg={pkg} />
          </Section>
        </li>
      ))}
    </ul>
  )
}

function Body({ kind, pkg }: { kind: SectionKind; pkg: CatchUpPackage }) {
  const { t, i18n } = useTranslation()
  const chapters = useCurriculum().data?.chapters ?? []

  switch (kind) {
    case 'concept':
      return (
        <>
          {pkg.audio && <SegmentPlayer segments={pkg.audio.segments} />}
          {pkg.script && (
            <p lang={pkg.language} className="whitespace-pre-line text-body">
              {pkg.script}
            </p>
          )}
          {pkg.glossKn && (
            <div className="rounded-card border-2 border-primary bg-panel p-[14px]">
              <h4 className="text-label text-primary-strong">{t('approve.pkg.summary')}</h4>
              <p lang="kn" className="mt-1 text-body text-ink">
                {pkg.glossKn}
              </p>
              {i18n.language !== 'kn' && pkg.glossEn && <p className="mt-2 text-body text-ink-muted">{pkg.glossEn}</p>}
              {pkg.keyWordsKn.length > 0 && (
                <>
                  <h5 className="mt-3 text-caption text-ink-muted">{t('approve.pkg.keyWords')}</h5>
                  <ul className="mt-1 flex flex-wrap gap-2">
                    {pkg.keyWordsKn.map((w) => (
                      <li key={w} lang="kn" className="rounded-sm border border-border bg-surface px-2 py-1 text-label">
                        {w}
                      </li>
                    ))}
                  </ul>
                </>
              )}
            </div>
          )}
        </>
      )
    case 'activity':
      return (
        <>
          {pkg.activity.instruction && (
            <p lang={pkg.language} className="text-body">
              {pkg.activity.instruction}
            </p>
          )}
          {pkg.activity.objectOption && (
            <p lang={pkg.language} className="text-caption text-ink-muted">
              {pkg.activity.objectOption}
            </p>
          )}
          <p className="text-caption text-ink-muted">{t('approve.sections.activityNumbers', { question: pkg.activity.question })}</p>
        </>
      )
    case 'picture':
      return (
        <>
          <p className="text-caption text-ink-muted">
            {t('approve.sections.tests', { lesson: lessonNames(chapters, [pkg.check.competencyId], i18n.language)[0] })}
          </p>
          <p className="text-title">{sumQuestion(pkg.check.question)}</p>
          <ul className="flex gap-2" aria-label={t('approve.pkg.options')}>
            {pkg.check.options.map((option, i) => (
              <li
                key={option}
                className={`rounded-sm border px-3 py-1 text-label ${
                  i === pkg.check.correctIndex ? 'border-primary bg-panel text-primary-strong' : 'border-border text-ink'
                }`}
              >
                {option}
                {i === pkg.check.correctIndex && (
                  <>
                    {' ✓'}
                    <span className="sr-only">{t('approve.pkg.correct')}</span>
                  </>
                )}
              </li>
            ))}
          </ul>
          <p className="text-caption text-ink-muted">{t('approve.sections.pictureByCode')}</p>
        </>
      )
    case 'video':
      return pkg.video ? (
        // From the admin page's link for the lesson (D-106), put in by code; she can remove it.
        <p className="flex flex-col gap-1">
          <a href={pkg.video.url} target="_blank" rel="noopener noreferrer" className="btn-link w-fit">
            {t('todays.video.open')}
          </a>
          <span className="break-all text-caption text-ink-muted">{pkg.video.url}</span>
        </p>
      ) : null
    case 'note':
      return pkg.note ? <p className="text-body">{pkg.note.text}</p> : <p className="text-caption text-ink-muted">{t('todays.noNote')}</p>
  }
}

/** One section with Edit / Remove while she can change the catch-up. The picture's numbers are code's: no Edit. */
function Section({
  kind,
  pkg,
  classId,
  canChange,
  open,
  onToggle,
  children,
}: {
  kind: SectionKind
  pkg: CatchUpPackage
  classId: string
  canChange: boolean
  open: boolean
  onToggle: () => void
  children: ReactNode
}) {
  const { t } = useTranslation()
  const setRemoved = useSetPackageRemoved(classId)
  const [editing, setEditing] = useState(false)
  const removed = pkg.removed.includes(kind)
  const editable = (kind === 'concept' || kind === 'activity' || kind === 'note') && !removed && (kind !== 'concept' || pkg.script !== null)
  // The family's page is Listen → Activity → Answer, so only the video and her note can go.
  const removable = (kind === 'video' && pkg.video !== null) || (kind === 'note' && pkg.note !== null)

  return (
    <SectionCard kind={kind} removed={removed} open={open} onToggle={onToggle}>
      {editing && canChange && editable ? (
        <EditForm kind={kind as Editable} pkg={pkg} classId={classId} onDone={() => setEditing(false)} />
      ) : (
        <>
          {children}
          {canChange && (
            <SectionActions
              editLabel={editable ? (kind === 'note' && !pkg.note ? t('todays.addNote') : t('todays.edit')) : null}
              onEdit={() => setEditing(true)}
              remove={
                removable
                  ? {
                      removed,
                      pending: setRemoved.isPending,
                      onToggle: () => setRemoved.mutate({ id: pkg.id, kind: kind as 'video' | 'note', removed: !removed }),
                    }
                  : null
              }
            />
          )}
        </>
      )}
    </SectionCard>
  )
}

/**
 * Her words for the script, the activity or her note. The script and the activity are in the family's language (they hear
 * and read it); her note goes as she writes it. The API checks them by code; a refusal says why and nothing is saved.
 */
function EditForm({ kind, pkg, classId, onDone }: { kind: Editable; pkg: CatchUpPackage; classId: string; onDone: () => void }) {
  const { t } = useTranslation()
  const edit = useEditPackageSection(classId)
  const problem = editProblem(edit.error)
  const language = t(`missed.language.${pkg.language}`)
  const fields: EditField[] =
    kind === 'concept'
      ? [{ label: t('approve.sections.scriptLabel', { language }), value: pkg.script ?? '', rows: 8, lang: pkg.language }]
      : kind === 'activity'
        ? [
            { label: t('approve.sections.instructionLabel', { language }), value: pkg.activity.instruction ?? '', rows: 3, lang: pkg.language },
            { label: t('approve.sections.objectLabel'), value: pkg.activity.objectOption ?? '', rows: 2, lang: pkg.language },
          ]
        : [{ label: t('approve.sections.noteLabel'), value: pkg.note?.text ?? '', placeholder: t('todays.notePlaceholder') }]

  const problemText =
    problem &&
    t(`approve.sections.problem.${problem.problem}`, {
      defaultValue: t('approve.sections.problem.other'),
      language,
      numbers: problem.problem === 'numbers' ? problem.allowed.filter((n) => n.length > 1).join(', ') : problem.detail,
    })

  return (
    <SectionEditForm
      fields={fields}
      hint={t(kind === 'concept' ? 'approve.sections.saveHintVoice' : 'approve.sections.saveHint')}
      problem={problemText}
      conflict={edit.isError}
      pending={edit.isPending}
      onSave={([text = '', objectOption]) =>
        edit.mutate(
          { id: pkg.id, kind, scriptVersion: pkg.scriptVersion, text, objectOption: kind === 'activity' ? (objectOption ?? '') : undefined },
          { onSuccess: onDone },
        )
      }
      onCancel={onDone}
    />
  )
}
