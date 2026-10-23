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
    'GET /api/curriculum': () => ({ status: 200, body: curriculum }),
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
    expect(screen.getByText('AI-assisted')).toBeInTheDocument()
    expect(screen.getByRole('list', { name: 'Parts of the catch-up' })).toHaveTextContent('Computer voice· Lesson · 1:24')
    expect(screen.getByText('ದಶಕ ಸಹಿತ ವ್ಯವಕಲನ. ನಿಮ್ಮ ಟೀಚರ್‌ಗೆ ತೋರಿಸು')).toHaveAttribute('lang', 'kn')
    expect(screen.getByText(/Word problems next week need borrowing\./)).toBeInTheDocument()
    expect(screen.getByText(/Checking can wait\./)).toBeInTheDocument()
    const options = screen.getByRole('list', { name: 'The three pictures' })
    expect(within(options).getByText(/263/)).toHaveTextContent('263 ✓correct answer')
  })

  it('Approve needs the tick and sends the version she read', async () => {
    let current = pkg()
    const calls = setup(() => current, {
      'POST /api/packages/pkg-te/approve': () => {
        current = pkg({ status: 'approved' })
        return { status: 200, body: current }
      },
    })
    renderApp('/approve/pkg-te')

    const approve = await screen.findByRole('button', { name: 'Approve' })
    expect(approve).toBeDisabled()
    expect(screen.getByText('Tick the summary box to approve.')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('checkbox', { name: 'I’ve checked the Kannada summary' }))
    await userEvent.click(approve)

    expect(await screen.findByText('Approved. Goes to the families after 6 pm.')).toBeInTheDocument()
    expect(calls.find((c) => c.key === 'POST /api/packages/pkg-te/approve')?.body).toMatchObject({ scriptVersion: 2 })
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
    expect(screen.queryByRole('button', { name: 'Approve' })).not.toBeInTheDocument()
    expect(calls.find((c) => c.key === 'POST /api/packages/pkg-te/regenerate')?.body).toMatchObject({ hint: 'simpler' })
  })

  it('a new version arriving clears the tick', async () => {
    let current = pkg()
    setup(() => current)
    const { unmount } = renderApp('/approve/pkg-te')
    await userEvent.click(await screen.findByRole('checkbox', { name: 'I’ve checked the Kannada summary' }))
    expect(screen.getByRole('button', { name: 'Approve' })).toBeEnabled()
    unmount()

    current = pkg({ scriptVersion: 3 })
    renderApp('/approve/pkg-te')
    expect(await screen.findByRole('checkbox', { name: 'I’ve checked the Kannada summary' })).not.toBeChecked()
    expect(screen.getByRole('button', { name: 'Approve' })).toBeDisabled()
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
    expect(screen.queryByRole('button', { name: 'Approve' })).not.toBeInTheDocument()
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
    await userEvent.click(screen.getByRole('button', { name: 'Approve' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('This changed on another device. Showing the latest.')
  })

  it('sent: read only, with the time and how many families', async () => {
    setup(() => pkg({ status: 'sent', sentAt: '2026-10-23T12:32:00Z', familiesSent: 1 }))
    renderApp('/approve/pkg-te')

    expect(await screen.findByText(/^Sent at 6:02\s?PM · 1 families$/i)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Approve|Simpler|Shorter/ })).not.toBeInTheDocument()
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
