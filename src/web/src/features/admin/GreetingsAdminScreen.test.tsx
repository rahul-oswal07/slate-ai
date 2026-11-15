import { QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { createQueryClient } from '../../app/queryClient'
import { initI18n } from '../../i18n'
import { GreetingsAdminScreen } from './GreetingsAdminScreen'

const i18n = initI18n()
const clipId = '11111111-1111-1111-1111-111111111111'
const shobha = { clipId, teacherName: 'Shobha', seconds: 8.2, recordedAt: '2026-10-28T09:00:00Z' }

function renderAt(url: string) {
  render(
    <QueryClientProvider client={createQueryClient()}>
      <MemoryRouter initialEntries={[url]}>
        <GreetingsAdminScreen />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

/** The admin API: the list and one clip, no key needed (D-112). */
function stubApi(greetings: unknown[]) {
  const fetch = vi.fn<typeof globalThis.fetch>(async (input) => {
    return String(input).endsWith('/audio')
      ? // Raw bytes, not a jsdom Blob: Node 22's Response can't read one (CI).
        new Response(new Uint8Array([1, 2]), { status: 200, headers: { 'Content-Type': 'audio/ogg' } })
      : new Response(JSON.stringify(greetings), { status: 200, headers: { 'Content-Type': 'application/json' } })
  })
  vi.stubGlobal('fetch', fetch)
  return fetch
}

describe('GreetingsAdminScreen (T-66)', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en')
    URL.createObjectURL = vi.fn<() => string>(() => 'blob:greeting')
    URL.revokeObjectURL = vi.fn<() => void>()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('lists every greeting and plays one', async () => {
    const user = userEvent.setup()
    const fetch = stubApi([shobha])
    renderAt('/admin/greetings')

    expect(await screen.findByText('Shobha')).toBeInTheDocument()
    expect(screen.getByText('1 greeting')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Play Shobha’s greeting' }))

    expect(await screen.findByLabelText('Shobha’s greeting')).toHaveAttribute('src', 'blob:greeting')
    expect(fetch).toHaveBeenCalledWith(`/api/admin/greetings/${clipId}/audio`)
    expect(screen.getByRole('link', { name: 'Lesson audio' })).toHaveAttribute('href', '/admin/lesson-audio')
  })

  it('downloads a greeting as a named file (T-68)', async () => {
    const user = userEvent.setup()
    const fetch = stubApi([shobha])
    const saved: { href: string; download: string }[] = []
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
      saved.push({ href: this.href, download: this.download })
    })
    renderAt('/admin/greetings')

    await user.click(await screen.findByRole('button', { name: 'Download Shobha’s greeting' }))

    await waitFor(() => expect(saved).toEqual([{ href: 'blob:greeting', download: 'greeting-shobha-2026-10-28.ogg' }]))
    expect(fetch).toHaveBeenCalledWith(`/api/admin/greetings/${clipId}/audio`)
    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:greeting')
  })

  it('says so when a download fails', async () => {
    const user = userEvent.setup()
    const fetch = stubApi([shobha])
    renderAt('/admin/greetings')
    const button = await screen.findByRole('button', { name: 'Download Shobha’s greeting' })
    fetch.mockResolvedValueOnce(new Response(null, { status: 500 }))

    await user.click(button)

    expect(await screen.findByRole('alert')).toHaveTextContent('Couldn’t download this audio. Try again.')
  })

  it('says so when the page fails to load', async () => {
    const fetch = stubApi([shobha])
    fetch.mockResolvedValueOnce(new Response(null, { status: 404 }))
    renderAt('/admin/greetings')

    expect(await screen.findByRole('alert')).toHaveTextContent('Couldn’t load this page.')
    expect(screen.queryByText('Shobha')).not.toBeInTheDocument()
  })

  it('says so when nobody has recorded a greeting', async () => {
    stubApi([])
    renderAt('/admin/greetings')

    expect(await screen.findByText('No greetings recorded yet.')).toBeInTheDocument()
  })
})
