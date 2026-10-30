import { QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
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

/** The admin API: the list and one clip, each only with the right key. */
function stubApi(greetings: unknown[], key = 'secret-key') {
  const fetch = vi.fn<typeof globalThis.fetch>(async (input, init) => {
    const sent = new Headers(init?.headers).get('X-Admin-Key')
    if (sent !== key) {
      return new Response(null, { status: 404 })
    }
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

  afterEach(() => vi.unstubAllGlobals())

  it('lists every greeting with the key from the link and plays one', async () => {
    const user = userEvent.setup()
    const fetch = stubApi([shobha])
    renderAt('/admin/greetings#key=secret-key')

    expect(await screen.findByText('Shobha')).toBeInTheDocument()
    expect(screen.getByText('1 greeting')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Play Shobha’s greeting' }))

    expect(await screen.findByLabelText('Shobha’s greeting')).toHaveAttribute('src', 'blob:greeting')
    expect(fetch).toHaveBeenCalledWith(`/api/admin/greetings/${clipId}/audio`, expect.anything())
    expect(fetch.mock.calls.every(([url]) => !String(url).includes('secret-key'))).toBe(true)
  })

  it('asks for the full link without a key and calls nothing', async () => {
    const fetch = stubApi([shobha])
    renderAt('/admin/greetings')

    expect(screen.getByRole('alert')).toHaveTextContent('This page needs its full link.')
    expect(fetch).not.toHaveBeenCalled()
  })

  it('a wrong key gets the same message as no key', async () => {
    stubApi([shobha])
    renderAt('/admin/greetings#key=wrong')

    expect(await screen.findByText('This page needs its full link.')).toBeInTheDocument()
    expect(screen.queryByText('Shobha')).not.toBeInTheDocument()
  })

  it('says so when nobody has recorded a greeting', async () => {
    stubApi([])
    renderAt('/admin/greetings#key=secret-key')

    expect(await screen.findByText('No greetings recorded yet.')).toBeInTheDocument()
  })
})
