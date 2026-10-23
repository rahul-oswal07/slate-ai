import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Icon } from '../../components/Icon'
import { useAnnounce } from '../../components/useAnnounce'
import { useFocusOnMount } from '../../components/useFocusOnMount'
import { useMe } from '../auth/useMe'
import { istToday } from '../today/schoolTime'
import type { BuddyCard } from './types'
import { useBuddyCards, useSaveTicks } from './useBuddyCards'

/** "was present both days" fits only 2 days (T5 §5): pick the wording from the numbers. */
function buddyLabel(t: ReturnType<typeof useTranslation>['t'], present: number, missed: number) {
  if (present < missed) {
    return t('back.buddySome', { present, count: missed })
  }
  return missed === 1
    ? t('back.buddyOneDay')
    : missed === 2
      ? t('back.buddyBothDays')
      : t('back.buddyAllDays', { count: missed })
}

/** The section title: sums to work out, numbers to build with sticks, or questions. */
function itemsTitle(t: ReturnType<typeof useTranslation>['t'], kind: string | null) {
  if (kind === 'count') {
    return t('back.makeTitle')
  }
  return kind === 'placeValue' || kind === 'biggest' || kind === 'smallest'
    ? t('back.questionsTitle')
    : t('back.sumsTitle')
}

/** One item as the children read it. Numbers stay Latin digits, as in the textbook. */
function itemText(t: ReturnType<typeof useTranslation>['t'], kind: string | null, question: string) {
  switch (kind) {
    case 'count':
      return t('back.task.count', { n: question })
    case 'placeValue':
    case 'biggest':
    case 'smallest':
      return t(`back.task.${kind}`, { question })
    default:
      // add, sub, expanded: a sum to work out.
      return `${question} = ?`
  }
}

/**
 * T5 Back today (FR-12): a buddy card for every child who came back, one at a time with Prev / Next, printable.
 * Everything on the card is made by code (no AI). Never shows the family's channel or a score (B-20).
 */
export function BackTodayScreen() {
  const { t } = useTranslation()
  const headingRef = useFocusOnMount<HTMLHeadingElement>()
  const classId = useMe().data?.classId ?? ''
  const today = istToday()
  const query = useBuddyCards(classId, today)
  const [index, setIndex] = useState(0)
  const cards = query.data?.cards ?? []
  const current = Math.min(index, Math.max(cards.length - 1, 0))
  const card = cards[current]

  return (
    <section aria-labelledby="back-title" className="flex flex-col gap-4 px-4 py-4">
      <h1 id="back-title" ref={headingRef} tabIndex={-1} className="text-title print:hidden">
        {t('back.title')}
      </h1>

      {query.data?.savedAt && (
        <output className="block border-l-4 border-l-warning bg-surface p-3 text-body text-ink print:hidden">
          {t('back.offline')}
        </output>
      )}

      {query.isError && !query.data ? (
        <div role="alert" className="flex flex-col gap-3 border-l-4 border-l-error bg-error-bg p-3 text-body text-ink">
          {t('back.loadFailed')}
          <button
            type="button"
            onClick={() => void query.refetch()}
            className="min-h-target rounded-md border border-border bg-surface px-4 text-label"
          >
            {t('common.tryAgain')}
          </button>
        </div>
      ) : query.data && cards.length === 0 ? (
        <div className="flex flex-col items-center gap-3 py-8 text-center">
          <Icon name="users" size={56} className="text-ink-muted" />
          <h2 className="text-title">{t('back.noneTitle')}</h2>
          <p className="text-body text-ink-muted">{t('back.noneBody')}</p>
        </div>
      ) : card ? (
        <Cards classId={classId} date={today} cards={cards} index={current} onMove={(next) => setIndex(next)} />
      ) : null}
    </section>
  )
}

