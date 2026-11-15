import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { fakeApi, renderApp, type FakeRoute } from '../../test/renderApp'
import type { ParentEvening, ParentTodaysMessage } from './types'

const i18n = initI18n()
const token = 'ZyXwVuTsRqPoNmLkJiHgFe'
const pictureUrl = `POST /api/p/${token}/picture`

function todays(overrides: Partial<ParentTodaysMessage> = {}): ParentTodaysMessage {
  return {
    concept: 'Look at the bundles of ten first.',
    steps: ['Make 75 and 57 with sticks.', 'Ask which has more bundles.'],
    picture: { a: 48, b: 33, firstAnswer: null, secondAnswer: null, locked: false },
    video: null,
    note: null,
    ...overrides,
  }
}

function evening(t: ParentTodaysMessage | null = todays()): ParentEvening {
  return {
    kind: 'evening',
    language: 'en',
    childName: 'Manjunath',
    teacherName: 'Shobha',
    text: 'Manjunath was away today. The class learned More, less, equal (Nalikali Maths · Part 1 · p 4).',
    greeting: null,
    lessonAudio: null,
    stopped: false,
    reason: null,
    todaysMessage: t,
  }
}

function setup(page: ParentEvening, extra: Record<string, FakeRoute> = {}) {
  return fakeApi({ [`GET /api/p/${token}`]: () => ({ status: 200, body: page }), ...extra })
}

