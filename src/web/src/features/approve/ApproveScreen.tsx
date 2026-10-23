import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { NetworkError } from '../../api/client'
import { Icon } from '../../components/Icon'
import { useFocusOnMount } from '../../components/useFocusOnMount'
import { useOnline } from '../../components/useOnline'
import { useMe } from '../auth/useMe'
import { useCurriculum } from '../lessons/useLessons'
import { duration, lessonNames } from './packageText'
import { StatusTag } from './StatusTag'
import type { CatchUpPackage } from './types'
import { useGenerate, usePackages } from './usePackages'

/**
 * T3 Approve, list (docs/design/T3-approve.md; D-70: one catch-up at a time, no bulk approve or Withdraw).
 * One card per catch-up (targets × language), not per child. Online only, with the D-68 states.
 */
export function ApproveScreen() {
  const { t, i18n } = useTranslation()
  const me = useMe()
  const online = useOnline()
  const headingRef = useFocusOnMount<HTMLHeadingElement>()
  const classId = me.data?.classId ?? ''
  const week = usePackages(classId)
  const generate = useGenerate(classId)
  const chapters = useCurriculum().data?.chapters ?? []
  const packages = week.data?.packages ?? []
  const done = packages.filter((p) => p.status === 'approved' || p.status === 'sent')
  const allDone = packages.length > 0 && done.length === packages.length
  // Counted by the API: families with consent who haven't stopped (sent ones, or those it goes to after 6 pm).
  const families = done.reduce((sum, p) => sum + (p.status === 'sent' ? p.familiesSent : p.familiesReachable), 0)

  return (
    <section aria-labelledby="approve-title" className="flex flex-col gap-3">
      <h1 id="approve-title" ref={headingRef} tabIndex={-1} className="text-title">
        {t('approve.title')}
      </h1>
      <p className="text-body text-ink-muted">{t('approve.intro')}</p>

      {!online && (
        <output className="flex items-start gap-2 rounded-md border border-border border-l-4 border-l-warning bg-surface p-3">
          <Icon name="cloudOff" size={18} className="mt-1 shrink-0 text-warning" />
          <span className="text-body text-ink">{t('offline.noNetwork')}</span>
        </output>
      )}

      {week.isPending && classId !== '' && (
        <>
          <div aria-hidden="true" className="h-24 rounded-md border border-border bg-surface" />
          <div aria-hidden="true" className="h-24 rounded-md border border-border bg-surface" />
        </>
      )}

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

      {allDone && (
        <output className="rounded-md border border-border border-l-4 border-l-primary bg-surface p-3 text-body">
          {t('approve.allDone', { n: packages.length, families })}
        </output>
      )}

      {week.data && packages.length === 0 && (
        <div className="flex flex-col gap-3 rounded-md border border-border bg-surface p-[14px]">
          <p className="text-body">{t('approve.empty')}</p>
          <GenerateButton primary pending={generate.isPending} onClick={() => generate.mutate()} />
        </div>
      )}

      {packages.length > 0 && (
        <ul className="flex flex-col gap-3">
          {packages.map((p) => (
            <li key={p.id}>
              <PackageCard pkg={p} lessons={lessonNames(chapters, p.targetCompetencyIds, i18n.language)} />
            </li>
          ))}
        </ul>
      )}

      {packages.length > 0 && (
        <GenerateButton pending={generate.isPending} onClick={() => generate.mutate()} />
      )}

      {generate.isError && (
        <p role="alert" className="flex items-start gap-2 text-caption text-error">
          <Icon name="alert" size={18} className="mt-0.5 shrink-0" />
          {generate.error instanceof NetworkError ? t('offline.noNetwork') : t('approve.generateError')}
        </p>
      )}
    </section>
  )
}

function GenerateButton({ primary = false, pending, onClick }: { primary?: boolean; pending: boolean; onClick: () => void }) {
  const { t } = useTranslation()
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={pending}
      className={
        primary
          ? 'min-h-target-primary w-full rounded-md bg-primary text-label text-on-primary disabled:opacity-70'
          : 'min-h-target w-full text-label text-primary-strong disabled:opacity-70'
      }
    >
      {primary ? t('approve.generate') : t('approve.checkNew')}
    </button>
  )
}

function PackageCard({ pkg, lessons }: { pkg: CatchUpPackage; lessons: string[] }) {
  const { t } = useTranslation()
  const heading = t('approve.card.heading', { language: t(`approve.in.${pkg.language}`), count: pkg.children.length })
  const names = pkg.children.map((c) => c.firstName).join(', ')
  const meta = [lessons.join(' + '), pkg.audio && pkg.status !== 'failed' ? duration(pkg.audio.totalSeconds) : null]
    .filter(Boolean)
    .join(' · ')
  return (
    <Link
      to={`/approve/${pkg.id}`}
      aria-label={`${heading}. ${names}. ${meta}. ${t(`approve.status.${pkg.status}`)}`}
      className="flex items-center gap-3 rounded-md border border-border bg-surface p-[14px]"
    >
      <span className="flex min-w-0 flex-1 flex-col gap-1">
        <span className="text-label">{heading}</span>
        <span className="text-body">{names}</span>
        <span className="text-caption text-ink-muted">{meta}</span>
        <span className="mt-1 flex flex-wrap items-center gap-2">
          <StatusTag status={pkg.status} />
          {pkg.status === 'approved' && <span className="text-caption text-ink-muted">{t('approve.card.goesAt')}</span>}
        </span>
      </span>
      <Icon name="chevronRight" size={20} className="shrink-0 text-ink-muted" />
    </Link>
  )
}
