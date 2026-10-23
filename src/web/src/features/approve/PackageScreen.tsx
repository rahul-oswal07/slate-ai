import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import { ApiError, NetworkError } from '../../api/client'
import { Icon } from '../../components/Icon'
import { useAnnounce } from '../../components/useAnnounce'
import { useFocusOnMount } from '../../components/useFocusOnMount'
import { useMe } from '../auth/useMe'
import { formatSchoolTime } from '../today/schoolTime'
import { useCurriculum } from '../lessons/useLessons'
import { lessonNames } from './packageText'
import { SegmentPlayer } from './SegmentPlayer'
import { StatusTag } from './StatusTag'
import type { CatchUpPackage, CompressionItem } from './types'
import { useApprove, usePackages, useRegenerate } from './usePackages'

/**
 * T3 Approve, detail: listen, read the Kannada summary and "Why this", see the picture question, tick, Approve
 * (bound to the script version she ticked: a new version clears the tick). Needs review blocks Approve and says why
 * in numbers. Withdraw and bulk approve are roadmap (D-70).
 */
export function PackageScreen() {
  const { id = '' } = useParams()
  const { t } = useTranslation()
  const me = useMe()
  const headingRef = useFocusOnMount<HTMLHeadingElement>()
  const classId = me.data?.classId ?? ''
  const week = usePackages(classId)
  const pkg = week.data?.packages.find((p) => p.id === id)

  return (
    <section aria-labelledby="pkg-title" className="flex flex-col gap-3 pb-4">
      <Link to="/approve" className="flex min-h-target w-fit items-center text-label text-primary-strong">
        ‹ {t('common.back')}
      </Link>
      <h1 id="pkg-title" ref={headingRef} tabIndex={-1} className="text-title">
        {t('approve.pkg.title')}
      </h1>
      {week.isPending && <div aria-hidden="true" className="h-40 rounded-md border border-border bg-surface" />}
      {week.isError && !week.data && (
        <div role="alert" className="rounded-md border border-border border-l-4 border-l-error bg-error-bg p-4">
          <p className="text-body">{t('app.loadError')}</p>
          <button
            type="button"
            onClick={() => void week.refetch()}
            className="mt-3 min-h-target w-full rounded-md border border-border bg-surface text-label"
          >
            {t('common.tryAgain')}
          </button>
        </div>
      )}
      {week.data && !pkg && <p className="text-body">{t('approve.pkg.missing')}</p>}
      {pkg && <PackageDetail pkg={pkg} classId={classId} />}
    </section>
  )
}

