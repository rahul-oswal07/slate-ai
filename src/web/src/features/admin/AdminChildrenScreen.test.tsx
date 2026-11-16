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
  { id: 'c1', rollNo: 1, firstName: 'Ananya', homeLanguage: 'kn', consent: 'no', stopped: false, phone: null, phone2: null, languageChanged: false },
  { id: 'c15', rollNo: 15, firstName: 'Manjunath', homeLanguage: 'kn', consent: 'yes', stopped: false, phone: '+919876543210', phone2: '+919845012345', languageChanged: false },
  { id: 'c24', rollNo: 24, firstName: 'Ravi', homeLanguage: 'te', consent: 'yes', stopped: true, phone: '+919812345678', phone2: null, languageChanged: true },
]

/**
 * The admin API (no key, D-112): GET lists the class; PUT …/phone saves number 1 or 2 (`slot`, T-140) as E.164, clears
 * it (blank) or refuses a short number (400); PUT …/language changes the home language (T-122), or fails with 500 when `languageFails`.
 */
function stubApi({ languageFails = false, demo = false }: { languageFails?: boolean; demo?: boolean } = {}) {
  const rows = new Map(children.map((c) => [c.id, c]))
  const calls: {
    method: string
    url: string
    body: { phone?: string; slot?: number; language?: string; clientRequestId?: string } | null
  }[] = []
  vi.stubGlobal(
    'fetch',
    vi.fn<typeof globalThis.fetch>(async (input, init) => {
      const method = init?.method ?? 'GET'
      const body = init?.body ? (JSON.parse(String(init.body)) as { phone?: string; slot?: number; language?: string }) : null
      calls.push({ method, url: String(input), body })
      if (method === 'PUT' && String(input).endsWith('/language')) {
        const id = String(input).split('/').at(-2) ?? ''
        const current = rows.get(id)
        if (languageFails || !current) {
          return new Response(null, { status: languageFails ? 500 : 404 })
        }
        const updated: AdminChild = { ...current, homeLanguage: body?.language ?? current.homeLanguage, languageChanged: true }
        rows.set(id, updated)
        return Response.json(updated)
      }
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
        const value = digits === '' ? null : `+91${digits}`
        const updated: AdminChild = body?.slot === 2 ? { ...current, phone2: value } : { ...current, phone: value }
        rows.set(id, updated)
        return Response.json(updated)
      }
      const list: AdminChildren = { classes: [{ id: 'k1', label: 'Shobha · Grade 3', demo, children: [...rows.values()] }] }
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

  it('lists the class with each child and the two numbers saved for them (T-123, T-140)', async () => {
    stubApi()
    renderAt()

    expect(await screen.findByRole('heading', { level: 2, name: 'Shobha · Grade 3 · 3 children' })).toBeInTheDocument()
    const ananya = await row(/1 · Ananya · Kannada/)
    expect(within(ananya).getByText('No number: WhatsApp skips this family')).toBeInTheDocument()
    expect(within(ananya).getAllByText('Not set')).toHaveLength(2)
    expect(within(ananya).getByText('No consent: nothing is sent')).toBeInTheDocument()
    const manjunath = await row(/15 · Manjunath · Kannada/)
    expect(within(manjunath).getByText('+919876543210')).toBeInTheDocument()
    expect(within(manjunath).getByText('+919845012345')).toBeInTheDocument()
    expect(within(manjunath).getByRole('button', { name: "Clear Manjunath's number 1" })).toBeInTheDocument()
    expect(within(manjunath).getByRole('button', { name: "Clear Manjunath's number 2" })).toBeInTheDocument()
    const ravi = await row(/24 · Ravi · Telugu/)
    expect(within(ravi).getByText('+919812345678')).toBeInTheDocument()
    expect(within(ravi).getByText('Family stopped messages: nothing is sent')).toBeInTheDocument()
    expect(within(ravi).getByRole('button', { name: "Clear Ravi's number 1" })).toBeInTheDocument()
    expect(within(ravi).queryByRole('button', { name: "Clear Ravi's number 2" })).not.toBeInTheDocument()
    expect(within(ananya).queryByRole('button', { name: /Clear/ })).not.toBeInTheDocument()
  })

  it("links each demo child's simulated family phone (T-130)", async () => {
    stubApi({ demo: true })
    renderAt()

    const manjunath = await row(/15 · Manjunath/)
    expect(within(manjunath).getByRole('link', { name: 'Family phone (simulated) for Manjunath' })).toHaveAttribute(
      'href',
      '/demo/family/c15',
    )
    expect(screen.getAllByRole('link', { name: /Family phone \(simulated\)/ }).map((a) => a.getAttribute('href'))).toEqual([
      '/demo/family/c1',
      '/demo/family/c15',
      '/demo/family/c24',
    ])
  })

  it('a class that is not the demo class has no family phone links (T-130)', async () => {
    stubApi()
    renderAt()

    await row(/15 · Manjunath/)
    expect(screen.queryByRole('link', { name: /Family phone \(simulated\)/ })).not.toBeInTheDocument()
  })

  it('Edit then Save sends the number and shows it saved', async () => {
    const user = userEvent.setup()
    const { calls } = stubApi()
    renderAt()

    await user.click(await screen.findByRole('button', { name: "Edit Ananya's number 1" }))
    await user.type(screen.getByLabelText("Ananya's family's number 1"), '98450 12345')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    const ananya = await row(/1 · Ananya/)
    expect(await within(ananya).findByText('+919845012345')).toBeInTheDocument()
    expect(within(ananya).getByText('Saved')).toBeInTheDocument()
    expect(within(ananya).queryByText('No number: WhatsApp skips this family')).not.toBeInTheDocument()
    const put = calls.find((c) => c.method === 'PUT')
    expect(put?.url).toBe('/api/admin/children/c1/phone')
    expect(put?.body).toMatchObject({ phone: '98450 12345', slot: 1 })
  })

  it('a refused number says how to fix it and keeps what was typed', async () => {
    const user = userEvent.setup()
    stubApi()
    renderAt()

    await user.click(await screen.findByRole('button', { name: "Edit Ananya's number 1" }))
    const input = screen.getByLabelText("Ananya's family's number 1")
    await user.type(input, '12345')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Use a 10-digit Indian mobile number')
    expect(input).toHaveValue('12345')
    expect(input).toHaveAttribute('aria-invalid', 'true')
  })

  it('Number 2 is saved on its own and Number 1 stays (T-140)', async () => {
    const user = userEvent.setup()
    const { calls } = stubApi()
    renderAt()

    await user.click(await screen.findByRole('button', { name: "Edit Ravi's number 2" }))
    await user.type(screen.getByLabelText("Ravi's family's number 2"), '98450 99999')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    const ravi = await row(/24 · Ravi/)
    expect(await within(ravi).findByText('+919845099999')).toBeInTheDocument()
    expect(within(ravi).getByText('+919812345678')).toBeInTheDocument()
    expect(within(ravi).queryByText('Not set')).not.toBeInTheDocument()
    expect(calls.find((c) => c.method === 'PUT')?.body).toMatchObject({ phone: '98450 99999', slot: 2 })
  })

  it('Clear removes one number and keeps the other; with neither the family is skipped (T-140)', async () => {
    const user = userEvent.setup()
    const { calls } = stubApi()
    renderAt()

    await user.click(await screen.findByRole('button', { name: "Clear Manjunath's number 1" }))

    const manjunath = await row(/15 · Manjunath/)
    expect(await within(manjunath).findByText('Cleared')).toBeInTheDocument()
    expect(within(manjunath).getByText('+919845012345')).toBeInTheDocument()
    expect(within(manjunath).getByText('Not set')).toBeInTheDocument()
    expect(within(manjunath).queryByText('No number: WhatsApp skips this family')).not.toBeInTheDocument()
    expect(calls.find((c) => c.method === 'PUT')?.body).toMatchObject({ phone: '', slot: 1 })

    await user.click(within(manjunath).getByRole('button', { name: "Clear Manjunath's number 2" }))

    expect(await within(manjunath).findByText('No number: WhatsApp skips this family')).toBeInTheDocument()
    expect(within(manjunath).getAllByText('Not set')).toHaveLength(2)
    expect(calls.filter((c) => c.method === 'PUT').at(-1)?.body).toMatchObject({ phone: '', slot: 2 })
  })

  it('Edit starts from the saved number of that slot', async () => {
    const user = userEvent.setup()
    stubApi()
    renderAt()

    await user.click(await screen.findByRole('button', { name: "Edit Manjunath's number 2" }))

    expect(screen.getByLabelText("Manjunath's family's number 2")).toHaveValue('+919845012345')
    expect(screen.queryByLabelText("Manjunath's family's number 1")).not.toBeInTheDocument()
  })

  it('choosing another home language saves it at once (T-122)', async () => {
    const user = userEvent.setup()
    const { calls } = stubApi()
    renderAt()

    const select = await screen.findByLabelText("Ananya's home language")
    expect(select).toHaveValue('kn')
    expect(within(select).getAllByRole('option').map((o) => o.textContent)).toEqual([
      'Kannada',
      'English',
      'Telugu',
      'Tamil',
      'Hindi',
    ])
    await user.selectOptions(select, 'hi')

    const ananya = await row(/1 · Ananya/)
    expect(await within(ananya).findByText('Home language saved')).toBeInTheDocument()
    expect(within(ananya).getByText(/1 · Ananya · Hindi/)).toBeInTheDocument()
    expect(within(ananya).getByText('Changed here')).toBeInTheDocument()
    expect(select).toHaveValue('hi')
    const put = calls.find((c) => c.method === 'PUT')
    expect(put?.url).toBe('/api/admin/children/c1/language')
    expect(put?.body).toMatchObject({ language: 'hi', clientRequestId: expect.any(String) as string })
  })

  it('a failed language change says so and keeps the stored language', async () => {
    const user = userEvent.setup()
    stubApi({ languageFails: true })
    renderAt()

    const select = await screen.findByLabelText("Ananya's home language")
    await user.selectOptions(select, 'ta')

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not save the language. Nothing was changed.')
    expect(select).toHaveValue('kn')
    expect(screen.queryByText('Home language saved')).not.toBeInTheDocument()
  })

  it('marks a language that was changed on this page', async () => {
    stubApi()
    renderAt()

    const ravi = await row(/24 · Ravi · Telugu/)
    expect(within(ravi).getByText('Changed here')).toBeInTheDocument()
    expect(within(await row(/15 · Manjunath/)).queryByText('Changed here')).not.toBeInTheDocument()
  })
})
