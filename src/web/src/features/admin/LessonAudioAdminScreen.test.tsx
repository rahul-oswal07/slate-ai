import { QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { createQueryClient } from '../../app/queryClient'
import { initI18n } from '../../i18n'
import { istToday } from '../today/schoolTime'
import { LessonAudioAdminScreen } from './LessonAudioAdminScreen'
import type { AdminLessonAudioDay } from './useAdmin'

const i18n = initI18n()
const clipId = '22222222-2222-2222-2222-222222222222'
const sixPm = '2026-10-29T12:30:00Z'

const day: AdminLessonAudioDay = {
  date: '2026-10-29',
  sentCount: 2,
  audios: [
    {
      id: 'a1',
      classLabel: 'Shobha · Grade 3',
      language: 'te',
      lessons: ['3-digit addition with carrying'],
      status: 'approved',
      verdict: 'valid',
      approvedBy: 'Lakshmi',
      approvedAt: '2026-10-29T10:00:00Z',
      clipId,
      seconds: 42.5,
      transcript: 'ఈ రోజు మనం కూడిక నేర్చుకున్నాం.',
      families: [
        { childName: 'Ravi', rollNo: 4, language: 'te', sentAt: sixPm },
        { childName: 'Rohan', rollNo: 9, language: 'te', sentAt: sixPm },
      ],
    },
    {
      id: 'a2',
      classLabel: 'Shobha · Grade 3',
      language: 'hi',
      lessons: ['3-digit addition with carrying'],
      status: 'failed',
      verdict: 'fallback:numbers',
      approvedBy: null,
      approvedAt: null,
      clipId: null,
      seconds: null,
      transcript: null,
      families: [],
    },
  ],
}

function renderAt(url: string) {
  render(
    <QueryClientProvider client={createQueryClient()}>
      <MemoryRouter initialEntries={[url]}>
        <LessonAudioAdminScreen />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

/** The admin API for the day view: any date answers with `body`, only with the right key. */
function stubApi(body: AdminLessonAudioDay, key = 'secret-key') {
  const fetch = vi.fn<typeof globalThis.fetch>(async (input, init) => {
    if (new Headers(init?.headers).get('X-Admin-Key') !== key) {
      return new Response(null, { status: 404 })
    }
    return String(input).endsWith('/audio')
      ? // Raw bytes, not a jsdom Blob: Node 22's Response can't read one (CI).
        new Response(new Uint8Array([1, 2]), { status: 200, headers: { 'Content-Type': 'audio/ogg' } })
      : new Response(JSON.stringify(body), { status: 200, headers: { 'Content-Type': 'application/json' } })
  })
  vi.stubGlobal('fetch', fetch)
  return fetch
}

describe('LessonAudioAdminScreen (T-67)', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en')
    URL.createObjectURL = vi.fn<() => string>(() => 'blob:lesson')
    URL.revokeObjectURL = vi.fn<() => void>()
  })

  afterEach(() => vi.unstubAllGlobals())

  it('shows today by default, then the chosen day, always with the key header', async () => {
    const fetch = stubApi(day)
    renderAt('/admin/lesson-audio#key=secret-key')

    expect(await screen.findByText('2 lesson audios sent to families')).toBeInTheDocument()
    expect(fetch).toHaveBeenCalledWith(`/api/admin/lesson-audio?date=${istToday()}`, expect.anything())

    fireEvent.change(screen.getByLabelText('Day'), { target: { value: '2026-10-29' } })

    await waitFor(() => expect(fetch).toHaveBeenCalledWith('/api/admin/lesson-audio?date=2026-10-29', expect.anything()))
    expect(fetch.mock.calls.every(([url]) => !String(url).includes('secret-key'))).toBe(true)
  })

  it('details a sent audio: lesson, approval, words, families, and plays it', async () => {
    const user = userEvent.setup()
    const fetch = stubApi(day)
    renderAt('/admin/lesson-audio#key=secret-key')

    const card = (await screen.findByText('Shobha · Grade 3 · తెలుగు')).closest('li') as HTMLElement
    expect(within(card).getByText('Sent')).toBeInTheDocument()
    expect(within(card).getByText('3-digit addition with carrying')).toBeInTheDocument()
    expect(within(card).getByText(/^Approved by Lakshmi/)).toBeInTheDocument()
    expect(within(card).getByText('Sent to 2 families')).toBeInTheDocument()
    expect(within(card).getByText('Ravi (roll 4)')).toBeInTheDocument()
    expect(within(card).getByText('Rohan (roll 9)')).toBeInTheDocument()
    await user.click(within(card).getByText('Words spoken'))
    expect(within(card).getByText('ఈ రోజు మనం కూడిక నేర్చుకున్నాం.')).toBeVisible()

    await user.click(within(card).getByRole('button', { name: 'Play the తెలుగు lesson audio (42.5 s)' }))

    expect(await within(card).findByLabelText('తెలుగు lesson audio')).toHaveAttribute('src', 'blob:lesson')
    expect(fetch).toHaveBeenCalledWith(`/api/admin/lesson-audio/${clipId}/audio`, expect.anything())
  })

  it('lists audio that never went out with where it stopped', async () => {
    stubApi(day)
    renderAt('/admin/lesson-audio#key=secret-key')

    const card = (await screen.findByText('Shobha · Grade 3 · हिन्दी')).closest('li') as HTMLElement
    expect(within(card).getByText('Failed')).toBeInTheDocument()
    expect(within(card).getByText('Reason: fallback:numbers')).toBeInTheDocument()
    expect(within(card).queryByRole('button')).not.toBeInTheDocument()
    expect(within(card).queryByText(/^Sent to/)).not.toBeInTheDocument()
  })

  it('says so when the day has no lesson audio', async () => {
    stubApi({ date: '2026-10-29', sentCount: 0, audios: [] })
    renderAt('/admin/lesson-audio#key=secret-key')

    expect(await screen.findByText('0 lesson audios sent to families')).toBeInTheDocument()
    expect(screen.getByText('No lesson audio on this day.')).toBeInTheDocument()
  })

  it('needs the full link, and the nav keeps the key', async () => {
    stubApi(day)
    renderAt('/admin/lesson-audio#key=wrong')

    expect(await screen.findByText('This page needs its full link.')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Greetings' })).toHaveAttribute('href', '/admin/greetings#key=wrong')
    expect(screen.getByRole('link', { name: 'Lesson audio' })).toHaveAttribute('href', '/admin/lesson-audio#key=wrong')
  })
})