function Cards({
  classId,
  date,
  cards,
  index,
  onMove,
}: {
  classId: string
  date: string
  cards: BuddyCard[]
  index: number
  onMove: (index: number) => void
}) {
  const { t } = useTranslation()
  const announce = useAnnounce()
  const save = useSaveTicks(classId, date)
  const nameRef = useRef<HTMLHeadingElement>(null)
  const moved = useRef(false)
  const card = cards[index] as BuddyCard
  const next = cards[index + 1]

  // Moving between cards keeps focus on the card's heading (B-19); not on first open (the page title has it).
  useEffect(() => {
    if (moved.current) {
      nameRef.current?.focus()
    }
  }, [index])

  const move = (to: number) => {
    moved.current = true
    save.reset()
    onMove(to)
  }

  return (
    <>
      <p className="text-caption text-ink-muted print:hidden">
        {t('back.position', { i: index + 1, n: cards.length })}
        {next && ` · ${t('back.next', { name: next.firstName })}`}
      </p>

      <article
        aria-labelledby={`card-${card.childId}`}
        className="flex flex-col gap-4 rounded-md border-2 border-border bg-surface p-4 print:border-black print:text-black"
      >
        <div>
          <h2 id={`card-${card.childId}`} ref={nameRef} tabIndex={-1} className="text-title">
            {card.firstName}
          </h2>
          <p className="text-caption text-ink-muted">{t('back.daysAway', { count: card.daysAway })}</p>
        </div>

        <div className="flex items-start gap-3 rounded-md border border-border bg-panel p-3 print:bg-white">
          <Icon name="users" size={22} className="mt-1 shrink-0 text-primary-strong" />
          {card.buddy ? (
            <div>
              <p className="text-caption text-primary-strong">
                {buddyLabel(t, card.buddy.presentDays, card.missedDays)}
              </p>
              <p className="text-label">
                {card.buddy.firstName} · {t('back.minutes', { n: 10 })}
              </p>
            </div>
          ) : (
            <div>
              <p className="text-label">{t('back.teacherLed')}</p>
              <p className="text-caption text-ink-muted">{t('back.teacherLedWhy')}</p>
            </div>
          )}
        </div>

        {card.competencyId === null ? (
          <p className="text-body text-ink-muted">{t('back.noLesson')}</p>
        ) : (
          <>
            {card.sums.length > 0 && (
              <section aria-labelledby={`sums-${card.childId}`} className="flex flex-col gap-2">
                <h3 id={`sums-${card.childId}`} className="text-caption font-bold text-ink-muted">
                  {itemsTitle(t, card.practiceKind)}
                </h3>
                <ol className="flex flex-col gap-1">
                  {card.sums.map((sum) => (
                    <li key={sum.question} className="text-title font-bold">
                      {itemText(t, card.practiceKind, sum.question)}
                    </li>
                  ))}
                </ol>
                {/* Making a number with sticks has nothing to look up: no answers to show. */}
                {card.practiceKind !== 'count' && (
                  <details className="text-body print:hidden">
                    <summary className="flex min-h-target cursor-pointer items-center text-label text-primary-strong">
                      {t('back.answers')}
                    </summary>
                    <ol className="flex flex-col gap-1 pb-1">
                      {card.sums.map((sum) => (
                        <li key={sum.question} lang="en">
                          {sum.question} → {sum.answer}
                        </li>
                      ))}
                    </ol>
                  </details>
                )}
              </section>
            )}

            {card.keyWordsKn.length > 0 && (
              <section aria-labelledby={`words-${card.childId}`} className="flex flex-col gap-2">
                <h3 id={`words-${card.childId}`} className="text-caption font-bold text-ink-muted">
                  {t('back.keywordsTitle')}
                </h3>
                <p className="flex flex-wrap gap-2">
                  {card.keyWordsKn.map((word) => (
                    <span key={word} lang="kn" className="rounded-sm bg-panel px-2 py-1 text-label print:bg-white">
                      {word}
                    </span>
                  ))}
                </p>
              </section>
            )}

            {card.homework && (
              <div className="flex min-h-target items-center gap-3 border-t border-divider pt-3">
                <input
                  id={`homework-${card.childId}`}
                  type="checkbox"
                  checked={card.homeworkReturned}
                  disabled={save.isPending}
                  aria-describedby={`homework-page-${card.childId}`}
                  onChange={(event) => save.mutate({ childId: card.childId, homeworkReturned: event.target.checked })}
                  className="size-6 shrink-0 accent-primary"
                />
                <span className="flex flex-col">
                  <label htmlFor={`homework-${card.childId}`} className="text-label">
                    {t('back.homework')}
                  </label>
                  <span id={`homework-page-${card.childId}`} className="text-caption text-ink-muted">
                    {card.homework.part === null
                      ? t('back.homeworkPageNoPart', { page: card.homework.page })
                      : t('back.homeworkPage', { part: card.homework.part, page: card.homework.page })}
                  </span>
                </span>
              </div>
            )}
          </>
        )}
      </article>

      {save.isError && (
        <p role="alert" className="border-l-4 border-l-error bg-error-bg p-3 text-body text-ink print:hidden">
          {t('back.saveFailed')}
        </p>
      )}

      <div className="flex items-center justify-center gap-2 print:hidden">
        <button
          type="button"
          aria-label={t('back.prev')}
          disabled={index === 0}
          onClick={() => move(index - 1)}
          className="flex min-h-target min-w-target items-center justify-center rounded-md border border-border bg-surface text-ink disabled:border-divider disabled:bg-canvas disabled:text-ink-muted"
        >
          <Icon name="arrowLeft" />
        </button>
        <button
          type="button"
          aria-label={t('back.print')}
          onClick={() => window.print()}
          className="flex min-h-target min-w-target items-center justify-center rounded-md border border-border bg-surface text-ink"
        >
          <Icon name="printer" />
        </button>
        <button
          type="button"
          aria-label={t('back.nextCard')}
          disabled={index === cards.length - 1}
          onClick={() => move(index + 1)}
          className="flex min-h-target min-w-target items-center justify-center rounded-md border border-border bg-surface text-ink disabled:border-divider disabled:bg-canvas disabled:text-ink-muted"
        >
          <Icon name="chevronRight" />
        </button>
      </div>

      <div className="print:hidden">
        {card.done ? (
          <div className="flex items-center justify-between gap-3 border-l-4 border-l-primary bg-surface p-3">
            <span className="flex items-center gap-2 text-body">
              <Icon name="checkCircle" size={20} className="text-primary-strong" />
              {t('back.doneBanner')}
            </span>
            <button
              type="button"
              disabled={save.isPending}
              onClick={() => save.mutate({ childId: card.childId, done: false })}
              className="min-h-target rounded-md px-3 text-label text-primary-strong underline"
            >
              {t('back.undo')}
            </button>
          </div>
        ) : (
          <button
            type="button"
            disabled={save.isPending}
            onClick={() =>
              save.mutate({ childId: card.childId, done: true }, { onSuccess: () => announce(t('announce.buddyDone')) })
            }
            className="min-h-target-primary w-full rounded-md bg-primary px-4 text-label text-on-primary disabled:opacity-60"
          >
            {t('back.done')}
          </button>
        )}
      </div>
    </>
  )
}
