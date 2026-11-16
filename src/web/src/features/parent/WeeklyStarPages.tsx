import { useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { StarGlyph } from '../../components/StarGlyph'
import { ParentHeader, PrimaryButton, StopControl } from './ParentParts'
import type { ParentWeeklyStar } from './types'

/** Confetti for the badge card: where each piece rests, as % of the card. It bursts out from the medal once. */
const BURST: { x: number; y: number; amber: boolean; round: boolean }[] = [
  { x: 8, y: 16, amber: true, round: true }, { x: 22, y: 6, amber: false, round: false }, { x: 36, y: 10, amber: true, round: false },
  { x: 64, y: 8, amber: false, round: true }, { x: 80, y: 12, amber: true, round: false }, { x: 92, y: 30, amber: false, round: true },
  { x: 6, y: 44, amber: false, round: false }, { x: 14, y: 70, amber: true, round: true }, { x: 28, y: 86, amber: false, round: true },
  { x: 46, y: 90, amber: true, round: false }, { x: 66, y: 88, amber: false, round: false }, { x: 84, y: 78, amber: true, round: true },
  { x: 94, y: 56, amber: false, round: false }, { x: 74, y: 26, amber: true, round: true },
]

/**
 * The weekly star page (T-125, D-118), two screens: the message she approved with "Open to see your star badge", then
 * the badge card with one short confetti burst (about 2 s) that then rests; none when the phone asks for reduced motion
 * (the global rule in tokens.css). No teacher voice and no numbers. The badge is `?step=badge`, so the phone's Back
 * returns to the message.
 */
export function WeeklyStarPages({ token, page }: { token: string; page: ParentWeeklyStar }) {
  const { t } = useTranslation()
  const [params, setParams] = useSearchParams()
  const badge = params.get('step') === 'badge'
  const heading = useRef<HTMLHeadingElement>(null)
  const teacherLine = t('parent.evening.title', { teacher: page.teacherName })

  useEffect(() => heading.current?.focus(), [badge])

  return (
    <>
      <ParentHeader
        title={t(badge ? 'parent.star.badgeTitle' : 'parent.star.title')}
        onBack={badge ? () => setParams({}) : undefined}
        headingRef={heading}
        subtitle={teacherLine}
      />
      <div className="flex flex-col gap-4 px-4 py-4">
        {badge ? (
          <>
            <StarBadge />
            <p lang={page.language} className="card text-body-lg font-bold">
              {page.badge}
            </p>
          </>
        ) : (
          <>
            <p lang={page.language} className="card text-body">
              {page.message}
            </p>
            <PrimaryButton onClick={() => setParams({ step: 'badge' })}>
              <span className="inline-flex items-center justify-center gap-2">
                <StarGlyph size={22} />
                {t('parent.star.open')}
              </span>
            </PrimaryButton>
          </>
        )}
        <StopControl token={token} childName={page.childName} />
      </div>
    </>
  )
}

/** The badge: the amber medal with a star, and confetti that bursts out once and rests. Decorative only. */
function StarBadge() {
  return (
    <div
      aria-hidden="true"
      data-testid="star-badge"
      className="relative flex h-[200px] w-full items-center justify-center overflow-hidden rounded-card border-[1.5px] border-edge bg-panel"
    >
      {BURST.map((piece, i) => (
        <span
          key={i}
          style={{
            left: `${piece.x}%`,
            top: `${piece.y}%`,
            // From the medal in the middle out to where the piece rests.
            ['--from-x' as string]: `${(50 - piece.x) * 3}px`,
            ['--from-y' as string]: `${(50 - piece.y) * 2}px`,
            animationDelay: `${i * 20}ms`,
          }}
          className={`confetti-burst absolute ${piece.round ? 'size-3 rounded-full' : 'h-2 w-4 -rotate-12 rounded-sm'} ${
            piece.amber ? 'bg-accent' : 'bg-primary'
          }`}
        />
      ))}
      <span className="relative flex size-[132px] items-center justify-center rounded-full bg-surface">
        <span className="flex size-[116px] items-center justify-center rounded-full border-[3px] border-ink bg-accent">
          <svg viewBox="0 0 48 48" width="60" height="60" fill="none" strokeLinecap="round" strokeLinejoin="round">
            <path
              pathLength="100"
              d="M24 6 L29.5 18.5 L43 20 L33 29 L36 42 L24 35.5 L12 42 L15 29 L5 20 L18.5 18.5 Z"
              strokeWidth="4"
              className="draw-on fill-surface stroke-ink"
            />
          </svg>
        </span>
      </span>
    </div>
  )
}
