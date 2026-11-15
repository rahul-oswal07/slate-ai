import { QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { createQueryClient } from '../../app/queryClient'
import { initI18n } from '../../i18n'
import { AdminChildrenScreen } from './AdminChildrenScreen'
import type { AdminChild, AdminChildren } from './useAdmin'

const i18n = initI18n()

const children: AdminChild[] = [
  { id: 'c1', rollNo: 1, firstName: 'Ananya', homeLanguage: 'kn', consent: 'no', stopped: false, phone: null, phoneFrom: null },
  { id: 'c15', rollNo: 15, firstName: 'Manjunath', homeLanguage: 'kn', consent: 'yes', stopped: false, phone: '+919876543210', phoneFrom: 'saved' },
  { id: 'c24', rollNo: 24, firstName: 'Ravi', homeLanguage: 'te', consent: 'yes', stopped: true, phone: '+919812345678', phoneFrom: 'setting' },
]

/** The admin API (no key, D-112): GET lists the class; PUT saves (as E.164), clears (blank) or refuses a short number (400). */
function stubApi() {
  const rows = new Map(children.map((c) => [c.id, c]))
  const calls: { method: string; url: string; body: { phone?: string } | null }[] = []
  vi.stubGlobal(
    'fetch',
    vi.fn<typeof globalThis.fetch>(async (input, init) => {
      const method = init?.method ?? 'GET'
      const body = init?.body ? (JSON.parse(String(init.body)) as { phone?: string }) : null
      calls.push({ method, url: String(input), body })
      if (method === 'PUT') {
        const id = String(input).split('/').at(-2) ?? ''
        const digits = (body?.phone ?? '').replace(/\D/g, '')
        if (digits !== '' && digits.length !== 10) {
          return Response.json({ errors: { phone: ['Use a 10-digit Indian mobile number.'] } }, { status: 400 })
        }
        const current = rows.get(id)
        if (!current) {
          return new Response(null, { status: 404 })
        }
        const updated: AdminChild =
          digits === '' ? { ...current, phone: null, phoneFrom: null } : { ...current, phone: `+91${digits}`, phoneFrom: 'saved' }
        rows.set(id, updated)
        return Response.json(updated)
      }
      const list: AdminChildren = { classes: [{ id: 'k1', label: 'Shobha · Grade 3', children: [...rows.values()] }] }
      return Response.json(list)
    }),
  )
  return { calls }
}

function renderAt(url = '/admin/children') {
  render(
    <QueryClientProvider client={createQueryClient()}>
      <MemoryRouter initialEntries={[url]}>
        <AdminChildrenScreen />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

const row = async (name: RegExp) => (await screen.findByText(name)).closest('li') as HTMLElement

describe('AdminChildrenScreen (T-118)', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('lists the class with each child, their number and where it comes from', async () => {
    stubApi()
    renderAt()

    expect(await screen.findByRole('heading', { level: 2, name: 'Shobha · Grade 3 · 3 children' })).toBeInTheDocument()
    const ananya = await row(/1 · Ananya · Kannada/)
    expect(within(ananya).getByText('No number: WhatsApp skips this family')).toBeInTheDocument()
    expect(within(ananya).getByText('No consent: nothing is sent')).toBeInTheDocument()
    const manjunath = await row(/15 · Manjunath · Kannada/)
    expect(within(manjunath).getByText('+919876543210')).toBeInTheDocument()
    expect(within(manjunath).getByText('Saved here')).toBeInTheDocument()
    expect(within(manjunath).getByRole('button', { name: "Clear Manjunath's saved number" })).toBeInTheDocument()
    const ravi = await row(/24 · Ravi · Telugu/)
    expect(within(ravi).getByText('From the server setting')).toBeInTheDocument()
    expect(within(ravi).getByText('Family stopped messages: nothing is sent')).toBeInTheDocument()
    expect(within(ravi).queryByRole('button', { name: /Clear/ })).not.toBeInTheDocument()
  })

  it('Edit then Save sends the number and shows it saved', async () => {
    const user = userEvent.setup()
    const { calls } = stubApi()
    renderAt()

    await user.click(await screen.findByRole('button', { name: "Edit Ananya's number" }))
    await user.type(screen.getByLabelText("Ananya's family's number"), '98450 12345')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    const ananya = await row(/1 · Ananya/)
    expect(await within(ananya).findByText('+919845012345')).toBeInTheDocument()
    expect(within(ananya).getByText('Saved here')).toBeInTheDocument()
    expect(within(ananya).getByText('Saved')).toBeInTheDocument()
    const put = calls.find((c) => c.method === 'PUT')
    expect(put?.url).toBe('/api/admin/children/c1/phone')
    expect(put?.body).toMatchObject({ phone: '98450 12345' })
  })

  it('a refused number says how to fix it and keeps what was typed', async () => {
    const user = userEvent.setup()
    stubApi()
    renderAt()

    await user.click(await screen.findByRole('button', { name: "Edit Ananya's number" }))
    const input = screen.getByLabelText("Ananya's family's number")
    await user.type(input, '12345')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Use a 10-digit Indian mobile number')
    expect(input).toHaveValue('12345')
    expect(input).toHaveAttribute('aria-invalid', 'true')
  })

  it('Clear removes the saved number', async () => {
    const user = userEvent.setup()
    const { calls } = stubApi()
    renderAt()

    await user.click(await screen.findByRole('button', { name: "Clear Manjunath's saved number" }))

    const manjunath = await row(/15 · Manjunath/)
    expect(await within(manjunath).findByText('No number: WhatsApp skips this family')).toBeInTheDocument()
    expect(within(manjunath).getByText('Cleared: back to the server setting, if any')).toBeInTheDocument()
    expect(calls.find((c) => c.method === 'PUT')?.body).toMatchObject({ phone: '' })
  })
})
