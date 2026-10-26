import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n, LANGUAGE_STORAGE_KEY } from '../../i18n'
import { fakeApi, renderApp, type FakeRoute } from '../../test/renderApp'
import { listenLabelKey } from './listenLabel'
import { sticksText } from './sticksText'
import type { ParentCatchUp } from './types'

const i18n = initI18n()
const token = 'AbCdEfGhIjKlMnOpQrStUv'
const pageUrl = `GET /api/p/${token}`

function page(overrides: Partial<ParentCatchUp> = {}): ParentCatchUp {
  return {
    kind: 'catchUp',
    language: 'en',
    childName: 'Ravi',
    teacherName: 'Shobha',
    stopped: false,
    answered: false,
    expiresAt: '2026-11-06T13:30:00Z',
    homework: { book: 'textbook', part: 2, page: 15 },
    audio: {
      segments: [
        { kind: 'disclosure', url: `/api/p/${token}/audio/d`, seconds: 4 },
        { kind: 'script', url: `/api/p/${token}/audio/s`, seconds: 50 },
      ],
      totalSeconds: 54,
    },
    transcript: { disclosureText: 'This is a lesson from your child’s teacher.', script: 'Today we take away…', keyWordsKn: ['ಕಳೆ'] },
    activity: { question: '43 − 18', number: 43, instruction: 'Open one bundle. Now take away 18.', objectOption: 'No sticks? Use bottle caps.' },
    check: {
      question: '52 − 27',
      prompt: null,
      options: [
        { id: 0, number: 35 },
        { id: 1, number: 25 },
        { id: 2, number: 15 },
      ],
    },
    ...overrides,
  }
}

function setup(current: () => unknown, extra: Record<string, FakeRoute> = {}, status = () => 200) {
  return fakeApi({ [pageUrl]: () => ({ status: status(), body: current() }), ...extra })
}

describe('P3 / P1 rules', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en')
  })

  it.each([
    ['idle', 'Listen'],
    ['loading', 'Loading…'],
    ['playing', 'Pause'],
    ['paused', 'Resume'],
    ['ended', 'Listen again'],
  ] as const)('Listen button reads %s → %s', (state, label) => {
    expect(i18n.t(listenLabelKey(state))).toBe(label)
  })

  it.each([
    [347, '3 bundles of a hundred, 4 bundles of ten, 7 loose sticks'],
    [43, '4 bundles of ten, 3 loose sticks'],
    [110, '1 bundle of a hundred, 1 bundle of ten'],
    [1, '1 loose stick'],
    [0, 'No sticks'],
  ])('sticks for %d read "%s"', (n, text) => {
    expect(sticksText(i18n.t, n)).toBe(text)
  })
})

