import { QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { createQueryClient } from '../../app/queryClient'
import { initI18n } from '../../i18n'
import { db } from '../../offline/db'
import { teacher } from '../../test/renderApp'
import { LessonCard } from '../today/LessonCard'
import { LessonPhoto } from './LessonPhoto'

const i18n = initI18n()
const date = '2026-10-30'
const audioUrl = `/api/classes/${teacher.classId}/lessons/${date}/audio`
const page = () => new File([new Uint8Array([1, 2, 3])], 'page.jpg', { type: 'image/jpeg' })

describe('LessonPhoto (T-64)', () => {
  beforeEach(async () => {
    await Promise.all([db.outbox.clear(), db.photos.clear(), db.notes.clear()])
    await i18n.changeLanguage('en')
    URL.createObjectURL = vi.fn<() => string>(() => 'blob:page')
    URL.revokeObjectURL = vi.fn<() => void>()
  })

  afterEach(() => vi.unstubAllGlobals())

  it('adds, previews, replaces and removes one photo, kept on the phone only', async () => {
    const user = userEvent.setup()
    const fetch = vi.fn<typeof globalThis.fetch>()
    vi.stubGlobal('fetch', fetch)
    render(<LessonPhoto date={date} onMatch={() => {}} />)

    expect(screen.getByText('Photograph the textbook page or the board, not the children.')).toBeInTheDocument()
    await user.upload(screen.getByTestId('photo-input'), page())

    expect(await screen.findByRole('img', { name: 'Your photo of today’s lesson' })).toHaveAttribute('src', 'blob:page')
    expect((await db.photos.get(date))?.type).toBe('image/jpeg')

    await user.click(screen.getByRole('button', { name: 'Replace photo' }))
    await user.upload(screen.getByTestId('photo-input'), new File([new Uint8Array([9])], 'board.png', { type: 'image/png' }))
    await waitFor(async () => expect((await db.photos.get(date))?.type).toBe('image/png'))
    expect(await db.photos.count()).toBe(1)

    await user.click(screen.getByRole('button', { name: 'Remove photo' }))

    expect(await screen.findByRole('button', { name: 'Add a photo of the page' })).toBeInTheDocument()
    expect(await db.photos.count()).toBe(0)
    expect(fetch).not.toHaveBeenCalled()
  })

  it('"Match from photo" on Today sends the photo alone as the photo part', async () => {
    const user = userEvent.setup()
    let sent: FormData | undefined
    vi.stubGlobal(
      'fetch',
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        if (String(input) === audioUrl && init?.method === 'POST') {
          sent = init.body as FormData
        }
        return new Response(JSON.stringify({ date, lesson: null, nextInBook: null }), {
          status: String(input) === audioUrl ? 202 : 200,
          headers: { 'Content-Type': 'application/json' },
        })
      }),
    )
    render(
      <QueryClientProvider client={createQueryClient()}>
        <MemoryRouter>
          <LessonCard
            classId={teacher.classId}
            date={date}
            day={{ date, lesson: null, nextInBook: null }}
            chapters={[]}
            onPick={() => {}}
          />
        </MemoryRouter>
      </QueryClientProvider>,
    )

    await user.upload(screen.getByTestId('photo-input'), page())
    await user.click(await screen.findByRole('button', { name: 'Match from photo' }))

    await waitFor(() => expect(sent).toBeDefined())
    expect(sent?.get('photo')).toBeInstanceOf(Blob)
    expect(sent?.get('file')).toBeNull()
    expect(sent?.get('clientRequestId')).toBeTruthy()
    expect(await db.outbox.count()).toBe(0)
  })
})
