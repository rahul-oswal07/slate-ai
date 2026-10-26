import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { fakeApi, renderApp } from '../../test/renderApp'
import type { ParentEvening } from './types'

const i18n = initI18n()
const token = 'ZyXwVuTsRqPoNmLkJiHgFe'

function evening(overrides: Partial<ParentEvening> = {}): ParentEvening {
  return {
    kind: 'evening',
    language: 'en',
    childName: 'Ravi',
    teacherName: 'Shobha',
    text: 'We missed Ravi today.',
    greeting: { url: `/api/p/${token}/greeting`, seconds: 8.4 },
    lessonAudio: null,
    stopped: false,
    reason: null,
    ...overrides,
  }
}

describe('Evening message page: the teacher’s greeting', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('shows her voice first, with her name and length, and plays the token URL', async () => {
    const urls: string[] = []
    vi.stubGlobal(
      'Audio',
      class {
        onended: (() => void) | null = null
        constructor(url: string) {
          urls.push(url)
        }
        play = () => Promise.resolve()
        pause = () => {}
      },
    )
    fakeApi({ [`GET /api/p/${token}`]: () => ({ status: 200, body: evening() }) })
    renderApp(`/p/${token}`)

    expect(await screen.findByText('Shobha teacher’s voice · 0:08')).toBeInTheDocument()
    const play = screen.getByRole('button', { name: 'Play the greeting' })
    await userEvent.click(play)

    expect(urls).toEqual([`/api/p/${token}/greeting`])
    expect(play).toHaveAttribute('aria-pressed', 'true')
    await userEvent.click(play)
    expect(play).toHaveAttribute('aria-pressed', 'false')
  })

  it('has no voice row when she has not recorded one', async () => {
    fakeApi({ [`GET /api/p/${token}`]: () => ({ status: 200, body: evening({ greeting: null }) }) })
    renderApp(`/p/${token}`)

    expect(await screen.findByText('We missed Ravi today.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Play the greeting' })).not.toBeInTheDocument()
  })

  it('plays the approved lesson audio after the AI disclosure, both from the token, with the AI label and the words', async () => {
    const urls: string[] = []
    vi.stubGlobal(
      'Audio',
      class {
        onended: (() => void) | null = null
        constructor(url: string) {
          urls.push(url)
        }
        play = () => {
          setTimeout(() => this.onended?.(), 0)
          return Promise.resolve()
        }
        pause = () => {}
      },
    )
    const lessonAudio = {
      segments: [
        { kind: 'disclosure' as const, url: `/api/p/${token}/lesson-audio/d1`, seconds: 5 },
        { kind: 'script' as const, url: `/api/p/${token}/lesson-audio/l1`, seconds: 23 },
      ],
      totalSeconds: 28,
      disclosureText: 'This is a lesson from your child’s teacher. A computer voice reads it.',
      transcript: 'Today we learned face value.',
    }
    fakeApi({ [`GET /api/p/${token}`]: () => ({ status: 200, body: evening({ lessonAudio }) }) })
    renderApp(`/p/${token}`)

    expect(await screen.findByRole('heading', { name: 'Today’s lesson · 0:28' })).toBeInTheDocument()
    expect(screen.getByText('AI-assisted, approved by Shobha teacher')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Play today’s lesson' }))

    await waitFor(() => expect(urls).toEqual([`/api/p/${token}/lesson-audio/d1`, `/api/p/${token}/lesson-audio/l1`]))
    await userEvent.click(screen.getByText('Read the words'))
    expect(screen.getByText('Today we learned face value.')).toBeVisible()
    expect(screen.getByText(/A computer voice reads it/)).toBeVisible()
  })

  it('has no lesson player when she did not send audio', async () => {
    fakeApi({ [`GET /api/p/${token}`]: () => ({ status: 200, body: evening() }) })
    renderApp(`/p/${token}`)

    expect(await screen.findByText('We missed Ravi today.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Play today’s lesson' })).not.toBeInTheDocument()
  })
})
