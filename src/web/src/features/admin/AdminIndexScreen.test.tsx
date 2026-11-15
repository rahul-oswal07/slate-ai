import { QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { App } from '../../app/App'
import { createQueryClient } from '../../app/queryClient'
import { initI18n } from '../../i18n'

const i18n = initI18n()

function renderAt(url: string) {
  render(
    <QueryClientProvider client={createQueryClient()}>
      <MemoryRouter initialEntries={[url]}>
        <App />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('/admin: every admin page', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en')
    // The index asks the API nothing; anything that does gets a 404, as without a key.
    vi.stubGlobal('fetch', vi.fn<typeof globalThis.fetch>(async () => new Response(null, { status: 404 })))
  })

  afterEach(() => vi.unstubAllGlobals())

  it('lists each page with its address and what it is for', async () => {
    renderAt('/admin#key=secret-key')

    expect(await screen.findByRole('heading', { name: 'Admin pages' })).toBeInTheDocument()
    const list = screen.getByRole('list')
    const items = within(list).getAllByRole('listitem')
    expect(items.map((item) => within(item).getByRole('link').textContent)).toEqual(['Greetings', 'Lesson audio', 'Lesson links'])
    expect(within(list).getByText('/admin/lesson-links')).toBeInTheDocument()
    expect(within(list).getByText(/Set the video link for each lesson/)).toBeInTheDocument()
  })

  it('its links keep the key in the fragment, never in the path', async () => {
    renderAt('/admin#key=secret-key')

    const links = within(await screen.findByRole('list')).getAllByRole('link')

    expect(links.map((link) => link.getAttribute('href'))).toEqual([
      '/admin/greetings#key=secret-key',
      '/admin/lesson-audio#key=secret-key',
      '/admin/lesson-links#key=secret-key',
    ])
  })

  it('shows without a key', async () => {
    renderAt('/admin')

    const list = await screen.findByRole('list')
    expect(within(list).getByRole('link', { name: 'Lesson links' })).toHaveAttribute('href', '/admin/lesson-links')
  })

  it('every admin page links back to it', async () => {
    renderAt('/admin/lesson-links#key=secret-key')

    const nav = await screen.findByRole('navigation', { name: 'Admin pages' })
    expect(within(nav).getByRole('link', { name: 'All pages' })).toHaveAttribute('href', '/admin#key=secret-key')
  })
})
