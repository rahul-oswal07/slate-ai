import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AccentTitle } from '../../components/AccentWord'
import { StarGlyph } from '../../components/StarGlyph'
import { PrimaryButton } from './ParentParts'
import type { ParentCatchUp } from './types'

const CONFETTI: { x: number; y: number; kind: 'dot' | 'bit' | 'squiggle'; amber: boolean }[] = [
  { x: 8, y: 14, kind: 'dot', amber: true }, { x: 24, y: 8, kind: 'bit', amber: true }, { x: 38, y: 6, kind: 'bit', amber: false },
  { x: 72, y: 10, kind: 'dot', amber: false }, { x: 84, y: 14, kind: 'dot', amber: true }, { x: 94, y: 30, kind: 'bit', amber: true },
  { x: 6, y: 34, kind: 'squiggle', amber: false }, { x: 16, y: 52, kind: 'dot', amber: false }, { x: 12, y: 74, kind: 'bit', amber: true },
  { x: 26, y: 84, kind: 'dot', amber: false }, { x: 42, y: 86, kind: 'squiggle', amber: false }, { x: 66, y: 86, kind: 'squiggle', amber: false },
  { x: 90, y: 80, kind: 'dot', amber: true }, { x: 84, y: 58, kind: 'bit', amber: false },
]

/** The banner on Done (D-77): an amber medal with a chalk star drawn on, and a little confetti. Decorative only. */
function Celebration() {
  return (
    <div aria-hidden="true" className="relative flex h-[172px] w-full items-center justify-center overflow-hidden rounded-card border-[1.5px] border-edge bg-panel">
      {CONFETTI.map((piece, i) => (
        <span
          key={i}
          style={{ left: `${piece.x}%`, top: `${piece.y}%`, animationDelay: `${i * 8}ms` }}
          className={`pop-in absolute ${
            piece.kind === 'dot'
              ? `size-3 rounded-full ${piece.amber ? 'bg-accent' : 'bg-primary'}`
              : piece.kind === 'bit'
                ? `h-2 w-4 -rotate-12 rounded-sm ${piece.amber ? 'bg-accent' : 'bg-primary'}`
                : 'h-1 w-6 rounded-pill bg-chalk'
          }`}
        />
      ))}
      <span className="relative flex size-[120px] items-center justify-center rounded-full bg-surface">
        <span className="flex size-[104px] items-center justify-center rounded-full border-[3px] border-ink bg-accent">
          <svg viewBox="0 0 48 48" width="52" height="52" fill="none" strokeLinecap="round" strokeLinejoin="round">
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

/** P4 Done: "Well done" whatever the answer (D-20); the teacher sees it; bring the page. No score anywhere. */
export function DoneStep({ page }: { page: ParentCatchUp }) {
  const { t } = useTranslation()
  const [finished, setFinished] = useState(false)
  return (
    <div className="flex flex-col items-center gap-4 py-2 text-center">
      <Celebration />
      <p className="text-display font-extrabold">
        <AccentTitle title={t('parent.done.wellDone', { child: page.childName })} accent={page.childName} />
      </p>
      <p className="flex items-start justify-center gap-2 text-body-lg">
        <StarGlyph size={22} />
        <span>{t('parent.done.star', { child: page.childName })}</span>
      </p>
      <p className="text-body-lg">{t('parent.done.teacherSees', { teacher: page.teacherName })}</p>
      {page.homework && (
        <p className="text-caption text-ink-muted">{t('parent.done.bringPage', { page: page.homework.page })}</p>
      )}
      {finished ? (
        <output className="block text-body">{t('parent.done.closeHint')}</output>
      ) : (
        <PrimaryButton onClick={() => setFinished(true)}>{t('parent.done.finish')}</PrimaryButton>
      )}
    </div>
  )
}
