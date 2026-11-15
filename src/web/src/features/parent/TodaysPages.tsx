import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { useAnnounce } from '../../components/useAnnounce'
import { clock } from './clock'
import { Celebration } from './DoneStep'
import { LessonAudioPlayer } from './LessonAudioPlayer'
import { ParentHeader, PrimaryButton, SecondaryButton, StepDots, StopControl } from './ParentParts'
import { ReasonView } from './ReasonView'
import { Sticks } from './Sticks'
import { sticksText } from './sticksText'
import type { ParentEvening, ParentTodaysMessage, Sign } from './types'
import { usePicture } from './useParentPage'
import { VoiceNote } from './VoiceNote'

type Step = 'voice' | 'reason' | 'concept' | 'activity' | 'video' | 'picture' | 'done'

/**
 * Where each step sits in the four chalk strokes: her voice, the reason and the concept are the first, the video counts
 * with the activity, Done is the fourth (P-cards).
 */
const DOT: Record<Step, number> = { voice: 1, reason: 1, concept: 1, activity: 2, video: 2, picture: 3, done: 4 }

const SIGNS: Sign[] = ['>', '<', '=']

/**
 * The steps this message has. Her voice first, then "Is everything OK?" (D-98) unless the family already told us (it
 * stays while they are on it, so sending doesn't pull the page away); a section she removed is skipped; the video only
 * when there is one (T-94).
 */
function todaysSteps(m: ParentTodaysMessage, reasonGiven: boolean, current: string | null): Step[] {
  const steps: Step[] = ['voice']
  if (!reasonGiven || current === 'reason') steps.push('reason')
  if (m.concept) steps.push('concept')
  if (m.steps?.length) steps.push('activity')
  if (m.video) steps.push('video')
  if (m.picture) steps.push('picture')
  steps.push('done')
  return steps
}


/**
 * Today's message for the family (T-91, D-91): her voice and the concept, the activity at home, the video when there is
 * one, the child's picture question (T-90), then Done ⭐. One step per screen, in `?step=` so the phone's Back works;
 * each step has Back (but the first) and its title in one row, then the teacher line (T-116). A reopened link starts at
 * the first step again. The stop link is on every step. Nothing here is AI except the concept and activity text and their
 * voices (the steps are voiced since T-120), which she approved.
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
  const requested = params.get('step') as Step | null
  const steps = todaysSteps(todays, message.reason !== null, requested)
  const first = steps[0] ?? 'done'
  // A reopened link starts at the first step again (T-116); the answer page then shows open or locked.
  const step: Step = requested && steps.includes(requested) ? requested : first
  const heading = useRef<HTMLHeadingElement>(null)
  const next = () => {
    const after = steps[steps.indexOf(step) + 1]
    if (after) {
      setParams({ step: after })
    }
  }
  // Back to the step before in this family's own list (removed sections stay skipped); none on the first.
  const before = steps[steps.indexOf(step) - 1]
  const back = before ? () => setParams({ step: before }) : undefined
  const teacherLine = t('parent.evening.title', { teacher: message.teacherName })

  useEffect(() => heading.current?.focus(), [step])

  // "Is everything OK?" (FR-21, D-98): the evening page's own reason screen, as a step. Send or Skip moves on; Back goes
  // back to her voice. Optional: nothing here blocks the lesson.
  if (step === 'reason') {
    return <ReasonView token={token} message={message} onDone={next} onBack={back} />
  }

  return (
    <>
      <ParentHeader
        title={t(`parent.todays.title.${step}`, { child: message.childName })}
        onBack={back}
        headingRef={heading}
        subtitle={teacherLine}
      />
      <div className="flex flex-col gap-4 px-4 py-4">
        <StepDots step={DOT[step]} />

        {step === 'voice' && (
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
              // In the family's language: A9 writes it from her Kannada (T-119).
              <p lang={message.language} className="card text-body">
                <span className="block text-caption text-ink-muted">{t('parent.todays.note', { teacher: message.teacherName })}</span>
                {todays.note}
              </p>
            )}
            <PrimaryButton onClick={next}>{t('parent.todays.next')}</PrimaryButton>
          </>
        )}

        {step === 'concept' && (
          <>
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
            {/* The steps read aloud (T-120), for a parent who reads little; the words stay above. */}
            {message.activityAudio && (
              <LessonAudioPlayer audio={message.activityAudio} teacherName={message.teacherName} title={t('parent.todays.activityListen')} />
            )}
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
          <PictureQuestion token={token} picture={todays.picture} childName={message.childName} onNext={next} />
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
 * and three big signs. Tapping a sign only chooses it; Submit sends it (D-108). Nothing says right or wrong: after the
 * first submit the page shows their choice with Try again and Next; after the second it is locked, with Next only. Code
 * scores the latest on the server; the phone never learns the answer.
 */
function PictureQuestion({
  token,
  picture,
  childName,
  onNext,
}: {
  token: string
  picture: NonNullable<ParentTodaysMessage['picture']>
  childName: string
  onNext: () => void
}) {
  const { t } = useTranslation()
  const announce = useAnnounce()
  const submit = usePicture(token)
  const latest = picture.secondAnswer ?? picture.firstAnswer
  const [retrying, setRetrying] = useState(false)
  const [chosen, setChosen] = useState<Sign | null>(null)
  const open = latest === null || (retrying && !picture.locked)
  const label = (sign: Sign) =>
    t(`parent.todays.sign.${sign === '>' ? 'more' : sign === '<' ? 'less' : 'equal'}`, { a: picture.a, b: picture.b })

  const send = () => {
    if (chosen === null) {
      return
    }
    announce(t('parent.announce.sending'))
    submit.mutate(chosen, {
      onSuccess: () => {
        announce(t('parent.announce.sent'))
        setRetrying(false)
        setChosen(null)
      },
    })
  }

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
      {open ? (
        <>
          <fieldset className="grid grid-cols-3 gap-3">
            <legend className="sr-only">{t('parent.todays.signs', { a: picture.a, b: picture.b })}</legend>
            {SIGNS.map((sign) => (
              <button
                key={sign}
                type="button"
                aria-pressed={sign === chosen}
                disabled={submit.isPending}
                aria-label={label(sign)}
                onClick={() => {
                  submit.reset()
                  setChosen(sign)
                }}
                className={`min-h-[88px] rounded-button text-[40px] font-extrabold text-ink active:scale-95 disabled:opacity-60 ${
                  sign === chosen ? 'border-[4px] border-primary bg-panel' : 'border-[3px] border-ink bg-surface'
                }`}
              >
                {sign}
              </button>
            ))}
          </fieldset>
          {submit.isError && (
            <p role="alert" className="text-caption text-error">
              {t('parent.todays.sendFailed')}
            </p>
          )}
          <PrimaryButton onClick={send} disabled={chosen === null || submit.isPending}>
            {submit.isPending ? t('parent.answer.sending') : t('parent.todays.submit')}
          </PrimaryButton>
        </>
      ) : (
        <>
          <p className="card flex items-center justify-between gap-3 text-body">
            <span>{t('parent.answer.yours')}</span>
            <span aria-label={latest ? label(latest) : undefined} className="text-[40px] font-extrabold text-ink">
              {latest}
            </span>
          </p>
          {!picture.locked && (
            <SecondaryButton
              onClick={() => {
                setRetrying(true)
                setChosen(null)
              }}
            >
              {t('parent.answer.tryAgain')}
            </SecondaryButton>
          )}
          <PrimaryButton onClick={onNext}>{t('parent.next')}</PrimaryButton>
        </>
      )}
    </>
  )
}
