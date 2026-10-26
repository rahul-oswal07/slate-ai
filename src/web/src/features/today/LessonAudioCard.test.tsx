import { QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { createQueryClient } from '../../app/queryClient'
import { initI18n } from '../../i18n'
import { fakeApi, teacher } from '../../test/renderApp'
import type { EveningDay, LessonAudioDay, LessonAudioItem } from '../evening/types'
import { LessonAudioCard } from './LessonAudioCard'

const i18n = initI18n()
const date = '2026-10-20'
const url = `/api/classes/${teacher.classId}/lesson-audio/${date}`

const evening: EveningDay = {
  date,
  blocked: null,
  templateApproved: true,
  lessonConfirmed: true,
  absent: 2,
  families: 2,
  sent: 0,
  sendsAt: '18:00:00',
  children: [],
}

function item(overrides: Partial<LessonAudioItem> = {}): LessonAudioItem {
  return {
    language: 'te',
    status: 'ready',
    children: 1,
    clipId: 'clip-1',
    segments: [
      { kind: 'disclosure', url: '/api/audio/d1', seconds: 5 },
      { kind: 'script', url: '/api/audio/clip-1', seconds: 24 },
    ],
    seconds: 29,
    transcript: 'ఈ రోజు మేము ముఖ విలువ నేర్చుకున్నాం.',
    approvedAt: null,
    ...overrides,
  }
}

function day(overrides: Partial<LessonAudioDay> = {}): LessonAudioDay {
  return { date, available: true, enabled: true, sent: false, items: [item()], ...overrides }
}

function renderCard(eveningDay: EveningDay = evening) {
  return render(
    <QueryClientProvider client={createQueryClient()}>
      <LessonAudioCard classId={teacher.classId} date={date} evening={eveningDay} />
    </QueryClientProvider>,
  )
}

const posts = (calls: { key: string; body: unknown }[], suffix: string) =>
  calls.filter((c) => c.key === `POST ${url}/${suffix}`).map((c) => c.body)

describe('LessonAudioCard (T-62)', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
    // Playing a part finishes at once, so a click on Play listens to the whole clip.
    vi.stubGlobal(
      'Audio',
      class {
        onended: (() => void) | null = null
        currentTime = 0
        ended = false
        play = () => {
          setTimeout(() => this.onended?.(), 0)
          return Promise.resolve()
        }
        pause = () => {}
      },
    )
  })

  afterEach(() => vi.unstubAllGlobals())

  it('is not offered before the lesson is confirmed or when no family gets the message', async () => {
    const calls = fakeApi({ [`GET ${url}`]: () => ({ status: 200, body: day() }) })

    renderCard({ ...evening, lessonConfirmed: false })
    renderCard({ ...evening, families: 0 })
    renderCard({ ...evening, templateApproved: false })

    await new Promise((resolve) => setTimeout(resolve, 50))
    expect(screen.queryByRole('checkbox')).not.toBeInTheDocument()
    expect(calls).toHaveLength(0)
  })

  it('is unticked by default, and ticking asks for the clips and shows them being made', async () => {
    let ticked = false
    const calls = fakeApi({
      [`GET ${url}`]: () => ({ status: 200, body: ticked ? day({ items: [item({ status: 'generating', clipId: null, segments: [] })] }) : day({ enabled: false, items: [] }) }),
      [`PUT ${url}`]: () => {
        ticked = true
        return { status: 202, body: day({ items: [item({ status: 'generating', clipId: null, segments: [] })] }) }
      },
    })
    renderCard()

    const box = await screen.findByRole('checkbox', { name: 'Also send today’s lesson as audio' })
    expect(box).not.toBeChecked()
    expect(screen.queryByRole('listitem')).not.toBeInTheDocument()

    await userEvent.click(box)

    expect(await screen.findByText('Making the audio…')).toBeInTheDocument()
    expect(screen.getByText('In Telugu')).toBeInTheDocument()
    expect(screen.getByText('Families: 1')).toBeInTheDocument()
    expect(calls.find((c) => c.key === `PUT ${url}`)?.body).toMatchObject({ enabled: true })
  })

  it('will not approve a clip she has not listened to, and says why instead of doing nothing', async () => {
    const calls = fakeApi({ [`GET ${url}`]: () => ({ status: 200, body: day() }) })
    renderCard()

    await userEvent.click(await screen.findByRole('button', { name: 'Approve this clip' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Listen to the whole clip first, then approve.')
    expect(posts(calls, 'te/approve')).toHaveLength(0)
  })

  it('approves exactly the clip she listened to all the way through', async () => {
    let approved = false
    const calls = fakeApi({
      [`GET ${url}`]: () => ({ status: 200, body: day({ items: [item({ status: approved ? 'approved' : 'ready' })] }) }),
      [`POST ${url}/te/approve`]: () => {
        approved = true
        return { status: 200, body: day({ items: [item({ status: 'approved' })] }) }
      },
    })
    renderCard()
    await screen.findByText('Ready')

    await userEvent.click(screen.getByRole('button', { name: 'Play' }))
    await waitFor(() => expect(screen.getByRole('button', { name: 'Play' })).toBeInTheDocument())
    await userEvent.click(screen.getByRole('button', { name: 'Approve this clip' }))

    expect(await screen.findByText('Approved. It goes with tonight’s message.')).toBeInTheDocument()
    expect(posts(calls, 'te/approve')).toEqual([expect.objectContaining({ clipId: 'clip-1' })])
  })

  it('shows the words she is approving', async () => {
    fakeApi({ [`GET ${url}`]: () => ({ status: 200, body: day() }) })
    renderCard()

    await userEvent.click(await screen.findByText('Read the words'))

    expect(screen.getByText('ఈ రోజు మేము ముఖ విలువ నేర్చుకున్నాం.')).toBeVisible()
  })

  it('offers Try again when a clip failed, and asks for a new one', async () => {
    const calls = fakeApi({
      [`GET ${url}`]: () => ({ status: 200, body: day({ items: [item({ status: 'failed', clipId: null, segments: [], transcript: null })] }) }),
      [`POST ${url}/te/regenerate`]: () => ({ status: 202, body: day({ items: [item({ status: 'generating', clipId: null, segments: [] })] }) }),
    })
    renderCard()

    expect(await screen.findByText('Could not make this clip. Nothing will be sent for this language.')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Try again' }))

    await waitFor(() => expect(posts(calls, 'te/regenerate')).toHaveLength(1))
  })

  it('shows the latest when the day changed under her (409)', async () => {
    fakeApi({
      [`GET ${url}`]: () => ({ status: 200, body: day() }),
      [`POST ${url}/te/approve`]: () => ({ status: 409, body: day() }),
    })
    renderCard()
    await userEvent.click(await screen.findByRole('button', { name: 'Play' }))
    await new Promise((resolve) => setTimeout(resolve, 20))

    await userEvent.click(screen.getByRole('button', { name: 'Approve this clip' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('This changed on another device. Showing the latest.')
  })

  it('is read-only once tonight’s messages have gone', async () => {
    fakeApi({ [`GET ${url}`]: () => ({ status: 200, body: day({ available: false, sent: true, items: [item({ status: 'approved' })] }) }) })
    renderCard({ ...evening, sent: 2 })

    const box = await screen.findByRole('checkbox')

    expect(box).toBeChecked()
    expect(box).toBeDisabled()
    expect(screen.getByText('Tonight’s messages have gone. This can no longer be changed.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Approve this clip' })).not.toBeInTheDocument()
  })
})