describe("Family page for today's message (T-91, T-90)", () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('goes voice → is everything OK? → concept → activity → picture → done, with the stop link on every step', async () => {
    const user = userEvent.setup()
    setup(evening(), { [pictureUrl]: () => ({ status: 200, body: { attempt: 1, locked: false } }) })
    renderApp(`/p/${token}`)

    expect(await screen.findByRole('heading', { name: 'What Manjunath learned today' })).toBeInTheDocument()
    expect(screen.getByText(/Nalikali Maths · Part 1 · p 4/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Stop these messages' })).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Next' }))

    // D-98: "Is everything OK?" right after her voice; Skip never blocks the lesson.
    expect(screen.getByText('We missed Manjunath today. Is everything OK?')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Stop these messages' })).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Skip' }))

    expect(screen.getByRole('heading', { name: 'The idea, in a few words' })).toBeInTheDocument()
    expect(screen.getByText('Look at the bundles of ten first.')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Next' }))

    expect(screen.getByRole('heading', { name: 'Try this at home' })).toBeInTheDocument()
    expect(screen.getAllByRole('listitem')).toHaveLength(2)
    await user.click(screen.getByRole('button', { name: 'Next' }))

    expect(screen.getByRole('heading', { name: "Manjunath's turn" })).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: '48 is more than 33' }))
    await user.click(screen.getByRole('button', { name: 'Submit' }))
    await user.click(await screen.findByRole('button', { name: 'Next' }))

    expect(await screen.findByRole('heading', { name: 'Well done!' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Is everything OK/ })).toBeInTheDocument()
  })

  it('every step has Back and its title in one row above the teacher line; none on the first step (T-116)', async () => {
    const user = userEvent.setup()
    setup(evening())
    renderApp(`/p/${token}`)

    const first = await screen.findByRole('heading', { level: 1, name: 'What Manjunath learned today' })
    expect(first).toHaveFocus()
    expect(screen.queryByRole('button', { name: 'Back' })).not.toBeInTheDocument()
    expect(screen.getByText('Message from Shobha teacher')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Next' }))
    expect(screen.getByRole('heading', { level: 1, name: 'A quick question' })).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Skip' }))
    expect(screen.getByRole('heading', { level: 1, name: 'The idea, in a few words' })).toBeInTheDocument()
    expect(screen.getAllByRole('heading', { name: 'The idea, in a few words' })).toHaveLength(1) // not repeated below

    await user.click(screen.getByRole('button', { name: 'Back' }))
    expect(await screen.findByRole('heading', { level: 1, name: 'A quick question' })).toBeInTheDocument()
  })

  it('a reason chosen on the step is sent, thanked, and the lesson carries on', async () => {
    const user = userEvent.setup()
    const calls = setup(evening(), { [`POST /api/p/${token}/reason`]: () => ({ status: 200, body: { category: 'unwell' } }) })
    renderApp(`/p/${token}?step=reason`)

    await user.click(await screen.findByRole('button', { name: /Unwell/ }))
    await user.click(screen.getByRole('button', { name: 'Send' }))
    expect(await screen.findByRole('heading', { name: 'Thank you' })).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'OK' }))

    expect(await screen.findByRole('heading', { name: 'The idea, in a few words' })).toBeInTheDocument()
    expect(calls.find((c) => c.key === `POST /api/p/${token}/reason`)?.body).toMatchObject({ category: 'unwell' })
  })

  it('Skip sends nothing, and a family who already told us is not asked again', async () => {
    const user = userEvent.setup()
    const calls = setup(evening())
    renderApp(`/p/${token}?step=reason`)
    await user.click(await screen.findByRole('button', { name: 'Skip' }))

    expect(await screen.findByRole('heading', { name: 'The idea, in a few words' })).toBeInTheDocument()
    expect(calls.some((c) => c.key.startsWith('POST'))).toBe(false)
  })

  it('a reopened link with a reason goes from her voice straight to the concept', async () => {
    const user = userEvent.setup()
    setup({ ...evening(), reason: 'festival' })
    renderApp(`/p/${token}`)

    await user.click(await screen.findByRole('button', { name: 'Next' }))

    expect(screen.getByRole('heading', { name: 'The idea, in a few words' })).toBeInTheDocument()
  })

  it('the picture question shows both numbers as sticks and three big signs, and the page never has the answer', async () => {
    setup(evening())
    renderApp(`/p/${token}?step=picture`)

    const group = await screen.findByRole('group', { name: 'Which sign goes between 48 and 33?' })
    expect(group.querySelectorAll('button')).toHaveLength(3)
    expect(screen.getByText('48')).toBeInTheDocument()
    expect(screen.getByText(/4 bundles of ten, 8 loose sticks/)).toBeInTheDocument()
  })

  it('choose, then submit; never right or wrong; Try again once, then locked (D-108)', async () => {
    const user = userEvent.setup()
    let attempt = 0
    const calls = setup(evening(), {
      [pictureUrl]: () => {
        attempt++
        return { status: 200, body: { attempt, locked: attempt === 2 } }
      },
    })
    renderApp(`/p/${token}?step=picture`)

    // Tapping only chooses; nothing is sent until Submit.
    await user.click(await screen.findByRole('button', { name: '48 is less than 33' }))
    expect(screen.getByRole('button', { name: '48 is less than 33' })).toHaveAttribute('aria-pressed', 'true')
    await user.click(screen.getByRole('button', { name: '48 is equal to 33' }))
    expect(calls.some((c) => c.key === pictureUrl)).toBe(false)
    await user.click(screen.getByRole('button', { name: 'Submit' }))

    expect(await screen.findByText('Your answer')).toBeInTheDocument()
    expect(screen.queryByText(/wrong|right|look again/i)).not.toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Try again' }))
    await user.click(screen.getByRole('button', { name: '48 is more than 33' }))
    await user.click(screen.getByRole('button', { name: 'Submit' }))

    expect(await screen.findByText('Your answer')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Try again' })).not.toBeInTheDocument() // locked
    expect(calls.filter((c) => c.key === pictureUrl).map((c) => (c.body as { sign: string }).sign)).toEqual(['=', '>'])
    const ids = calls.filter((c) => c.key === pictureUrl).map((c) => (c.body as { clientRequestId: string }).clientRequestId)
    expect(new Set(ids).size).toBe(2) // each submit is its own request

    await user.click(screen.getByRole('button', { name: 'Next' }))
    expect(await screen.findByRole('heading', { name: 'Well done!' })).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Back' }))
    expect(await screen.findByText('Your answer')).toBeInTheDocument() // Back from Done: the locked answer page
  })

  it('sections she removed are skipped, and the video step appears only with a video', async () => {
    const user = userEvent.setup()
    setup(evening(todays({ steps: null, video: { url: 'https://diksha.gov.in/play/content/do_1' } })))
    renderApp(`/p/${token}?step=concept`)

    await user.click(await screen.findByRole('button', { name: 'Next' }))

    expect(screen.getByRole('heading', { name: "Watch how it's taught" })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Watch the video' })).toHaveAttribute('href', 'https://diksha.gov.in/play/content/do_1')
    expect(screen.getByText(/Uses mobile data/)).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Next (skip the video)' }))
    expect(screen.getByRole('heading', { name: "Manjunath's turn" })).toBeInTheDocument()
  })

  it('the activity steps can be heard, after the AI notice, with the words still shown (T-120)', async () => {
    const user = userEvent.setup()
    const audio: NonNullable<ParentEvening['activityAudio']> = {
      segments: [
        { kind: 'disclosure', url: `/api/p/${token}/lesson-audio/d1`, seconds: 4 },
        { kind: 'script', url: `/api/p/${token}/lesson-audio/s1`, seconds: 8 },
      ],
      totalSeconds: 12,
      disclosureText: 'A computer voice reads this.',
      transcript: 'Make 75 and 57 with sticks. Ask which has more bundles.',
    }
    setup({ ...evening(), reason: 'unwell', activityAudio: audio })
    renderApp(`/p/${token}?step=activity`)

    expect(await screen.findByRole('heading', { name: 'Try this at home' })).toBeInTheDocument()
    expect(screen.getByText('Make 75 and 57 with sticks.')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Listen to the steps · 0:12' })).toBeInTheDocument()
    expect(screen.getByText('A computer voice reads this.')).toBeInTheDocument() // the AI notice's words (FR-09)

    await user.click(screen.getByRole('button', { name: 'Next' }))
    expect(screen.queryByRole('heading', { name: /Listen to the steps/ })).not.toBeInTheDocument()
  })

  it('without a clip the activity is text only', async () => {
    setup({ ...evening(), reason: 'unwell' })
    renderApp(`/p/${token}?step=activity`)

    expect(await screen.findByRole('heading', { name: 'Try this at home' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: /Listen to the steps/ })).not.toBeInTheDocument()
  })

  it('her note shows with her name, marked in the family language it arrives in (T-119)', async () => {
    // The API sends her note in the family's language (A9 writes it from her Kannada), so a screen reader must read
    // it as that language, not as Kannada.
    setup({ ...evening(todays({ note: 'మేము ఆకులపై సంఖ్యలు రాసి పోల్చాము.' })), language: 'te' })
    renderApp(`/p/${token}`)

    expect(await screen.findByText(i18n.t('parent.todays.note', { teacher: 'Shobha', lng: 'te' }))).toBeInTheDocument()
    expect(screen.getByText('మేము ఆకులపై సంఖ్యలు రాసి పోల్చాము.')).toHaveAttribute('lang', 'te')
  })

  it('a reopened link starts at the first step; the answer page is locked once both tries are used (T-116)', async () => {
    setup(evening(todays({ picture: { a: 48, b: 33, firstAnswer: '>', secondAnswer: '<', locked: true } })))
    const { unmount } = renderApp(`/p/${token}`)

    expect(await screen.findByRole('heading', { name: 'What Manjunath learned today' })).toBeInTheDocument()
    unmount()

    renderApp(`/p/${token}?step=picture`)
    expect(await screen.findByText('Your answer')).toBeInTheDocument()
    expect(screen.queryByRole('group')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Try again' })).not.toBeInTheDocument()
  })

  it('without today’s message the plain evening message is unchanged (D-21)', async () => {
    setup(evening(null))
    renderApp(`/p/${token}`)

    expect(await screen.findByText(/Manjunath was away today/)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Next' })).not.toBeInTheDocument()
  })
})