describe('ParentScreen', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
    localStorage.clear()
    vi.spyOn(HTMLMediaElement.prototype, 'play').mockResolvedValue()
    vi.spyOn(HTMLMediaElement.prototype, 'pause').mockReturnValue()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('opens in the family’s language without changing the language saved on the phone', async () => {
    setup(() => page({ language: 'te' }))
    renderApp(`/p/${token}`)

    expect(await screen.findByRole('heading', { level: 1, name: 'Ravi కోసం పాఠం' })).toHaveFocus()
    expect(document.documentElement.lang).toBe('te')
    expect(screen.getByText('AI సహాయంతో · Shobha టీచర్ ఆమోదించారు')).toBeInTheDocument()
    expect(localStorage.getItem(LANGUAGE_STORAGE_KEY)).toBeNull()
    expect(i18n.language).toBe('en')
  })

  it('P1: Listen shows feedback at once, the parts are labelled, and Next works without playing', async () => {
    setup(() => page())
    renderApp(`/p/${token}`)

    expect(await screen.findByText('Step 1 of 4')).toBeInTheDocument()
    expect(screen.getByRole('list', { name: 'Parts of the lesson' })).toHaveTextContent('Computer voice· Lesson')
    expect(screen.getByText('0:54')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Listen' }))
    expect(screen.getByRole('button', { name: 'Loading…' })).toBeInTheDocument()
    expect(HTMLMediaElement.prototype.play).toHaveBeenCalled()

    await userEvent.click(screen.getByText('Read the words'))
    expect(screen.getByText('Today we take away…')).toHaveAttribute('lang', 'en')
    expect(screen.getByText('ಕಳೆ')).toHaveAttribute('lang', 'kn')

    await userEvent.click(screen.getByRole('button', { name: 'Next' }))
    expect(await screen.findByRole('heading', { level: 1, name: 'Activity at home' })).toHaveFocus()
  })

  it('P2: the sticks picture has its words, then the homework page', async () => {
    setup(() => page())
    renderApp(`/p/${token}?step=2`)

    expect(await screen.findByText('4 bundles of ten, 3 loose sticks')).toBeInTheDocument()
    expect(screen.getByText('Open one bundle. Now take away 18.')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Homework: page 15' })).toBeInTheDocument()
    expect(screen.getByText('Textbook Part 2 · show Shobha teacher when you come back.')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Next: question' }))
    expect(await screen.findByRole('heading', { level: 1, name: 'Question' })).toBeInTheDocument()
  })

  it('P3: no numeral before the tap; the chosen one is sent and the family sees Well done', async () => {
    let current = page()
    const calls = setup(() => current, {
      [`POST /api/p/${token}/answer`]: () => {
        current = { ...current, answered: true }
        return { status: 200, body: { answered: true } }
      },
    })
    renderApp(`/p/${token}?step=3`)

    const answers = await screen.findByRole('list', { name: 'Answers' })
    expect(screen.getByText('52 − 27 = ?')).toBeInTheDocument()
    expect(screen.getByText('Ravi, tap the right picture')).toBeInTheDocument()
    expect(within(answers).queryByText('35')).toBeNull()
    expect(within(answers).queryByText('25')).toBeNull()
    expect(screen.getByText('Tap one picture')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: '3 bundles of ten, 5 loose sticks' }))
    await userEvent.click(screen.getByRole('button', { name: '2 bundles of ten, 5 loose sticks' }))
    const chosen = screen.getByRole('button', { name: '2 bundles of ten, 5 loose sticks, 25' })
    expect(chosen).toHaveAttribute('aria-pressed', 'true')
    expect(within(answers).getByText('25')).toBeInTheDocument()
    expect(within(answers).queryByText('35')).toBeNull()

    await userEvent.click(screen.getByRole('button', { name: 'This one, send' }))

    expect(await screen.findByText('Well done, Ravi')).toBeInTheDocument()
    expect(screen.getByText('Shobha teacher will see this.')).toBeInTheDocument()
    expect(screen.getByText('Bring page 15 to school.')).toBeInTheDocument()
    // One star line (D-81) with the child's name from the page's own text and no number.
    const star = screen.getByText('Ravi earned a star for doing this catch-up.')
    expect(star.textContent).not.toMatch(/\d/)
    const answer = calls.find((c) => c.key === `POST /api/p/${token}/answer`)
    expect(answer?.body).toMatchObject({ choiceId: 1 })
    expect(screen.queryByText(/correct|wrong|right answer/i)).toBeNull()
  })

  it('P3: a failed send keeps the choice and Try again reuses the same request ID', async () => {
    let online = false
    const calls = setup(() => page(), {
      [`POST /api/p/${token}/answer`]: () => (online ? { status: 200, body: { answered: true } } : 'offline'),
    })
    renderApp(`/p/${token}?step=3`)

    await userEvent.click(await screen.findByRole('button', { name: '1 bundle of ten, 5 loose sticks' }))
    await userEvent.click(screen.getByRole('button', { name: 'This one, send' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not send. Your choice is still here.')
    expect(screen.getByRole('button', { name: '1 bundle of ten, 5 loose sticks, 15' })).toHaveAttribute('aria-pressed', 'true')
    online = true
    await userEvent.click(screen.getByRole('button', { name: 'Try again' }))

    expect(await screen.findByText('Well done, Ravi')).toBeInTheDocument()
    const sends = calls.filter((c) => c.key === `POST /api/p/${token}/answer`)
    const ids = sends.map((send) => (send.body as { clientRequestId: string }).clientRequestId)
    expect(ids).toHaveLength(2)
    expect(new Set(ids).size).toBe(1)
  })

  it('reopened after answering: the question goes straight to Done (D-62)', async () => {
    setup(() => page({ answered: true }))
    renderApp(`/p/${token}?step=3`)

    expect(await screen.findByText('Well done, Ravi')).toBeInTheDocument()
    expect(screen.queryByRole('list', { name: 'Answers' })).toBeNull()
  })

  it('stop: one confirmation, then Messages stopped; Start them again brings the lesson back', async () => {
    let current = page()
    const calls = setup(() => current, {
      [`POST /api/p/${token}/stop`]: () => {
        current = { ...page(), stopped: true, audio: null, transcript: null, activity: null, check: null }
        return { status: 200, body: { stopped: true } }
      },
      [`POST /api/p/${token}/restart`]: () => {
        current = page()
        return { status: 200, body: { stopped: false } }
      },
    })
    renderApp(`/p/${token}`)

    await userEvent.click(await screen.findByRole('button', { name: 'Stop these messages' }))
    const dialog = screen.getByRole('dialog', { name: 'Stop messages for Ravi?' })
    expect(within(dialog).getByRole('button', { name: 'No, keep them coming' })).toHaveFocus()
    expect(dialog).toHaveTextContent('You can start them again any time.')

    await userEvent.click(within(dialog).getByRole('button', { name: 'Stop messages' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Messages stopped' })).toBeInTheDocument()
    expect(screen.getByText('When Ravi comes back, the teacher will help as usual.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Stop these messages' })).toBeNull()
    expect(screen.queryByText('Step 1 of 4')).toBeNull()

    await userEvent.click(screen.getByRole('button', { name: 'Start them again' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Ravi’s lesson' })).toBeInTheDocument()
    expect(calls.map((c) => c.key)).toEqual(
      expect.arrayContaining([`POST /api/p/${token}/stop`, `POST /api/p/${token}/restart`]),
    )
  })

  it('stop dialog closes with “No, keep them coming” and nothing is sent', async () => {
    const calls = setup(() => page())
    renderApp(`/p/${token}`)

    await userEvent.click(await screen.findByRole('button', { name: 'Stop these messages' }))
    await userEvent.click(screen.getByRole('button', { name: 'No, keep them coming' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull())
    expect(calls.some((c) => c.key.endsWith('/stop'))).toBe(false)
  })

  it('an old link says so, still shows the homework and keeps the stop link', async () => {
    setup(
      () => ({ kind: 'catchUp', language: 'en', childName: 'Ravi', teacherName: 'Shobha', homework: { book: 'textbook', part: 2, page: 15 } }),
      {},
      () => 410,
    )
    renderApp(`/p/${token}`)

    expect(await screen.findByRole('heading', { level: 1, name: 'This link is old' })).toBeInTheDocument()
    expect(screen.getByText('Shobha teacher will send a new one. You don’t need to do anything.')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Homework: page 15' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Stop these messages' })).toBeInTheDocument()
  })

  it('an unknown link says it doesn’t work', async () => {
    fakeApi({})
    renderApp(`/p/${token}`)

    expect(await screen.findByText('This link doesn’t work. Ask the teacher to send it again.')).toBeInTheDocument()
  })

  it('no network on open: says nothing was lost and offers Try again', async () => {
    let online = false
    fakeApi({ [pageUrl]: () => (online ? { status: 200, body: page() } : 'offline') })
    renderApp(`/p/${token}`)

    expect(await screen.findByText(/No network\. Nothing was lost\./)).toBeInTheDocument()
    online = true
    await userEvent.click(screen.getByRole('button', { name: 'Try again' }))
    expect(await screen.findByRole('heading', { level: 1, name: 'Ravi’s lesson' })).toBeInTheDocument()
  })
})
