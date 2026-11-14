import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { useAnnounce } from '../../components/useAnnounce'
import { clock } from './clock'
import { Celebration } from './DoneStep'
import { LessonAudioPlayer } from './LessonAudioPlayer'
import { ParentHeader, PrimaryButton, SecondaryButton, StepDots, StopControl } from './ParentParts'
import { Sticks } from './Sticks'
import { sticksText } from './sticksText'
import type { ParentEvening, ParentTodaysMessage, Sign } from './types'
import { usePicture } from './useParentPage'
import { VoiceNote } from './VoiceNote'

type Step = 'concept' | 'activity' | 'video' | 'picture' | 'done'

/** Where each step sits in the four chalk strokes: the video counts with the activity, Done is the fourth (P-cards). */
const DOT: Record<Step, number> = { concept: 1, activity: 2, video: 2, picture: 3, done: 4 }

const SIGNS: Sign[] = ['>', '<', '=']

/** The steps this message has: a section she removed is skipped; the video only when there is one (T-94). */
function todaysSteps(m: ParentTodaysMessage): Step[] {
  const steps: Step[] = []
  if (m.concept) steps.push('concept')
  if (m.steps?.length) steps.push('activity')
  if (m.video) steps.push('video')
  if (m.picture) steps.push('picture')
  steps.push('done')
  return steps
}


/**
 * Today's message for the family (T-91, D-91): her voice and the concept, the activity at home, the video when there is
 * one, the child's picture question (T-90), then Done ⭐. One step per screen, in `?step=` so the phone's Back works.
 * The stop link is on every step. Nothing here is AI except the concept text and voice, which she approved.
 */
export function TodaysPages({
  token,
  message,
  onReason,
}: {
  token: string
  message: ParentEvening & { todaysMessage: ParentTodaysMessage }
  onReason: () => void
}) {
  const { t } = useTranslation()
  const [params, setParams] = useSearchParams()
  const todays = message.todaysMessage
  const steps = todaysSteps(todays)
  const requested = params.get('step') as Step | null
  const first = steps[0] ?? 'done'
  // A reopened link whose question is done opens on Done.
  const step: Step = requested && steps.includes(requested) ? requested : todays.picture?.done ? 'done' : first
  const heading = useRef<HTMLHeadingElement>(null)
  const next = () => {
    const after = steps[steps.indexOf(step) + 1]
    if (after) {
      setParams({ step: after })
    }
  }

  useEffect(() => heading.current?.focus(), [step])

  return (
    <>
      <ParentHeader title={t('parent.evening.title', { teacher: message.teacherName })} />
      <div className="flex flex-col gap-4 px-4 py-4">
        <StepDots step={DOT[step]} />
        <h2 ref={heading} tabIndex={-1} className="text-title font-extrabold">
          {t(`parent.todays.title.${step}`, { child: message.childName })}
        </h2>

        {step === 'concept' && (
          <>
            {message.greeting && (
              <VoiceNote
                url={message.greeting.url}
                label={t('parent.evening.voice', { teacher: message.teacherName, duration: clock(message.greeting.seconds) })}
              />
            )}
            <p lang={message.language} className="card text-body">
              {message.text}
            </p>
            {todays.note && (
              <p lang="kn" className="card text-body">
                <span className="block text-caption text-ink-muted">{t('parent.todays.note', { teacher: message.teacherName })}</span>
                {todays.note}
              </p>
            )}
            <p lang={message.language} className="card text-body">
              {todays.concept}
            </p>
            {message.lessonAudio && <LessonAudioPlayer audio={message.lessonAudio} teacherName={message.teacherName} />}
            <PrimaryButton onClick={next}>{t('parent.todays.next')}</PrimaryButton>
          </>
        )}

        {step === 'activity' && (
          <>
            <p className="text-body text-ink-muted">{t('parent.todays.activityIntro')}</p>
            <ol lang={message.language} className="card flex list-decimal flex-col gap-3 pl-8 text-body">
              {todays.steps?.map((s) => (
                <li key={s}>{s}</li>
              ))}
            </ol>
            <PrimaryButton onClick={next}>{t('parent.todays.next')}</PrimaryButton>
          </>
        )}

        {step === 'video' && todays.video && (
          <>
            <p className="text-body">{t('parent.todays.videoIntro')}</p>
            <a href={todays.video.url} target="_blank" rel="noopener noreferrer" className="btn-secondary flex items-center justify-center">
              {t('parent.todays.watch')}
            </a>
            <p className="text-caption text-ink-muted">{t('parent.todays.videoData')}</p>
            <PrimaryButton onClick={next}>{t('parent.todays.skipVideo')}</PrimaryButton>
          </>
        )}

        {step === 'picture' && todays.picture && (
          <PictureQuestion token={token} picture={todays.picture} childName={message.childName} onDone={next} />
        )}

        {step === 'done' && (
          <>
            <Celebration />
            <p className="text-body">{t('parent.todays.doneBody', { child: message.childName, teacher: message.teacherName })}</p>
            {message.reason ? (
              <p className="flex items-center justify-between gap-3 text-body">
                {t('parent.reason.given')}
                <button type="button" onClick={onReason} className="btn-link">
                  {t('parent.reason.change')}
                </button>
              </p>
            ) : (
              <SecondaryButton onClick={onReason}>{t('parent.reason.entry')}</SecondaryButton>
            )}
          </>
        )}

        <StopControl token={token} childName={message.childName} />
      </div>
    </>
  )
}

