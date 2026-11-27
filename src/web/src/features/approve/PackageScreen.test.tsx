import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { fakeApi, renderApp, teacher, type FakeRoute } from '../../test/renderApp'
import { curriculum, pkg, week } from './fixtures'
import type { CatchUpPackage } from './types'

const i18n = initI18n()
const packagesUrl = `GET /api/classes/${teacher.classId}/packages`

function setup(current: () => CatchUpPackage, extra: Record<string, FakeRoute> = {}) {
  return fakeApi({
    'GET /api/me': () => ({ status: 200, body: teacher }),
    [`GET /api/classes/${teacher.classId}/curriculum`]: () => ({ status: 200, body: curriculum }),
    [packagesUrl]: () => ({ status: 200, body: week(current()) }),
    ...extra,
  })
}

describe('PackageScreen', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
    vi.spyOn(HTMLMediaElement.prototype, 'play').mockResolvedValue()
    vi.spyOn(HTMLMediaElement.prototype, 'pause').mockReturnValue()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('shows everything she needs to decide: children, player, summary, why this, picture question', async () => {
    setup(() => pkg())
    renderApp('/approve/pkg-te')

    expect(await screen.findByRole('heading', { level: 1, name: 'Review catch-up' })).toHaveFocus()
    expect(await screen.findByText('Ravi, Mahesh')).toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 2, name: 'In Telugu · 2 children' })).toBeInTheDocument()
    expect(screen.getByText('AI-assisted')).toBeInTheDocument()
    expect(screen.getByRole('list', { name: 'Parts of the catch-up' })).toHaveTextContent('Computer voice· Lesson · 1:24')
    expect(screen.getByText('ದಶಕ ಸಹಿತ ವ್ಯವಕಲನ. ನಿಮ್ಮ ಟೀಚರ್‌ಗೆ ತೋರಿಸು')).toHaveAttribute('lang', 'kn')
    expect(screen.getByText(/Word problems next week need borrowing\./)).toBeInTheDocument()
    expect(screen.getByText(/Making sums can wait\./)).toBeInTheDocument()
    // The same sections as today's message, one open at a time (T-103): the concept first, the picture question on a tap.
    expect(screen.getByText('ఈ రోజు ...')).toHaveAttribute('lang', 'te')
    expect(screen.queryByRole('list', { name: 'The three pictures' })).not.toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Picture question' }))
    const options = screen.getByRole('list', { name: 'The three pictures' })
    expect(within(options).getByText(/263/)).toHaveTextContent('263 ✓correct answer')
    expect(screen.getByText('The numbers and the answer are worked out by the app.')).toBeInTheDocument()
    expect(screen.queryByText('ఈ రోజు ...')).not.toBeInTheDocument()
  })

  it('has the same sections as today’s message: no video without a link, her note to add (T-103)', async () => {
    setup(() => pkg())
    renderApp('/approve/pkg-te')

    const sections = await screen.findAllByRole('heading', { level: 3 })
    expect(sections.map((h) => h.textContent)).toEqual(['Concept', 'Activity at home', 'Picture question', 'My note'])
    await userEvent.click(screen.getByRole('button', { name: 'Activity at home' }))
    expect(screen.getByText('Make 523 with sticks.')).toBeInTheDocument()
    expect(screen.getByText('Numbers chosen by the app: 523 − 168')).toBeInTheDocument()
  })

  it('her note: saved as a new version, which clears her tick (T-103, guardrail 3)', async () => {
    let current = pkg()
    const calls = setup(() => current, {
      'PUT /api/packages/pkg-te/sections/note': () => {
        current = pkg({ scriptVersion: 3, note: { text: 'We wrote numbers on leaves.', removed: false } })
        return { status: 200, body: current }
      },
    })
    renderApp('/approve/pkg-te')

    await userEvent.click(await screen.findByRole('checkbox', { name: 'I’ve checked the Kannada summary' }))
    await userEvent.click(screen.getByRole('button', { name: 'My note' }))
    await userEvent.click(screen.getByRole('button', { name: 'Add my note' }))
    await userEvent.type(screen.getByLabelText('Your note to the family (they read it as you write it)'), 'We wrote numbers on leaves.')
    await userEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('We wrote numbers on leaves.')).toBeInTheDocument()
    expect(calls.find((c) => c.key === 'PUT /api/packages/pkg-te/sections/note')?.body).toMatchObject({
      scriptVersion: 2,
      text: 'We wrote numbers on leaves.',
    })
    expect(screen.getByText('Version 3')).toBeInTheDocument()
    expect(screen.getByRole('checkbox', { name: 'I’ve checked the Kannada summary' })).not.toBeChecked()
    expect(screen.getByRole('button', { name: 'Approve and send now' })).toBeDisabled()
  })

  it('her script: the voice is made again from her words, and she checks it again', async () => {
    let current = pkg()
    const calls = setup(() => current, {
      'PUT /api/packages/pkg-te/sections/concept': () => {
        current = pkg({ status: 'generating', scriptVersion: 3, script: 'మీ బిడ్డతో 523 పుల్లలు.', audio: null, glossKn: null, edited: true })
        return { status: 202, body: current }
      },
    })
    renderApp('/approve/pkg-te')

    await userEvent.click(await screen.findByRole('button', { name: 'Edit' }))
    const box = screen.getByLabelText('The words families hear (language: Telugu)')
    expect(box).toHaveValue('ఈ రోజు ...')
    expect(box).toHaveAttribute('lang', 'te')
    await userEvent.clear(box)
    await userEvent.type(box, 'మీ బిడ్డతో 523 పుల్లలు.')
    await userEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('Making the voice for your words… Check it again when it’s ready.')).toBeInTheDocument()
    expect(screen.getByText(/Version 3 · your words/)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Approve and send now' })).not.toBeInTheDocument()
    expect(calls.find((c) => c.key === 'PUT /api/packages/pkg-te/sections/concept')?.body).toMatchObject({
      scriptVersion: 2,
      text: 'మీ బిడ్డతో 523 పుల్లలు.',
    })
  })

  it('her activity: both lines go, and a refused edit says why and keeps her words', async () => {
    const calls = setup(() => pkg(), {
      'PUT /api/packages/pkg-te/sections/activity': () => ({
        status: 400,
        body: { errors: { problem: ['numbers'], detail: ['263'], allowed: ['5', '168', '523'] } },
      }),
    })
    renderApp('/approve/pkg-te')

    await userEvent.click(await screen.findByRole('button', { name: 'Activity at home' }))
    await userEvent.click(screen.getByRole('button', { name: 'Edit' }))
    const what = screen.getByLabelText('What the family does (language: Telugu)')
    await userEvent.clear(what)
    await userEvent.type(what, '263 పుల్లలు.')
    await userEvent.type(screen.getByLabelText('Other things they can use (can be left empty)'), 'రాళ్లు.')
    await userEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Use only this catch-up’s numbers. You can use: 168, 523.')
    expect(screen.getByLabelText('What the family does (language: Telugu)')).toHaveValue('263 పుల్లలు.')
    expect(calls.find((c) => c.key === 'PUT /api/packages/pkg-te/sections/activity')?.body).toMatchObject({
      scriptVersion: 2,
      text: '263 పుల్లలు.',
      objectOption: 'రాళ్లు.',
    })
  })

  it('the video can be removed (not sent) and put back; the lesson sections cannot', async () => {
    let current = pkg({ video: { url: 'https://diksha.gov.in/play/content/do_1', removed: false } })
    const calls = setup(() => current, {
      'PATCH /api/packages/pkg-te/sections/video': () => {
        current = pkg({ scriptVersion: 3, video: { url: 'https://diksha.gov.in/play/content/do_1', removed: true }, removed: ['video'] })
        return { status: 200, body: current }
      },
    })
    renderApp('/approve/pkg-te')

    expect(await screen.findByRole('button', { name: 'Concept' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Remove' })).not.toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Video' }))
    expect(screen.getByRole('link', { name: 'Open the video' })).toHaveAttribute('href', 'https://diksha.gov.in/play/content/do_1')
    await userEvent.click(screen.getByRole('button', { name: 'Remove' }))

    expect(await screen.findByText('Removed: not sent')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Put back' })).toBeInTheDocument()
    expect(calls.find((c) => c.key === 'PATCH /api/packages/pkg-te/sections/video')?.body).toMatchObject({ removed: true })
  })

  it('approved or sent: the sections are read only', async () => {
    setup(() => pkg({ status: 'approved', approvedAt: '2026-10-23T09:00:00Z', note: { text: 'Well done.', removed: false } }))
    renderApp('/approve/pkg-te')

    await userEvent.click(await screen.findByRole('button', { name: 'My note' }))
    expect(screen.getByText('Well done.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Edit|Remove|Add my note/ })).not.toBeInTheDocument()
  })

  it("shows A2's Kannada reason in Kannada, and the English one where A2 gave no Kannada (D-76)", async () => {
    await i18n.changeLanguage('kn')
    setup(() => pkg())
    renderApp('/approve/pkg-te')

    expect(await screen.findByText('ಮುಂದಿನ ವಾರದ ಪದ ಸಮಸ್ಯೆಗಳಿಗೆ ಎರವಲು ಬೇಕು.')).toHaveAttribute('lang', 'kn')
    expect(screen.queryByText(/Word problems next week need borrowing\./)).not.toBeInTheDocument()
    expect(screen.getByText(/Making sums can wait\./)).toBeInTheDocument()
  })

  it('both Approve buttons need the tick; send after 6 pm sends the version she read', async () => {
    let current = pkg()
    const calls = setup(() => current, {
      'POST /api/packages/pkg-te/approve': () => {
        current = pkg({ status: 'approved' })
        return { status: 200, body: current }
      },
    })
    renderApp('/approve/pkg-te')

    const later = await screen.findByRole('button', { name: 'Approve, send after 6 pm' })
    expect(later).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Approve and send now' })).toBeDisabled()
    expect(screen.getByText('Tick the summary box first.')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('checkbox', { name: 'I’ve checked the Kannada summary' }))
    await userEvent.click(later)

    expect(await screen.findByText('Approved. Goes to the families after 6 pm.')).toBeInTheDocument()
    expect(calls.find((c) => c.key === 'POST /api/packages/pkg-te/approve')?.body).toMatchObject({ scriptVersion: 2, send: false })
  })

  it('Approve and send now sends at once and says so (T-117)', async () => {
    let current = pkg()
    const calls = setup(() => current, {
      'POST /api/packages/pkg-te/approve': () => {
        current = pkg({ status: 'sent', sentAt: '2026-10-23T09:32:00Z', familiesSent: 1 })
        return { status: 200, body: current }
      },
    })
    renderApp('/approve/pkg-te')

    await userEvent.click(await screen.findByRole('checkbox', { name: 'I’ve checked the Kannada summary' }))
    await userEvent.click(screen.getByRole('button', { name: 'Approve and send now' }))

    expect(await screen.findByText("Approved and sent to the family's phone.")).toBeInTheDocument()
    expect(screen.queryByText('Approved. Goes to the families after 6 pm.')).not.toBeInTheDocument()
    expect(calls.find((c) => c.key === 'POST /api/packages/pkg-te/approve')?.body).toMatchObject({ scriptVersion: 2, send: true })
  })

  it('Simpler clears the tick and shows that a new version is being written', async () => {
    let current = pkg()
    const calls = setup(() => current, {
      'POST /api/packages/pkg-te/regenerate': () => {
        current = pkg({ status: 'generating' })
        return { status: 202, body: current }
      },
    })
    renderApp('/approve/pkg-te')

    await userEvent.click(await screen.findByRole('checkbox', { name: 'I’ve checked the Kannada summary' }))
    await userEvent.click(screen.getByRole('button', { name: 'Simpler' }))

    expect(await screen.findByText('Writing a new version… Check it again when it’s ready.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Approve and send now' })).not.toBeInTheDocument()
    expect(calls.find((c) => c.key === 'POST /api/packages/pkg-te/regenerate')?.body).toMatchObject({ hint: 'simpler' })
  })

  it('a new version arriving clears the tick', async () => {
    let current = pkg()
    setup(() => current)
    const { unmount } = renderApp('/approve/pkg-te')
    await userEvent.click(await screen.findByRole('checkbox', { name: 'I’ve checked the Kannada summary' }))
    expect(screen.getByRole('button', { name: 'Approve and send now' })).toBeEnabled()
    unmount()

    current = pkg({ scriptVersion: 3 })
    renderApp('/approve/pkg-te')
    expect(await screen.findByRole('checkbox', { name: 'I’ve checked the Kannada summary' })).not.toBeChecked()
    expect(screen.getByRole('button', { name: 'Approve and send now' })).toBeDisabled()
  })

  it('needs review: says why in numbers, offers Make it shorter, and cannot be approved', async () => {
    setup(() =>
      pkg({
        status: 'needsReview',
        review: { passed: false, attempts: 2, checks: [{ name: 'audio', passed: false, detail: 'audio is 104 seconds, limit is 95' }] },
      }),
    )
    renderApp('/approve/pkg-te')

    expect(await screen.findByText('audio is 104 seconds, limit is 95')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Make it shorter' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Approve and send now' })).not.toBeInTheDocument()
    expect(screen.queryByRole('checkbox')).not.toBeInTheDocument()
  })

  it('failed: alert, no AI label, and Try again asks for a new version', async () => {
    let current = pkg({ status: 'failed', audio: null })
    const calls = setup(() => current, {
      'POST /api/packages/pkg-te/regenerate': () => {
        current = pkg({ status: 'generating' })
        return { status: 202, body: current }
      },
    })
    renderApp('/approve/pkg-te')

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not write this catch-up. Nothing was sent.')
    expect(screen.queryByText('AI-assisted')).not.toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Try again' }))
    expect(calls.find((c) => c.key === 'POST /api/packages/pkg-te/regenerate')?.body).toMatchObject({ hint: null })
  })

  it('a 409 shows the latest and says it changed', async () => {
    let current = pkg()
    setup(() => current, {
      'POST /api/packages/pkg-te/approve': () => {
        current = pkg({ scriptVersion: 3 })
        return { status: 409, body: current }
      },
    })
    renderApp('/approve/pkg-te')

    await userEvent.click(await screen.findByRole('checkbox', { name: 'I’ve checked the Kannada summary' }))
    await userEvent.click(screen.getByRole('button', { name: 'Approve and send now' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('This changed on another device. Showing the latest.')
  })

  it('sent: read only, with the time and how many families', async () => {
    setup(() => pkg({ status: 'sent', sentAt: '2026-10-23T12:32:00Z', familiesSent: 2 }))
    renderApp('/approve/pkg-te')

    expect(await screen.findByText(/^Sent at 6:02\s?PM · 2 families$/i)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Approve|Simpler|Shorter/ })).not.toBeInTheDocument()
  })

  it('one child, one family: "In Hindi · 1 child" and "1 family", not "children" or "families" (T-100)', async () => {
    const rohan = pkg().children.slice(0, 1)
    setup(() => pkg({ language: 'hi', status: 'sent', sentAt: '2026-10-23T12:32:00Z', familiesSent: 1, children: rohan }))
    renderApp('/approve/pkg-te')

    expect(await screen.findByRole('heading', { level: 2, name: 'In Hindi · 1 child' })).toBeInTheDocument()
    expect(screen.getByText(/^Sent at 6:02\s?PM · 1 family$/i)).toBeInTheDocument()
  })

  it('the player plays the computer voice, then the lesson', async () => {
    const created: string[] = []
    const Real = window.Audio
    vi.stubGlobal('Audio', function (this: HTMLAudioElement, src: string) {
      created.push(src)
      return new Real(src)
    })
    setup(() => pkg())
    renderApp('/approve/pkg-te')

    await userEvent.click(await screen.findByRole('button', { name: 'Play' }))

    expect(created).toEqual(['/api/audio/d1'])
    expect(screen.getByRole('button', { name: 'Pause' })).toBeInTheDocument()
  })
})