function PackageDetail({ pkg, classId }: { pkg: CatchUpPackage; classId: string }) {
  const { t, i18n } = useTranslation()
  const announce = useAnnounce()
  const approve = useApprove(classId)
  const regenerate = useRegenerate(classId)
  const chapters = useCurriculum().data?.chapters ?? []
  // The tick belongs to the version she read: a new version (Simpler / Shorter) clears it (T3, D-09).
  const [tickedVersion, setTickedVersion] = useState<number | null>(null)
  const ticked = tickedVersion === pkg.scriptVersion
  const previous = useRef(pkg.status)

  useEffect(() => {
    if (previous.current === 'generating' && pkg.status !== 'generating') {
      announce(t('approve.announce.regenReady'))
    }
    previous.current = pkg.status
  }, [pkg.status, announce, t])

  const lessons = lessonNames(chapters, pkg.targetCompetencyIds, i18n.language)
  const checkLesson = lessonNames(chapters, [pkg.check.competencyId], i18n.language)[0]
  const busy = pkg.status === 'generating' || approve.isPending || regenerate.isPending
  const failedChecks = pkg.review?.checks.filter((c) => !c.passed) ?? []
  const tooLong = failedChecks.some((c) => c.name === 'audio' || c.name === 'length')
  const error = approve.error ?? regenerate.error

  function startRegenerate(hint: 'simpler' | 'shorter' | null) {
    setTickedVersion(null)
    announce(t('approve.announce.regenStarted'))
    regenerate.mutate({ id: pkg.id, hint })
  }

  return (
    <>
      <div className="flex flex-wrap items-center gap-2">
        <StatusTag status={pkg.status} />
        {pkg.status !== 'failed' && <AiLabel />}
      </div>
      <div>
        <h2 className="text-label">{t('approve.pkg.children', { language: t(`approve.in.${pkg.language}`) })}</h2>
        <p className="text-body font-bold">{pkg.children.map((c) => c.firstName).join(', ')}</p>
        <p className="text-caption text-ink-muted">{lessons.join(' + ')}</p>
      </div>

      {pkg.status === 'failed' && (
        <>
          <div role="alert" className="rounded-md border border-border border-l-4 border-l-error bg-error-bg p-[14px] text-body">
            {t('approve.pkg.failed')}
          </div>
          <ErrorLine error={error} />
          <button
            type="button"
            disabled={busy}
            onClick={() => startRegenerate(null)}
            className="min-h-target-primary w-full rounded-md bg-primary text-label text-on-primary disabled:opacity-70"
          >
            {t('common.tryAgain')}
          </button>
        </>
      )}

      {pkg.status === 'generating' && (
        <output className="flex items-start gap-2 rounded-md border border-border border-l-4 border-l-warning bg-surface p-[14px] text-body">
          <Icon name="clock" size={20} className="mt-0.5 shrink-0 text-warning" />
          {t('approve.pkg.regenerating')}
        </output>
      )}

      {pkg.status === 'needsReview' && (
        <div className="rounded-md border border-border border-l-4 border-l-error bg-error-bg p-[14px]">
          <p className="text-body font-bold">{t('approve.pkg.reviewTitle')}</p>
          <ul className="mt-1 list-disc pl-5 text-body">
            {failedChecks.map((c) => (
              <li key={c.name}>{c.detail}</li>
            ))}
          </ul>
          <p className="mt-2 text-caption">{t('approve.pkg.cannotApprove')}</p>
        </div>
      )}

      {pkg.status === 'approved' && (
        <output className="rounded-md border border-border border-l-4 border-l-primary bg-surface p-[14px] text-body">
          {t('approve.pkg.approved')}
        </output>
      )}

      {pkg.status === 'sent' && pkg.sentAt && (
        <output className="flex items-start gap-2 rounded-md border border-border border-l-4 border-l-primary bg-surface p-[14px] text-body">
          <Icon name="send" size={20} className="mt-0.5 shrink-0 text-primary-strong" />
          {t('approve.pkg.sent', { time: formatSchoolTime(pkg.sentAt, i18n.language), n: pkg.familiesSent })}
        </output>
      )}

      {pkg.status !== 'failed' && pkg.audio && <SegmentPlayer segments={pkg.audio.segments} />}

      {pkg.status !== 'failed' && pkg.glossKn && (
        <div className="rounded-md border border-border bg-panel p-[14px]">
          <h2 className="text-label text-primary-strong">{t('approve.pkg.summary')}</h2>
          <p lang="kn" className="mt-1 text-body text-ink">{pkg.glossKn}</p>
          {i18n.language !== 'kn' && pkg.glossEn && <p className="mt-2 text-body text-ink-muted">{pkg.glossEn}</p>}
          {pkg.keyWordsKn.length > 0 && (
            <>
              <h3 className="mt-3 text-caption text-ink-muted">{t('approve.pkg.keyWords')}</h3>
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

      {pkg.status !== 'failed' && <WhyThis pkg={pkg} />}

      {pkg.status !== 'failed' && pkg.check.options.length > 0 && (
        <div className="rounded-md border border-border bg-surface p-[14px]">
          <h2 className="text-label">{t('approve.pkg.check', { lesson: checkLesson })}</h2>
          <p className="mt-1 text-title">{pkg.check.question} = ?</p>
          <ul className="mt-2 flex gap-2" aria-label={t('approve.pkg.options')}>
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
        </div>
      )}

      {pkg.status === 'ready' && (
        <div className="flex flex-col gap-2 border-t border-divider pt-3">
          <label className="flex min-h-target items-center gap-3 text-body">
            <input
              type="checkbox"
              className="size-6 accent-primary"
              checked={ticked}
              disabled={busy}
              onChange={(e) => setTickedVersion(e.target.checked ? pkg.scriptVersion : null)}
            />
            {t('approve.pkg.tick')}
          </label>
          <ErrorLine error={error} />
          {!ticked && <p className="text-caption text-ink-muted">{t('approve.pkg.approveHint')}</p>}
          <button
            type="button"
            disabled={!ticked || busy}
            onClick={() =>
              approve.mutate(
                { id: pkg.id, scriptVersion: pkg.scriptVersion },
                { onSuccess: () => announce(t('approve.announce.approved')) },
              )
            }
            className="min-h-target-primary w-full rounded-md bg-primary text-label text-on-primary disabled:opacity-50"
          >
            {t('approve.pkg.approve')}
          </button>
          <RegenerateButtons busy={busy} onRegenerate={startRegenerate} />
        </div>
      )}

      {pkg.status === 'needsReview' && (
        <div className="flex flex-col gap-2">
          <ErrorLine error={error} />
          {tooLong ? (
            <>
              <button
                type="button"
                disabled={busy}
                onClick={() => startRegenerate('shorter')}
                className="min-h-target-primary w-full rounded-md bg-primary text-label text-on-primary disabled:opacity-70"
              >
                {t('approve.pkg.makeShorter')}
              </button>
              <button
                type="button"
                disabled={busy}
                onClick={() => startRegenerate('simpler')}
                className="min-h-target w-full text-label text-primary-strong disabled:opacity-70"
              >
                {t('approve.pkg.simpler')}
              </button>
            </>
          ) : (
            <RegenerateButtons busy={busy} onRegenerate={startRegenerate} />
          )}
        </div>
      )}
    </>
  )
}

function RegenerateButtons({ busy, onRegenerate }: { busy: boolean; onRegenerate: (hint: 'simpler' | 'shorter') => void }) {
  const { t } = useTranslation()
  return (
    <div className="grid grid-cols-2 gap-2">
      <button
        type="button"
        disabled={busy}
        onClick={() => onRegenerate('simpler')}
        className="min-h-target rounded-md border border-border bg-surface text-label disabled:opacity-70"
      >
        {t('approve.pkg.simpler')}
      </button>
      <button
        type="button"
        disabled={busy}
        onClick={() => onRegenerate('shorter')}
        className="min-h-target rounded-md border border-border bg-surface text-label disabled:opacity-70"
      >
        {t('approve.pkg.shorter')}
      </button>
    </div>
  )
}

/** "Why this": each lesson kept or left out, with A2's reason, or code's wording when the rule chose (T3: never shown as AI). */
function WhyThis({ pkg }: { pkg: CatchUpPackage }) {
  const { t, i18n } = useTranslation()
  const chapters = useCurriculum().data?.chapters ?? []
  const reasons = new Map<string, string | null>()
  const kept = new Set(pkg.targetCompetencyIds)
  const dropped: string[] = []
  for (const child of pkg.children) {
    const fromAi = child.rationale.source === 'ai'
    for (const item of [...child.rationale.targets, ...child.rationale.dropped] as CompressionItem[]) {
      if (fromAi && item.reason && !reasons.get(item.id)) {
        reasons.set(item.id, item.reason)
      }
      if (!kept.has(item.id) && !dropped.includes(item.id)) {
        dropped.push(item.id)
      }
    }
  }
  const rows = [...pkg.targetCompetencyIds.map((id) => ({ id, kept: true })), ...dropped.map((id) => ({ id, kept: false }))]

  return (
    <div className="rounded-md border border-border bg-surface p-[14px]">
      <h2 className="text-label">{t('approve.pkg.why')}</h2>
      <ul className="mt-2 flex flex-col gap-2">
        {rows.map((row) => (
          <li key={row.id} className={`flex items-start gap-2 text-body ${row.kept ? '' : 'text-ink-muted'}`}>
            <Icon name={row.kept ? 'check' : 'x'} size={18} className="mt-1 shrink-0" />
            <span>
              <span className="font-bold">{lessonNames(chapters, [row.id], i18n.language)[0]}</span>
              <span className="sr-only"> ({row.kept ? t('approve.why.keptLabel') : t('approve.why.droppedLabel')})</span>
              {': '}
              {reasons.get(row.id) ?? (row.kept ? t('approve.why.kept') : t('approve.why.dropped'))}
            </span>
          </li>
        ))}
      </ul>
    </div>
  )
}

function ErrorLine({ error }: { error: Error | null }) {
  const { t } = useTranslation()
  if (!error) {
    return null
  }
  const text =
    error instanceof NetworkError
      ? t('approve.pkg.offline')
      : error instanceof ApiError && error.status === 409
        ? t('common.conflict')
        : t('approve.pkg.error')
  return (
    <p role="alert" className="flex items-start gap-2 text-caption text-error">
      <Icon name="alert" size={18} className="mt-0.5 shrink-0" />
      {text}
    </p>
  )
}

function AiLabel() {
  const { t } = useTranslation()
  return (
    <span className="inline-flex items-center gap-1 rounded-sm bg-panel px-2 py-0.5 text-caption text-primary-strong">
      <Icon name="bot" size={14} />
      {t('ai.label')}
    </span>
  )
}