/**
 * The child's picture question (T-90, D-93): two numbers from the book, each drawn as bundles of ten and loose sticks,
 * and three big signs. Code scores each tap on the server. A first tap that isn't right gets a gentle "look again" and one
 * more try (D-95: "getting there"); the second tap is final. The phone never learns the answer.
 */
function PictureQuestion({
  token,
  picture,
  childName,
  onDone,
}: {
  token: string
  picture: NonNullable<ParentTodaysMessage['picture']>
  childName: string
  onDone: () => void
}) {
  const { t } = useTranslation()
  const announce = useAnnounce()
  const tap = usePicture(token)
  const [again, setAgain] = useState(picture.firstAnswer !== null && !picture.done)

  if (picture.done) {
    return <PrimaryButton onClick={onDone}>{t('parent.todays.next')}</PrimaryButton>
  }

  const choose = (sign: Sign) =>
    tap.mutate(sign, {
      onSuccess: (result) => {
        if (result.done) {
          announce(t('parent.todays.thanks'))
          onDone()
        } else {
          setAgain(true)
          announce(t('parent.todays.lookAgain'))
        }
      },
    })

  return (
    <>
      <p className="text-body">{t('parent.todays.pictureIntro', { child: childName })}</p>
      <div className="card flex items-end justify-around gap-2">
        {[picture.a, picture.b].map((n, i) => (
          <figure key={i} className="flex flex-col items-center gap-2">
            <Sticks n={n} />
            <figcaption className="text-display font-extrabold">
              {n}
              <span className="sr-only"> ({sticksText(t, n)})</span>
            </figcaption>
          </figure>
        ))}
      </div>
      {again && (
        <output className="block rounded-card border-l-4 border-l-accent bg-panel p-3 text-body">
          {t('parent.todays.lookAgain')}
        </output>
      )}
      <fieldset className="grid grid-cols-3 gap-3">
        <legend className="sr-only">{t('parent.todays.signs', { a: picture.a, b: picture.b })}</legend>
        {SIGNS.map((sign) => (
          <button
            key={sign}
            type="button"
            disabled={tap.isPending}
            aria-label={t(`parent.todays.sign.${sign === '>' ? 'more' : sign === '<' ? 'less' : 'equal'}`, { a: picture.a, b: picture.b })}
            onClick={() => choose(sign)}
            className="min-h-[88px] rounded-button border-[3px] border-ink bg-surface text-[40px] font-extrabold text-ink active:scale-95 disabled:opacity-60"
          >
            {sign}
          </button>
        ))}
      </fieldset>
      {tap.isError && (
        <p role="alert" className="text-caption text-error">
          {t('parent.todays.sendFailed')}
        </p>
      )}
    </>
  )
}
