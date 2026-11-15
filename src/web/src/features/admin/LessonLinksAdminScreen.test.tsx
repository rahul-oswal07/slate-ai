import { QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { createQueryClient } from '../../app/queryClient'
import { initI18n } from '../../i18n'
import { LessonLinksAdminScreen } from './LessonLinksAdminScreen'
import type { AdminLessonLinks } from './useAdmin'

const i18n = initI18n()
const diksha = 'https://diksha.gov.in/play/content/do_1'

const compare = {
  id: 'M3.BRIDGE.COMPARE',
  nameKn: 'ಹೆಚ್ಚು – ಕಡಿಮೆ – ಸಮ',
  nameEn: 'More, less, equal',
  textbookPage: { book: 'nalikali' as const, part: 1, page: 4 },
}

const list = (url: string | null): AdminLessonLinks => ({
  chapters: [
    {
      number: 0,
      nameKn: 'ಸೇತುಬಂಧ',
      nameEn: 'Bridge course',
      lessons: [{ ...compare, url }],
    },
    {
      number: 5,
      nameKn: 'ಕಳೆಯುವ ಮೋಜು',
      nameEn: 'The fun of subtracting',
      lessons: [{ id: 'M3.SUB.BORROW', nameKn: 'ಎರವಲು', nameEn: 'Subtraction with borrowing', textbookPage: { book: 'textbook', part: 2, page: 15 }, url: null }],
    },
  ],
})

function renderAt(url: string) {
  render(
    <QueryClientProvider client={createQueryClient()}>
      <MemoryRouter initialEntries={[url]}>
        <LessonLinksAdminScreen />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

/** The admin API with the key; PUT stores the link (blank removes it) or refuses anything but https with a 400. */
function stubApi(key = 'secret-key', initial: string | null = null) {
  let url = initial
  const calls: { method: string; url: string; body: unknown }[] = []
  vi.stubGlobal(
    'fetch',
    vi.fn<typeof globalThis.fetch>(async (input, init) => {
      if (new Headers(init?.headers).get('X-Admin-Key') !== key) {
        return new Response(null, { status: 404 })
      }
      const method = init?.method ?? 'GET'
      const body: unknown = init?.body ? JSON.parse(String(init.body)) : null
      calls.push({ method, url: String(input), body })
      if (method === 'PUT') {
        const sent = ((body as { url: string }).url ?? '').trim()
        if (sent !== '' && !sent.startsWith('https://')) {
          return Response.json({ errors: { url: ['Use a full https link.'] } }, { status: 400 })
        }
        url = sent === '' ? null : sent
        return Response.json({ ...compare, url })
      }
      return Response.json(list(url))
    }),
  )
  return calls
}

describe('LessonLinksAdminScreen (T-94)', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('lists every chapter and lesson with its page', async () => {
    stubApi()
    renderAt('/admin/lesson-links#key=secret-key')

    expect(await screen.findByRole('heading', { name: 'Bridge course' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Chapter 5 · The fun of subtracting' })).toBeInTheDocument()
    expect(screen.getByRole('textbox', { name: /More, less, equal/ })).toHaveValue('')
    expect(screen.getByText('Part 2, page 15')).toBeInTheDocument()
  })

  it('saves a link with the key in a header, never in the URL', async () => {
    const calls = stubApi()
    renderAt('/admin/lesson-links#key=secret-key')

    await userEvent.type(await screen.findByRole('textbox', { name: /More, less, equal/ }), diksha)
    await userEvent.click(screen.getByRole('button', { name: 'Save the link for More, less, equal' }))

    expect(await screen.findByText('Saved')).toBeInTheDocument()
    const put = calls.find((c) => c.method === 'PUT')
    expect(put?.url).toBe('/api/admin/lesson-links/M3.BRIDGE.COMPARE')
    expect(put?.body).toMatchObject({ url: diksha })
    expect(calls.every((c) => !c.url.includes('secret-key'))).toBe(true)
  })

  it('a link that isn’t https is refused, and says how to fix it', async () => {
    stubApi()
    renderAt('/admin/lesson-links#key=secret-key')
    const input = await screen.findByRole('textbox', { name: /More, less, equal/ })

    await userEvent.type(input, 'http://diksha.gov.in/x')
    await userEvent.click(screen.getByRole('button', { name: 'Save the link for More, less, equal' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Use a full https link')
    expect(input).toHaveAttribute('aria-invalid', 'true')
  })

  it('removes a link', async () => {
    const calls = stubApi('secret-key', diksha)
    renderAt('/admin/lesson-links#key=secret-key')
    const card = (await screen.findByRole('textbox', { name: /More, less, equal/ })).closest('form') as HTMLElement

    await userEvent.click(within(card).getByRole('button', { name: 'Remove the link for More, less, equal' }))

    expect(await within(card).findByText('Removed')).toBeInTheDocument()
    await waitFor(() => expect(within(card).getByRole('textbox')).toHaveValue(''))
    expect(calls.find((c) => c.method === 'PUT')?.body).toMatchObject({ url: '' })
  })

  it('without the key it asks for the full link', async () => {
    stubApi()
    renderAt('/admin/lesson-links')

    expect(await screen.findByText('This page needs its full link.')).toBeInTheDocument()
  })
})
