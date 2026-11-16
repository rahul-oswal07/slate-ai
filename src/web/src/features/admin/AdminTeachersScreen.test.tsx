import { QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { createQueryClient } from '../../app/queryClient'
import { initI18n } from '../../i18n'
import { AdminTeachersScreen } from './AdminTeachersScreen'
import type { AdminTeacher } from './useAdmin'

const i18n = initI18n()

const teachers: AdminTeacher[] = [
  { id: 't-monika', name: 'Monika', classLabel: 'Shobha · Grade 3', consentAt: null, greetingSeconds: null },
  { id: 't-shobha', name: 'Shobha', classLabel: 'Shobha · Grade 3', consentAt: '2026-11-15T04:30:00Z', greetingSeconds: 9.6 },
]

/** The admin API (no key, D-112): GET lists the teachers; POST …/clear-consent clears one, or fails with 500. */
function stubApi({ clearFails = false }: { clearFails?: boolean } = {}) {
  const rows = new Map(teachers.map((t) => [t.id, t]))
  const calls: { method: string; url: string; body: { clientRequestId?: string } | null }[] = []
  vi.stubGlobal(
    'fetch',
    vi.fn<typeof globalThis.fetch>(async (input, init) => {
      const method = init?.method ?? 'GET'
      const body = init?.body ? (JSON.parse(String(init.body)) as { clientRequestId?: string }) : null
      calls.push({ method, url: String(input), body })
      if (method === 'POST') {
        const id = String(input).split('/').at(-2) ?? ''
        const current = rows.get(id)
        if (clearFails || !current) {
          return new Response(null, { status: clearFails ? 500 : 404 })
        }
        const cleared: AdminTeacher = { ...current, consentAt: null, greetingSeconds: null }
        rows.set(id, cleared)
        return Response.json(cleared)
      }
      return Response.json([...rows.values()])
    }),
  )
  return { calls }
}

function renderAt() {
  render(
    <QueryClientProvider client={createQueryClient()}>
      <MemoryRouter initialEntries={['/admin/teachers']}>
        <AdminTeachersScreen />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

const card = async (name: string) =>
  (await screen.findByRole('heading', { level: 2, name })).closest('li') as HTMLElement

describe('AdminTeachersScreen (T-133)', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('lists each teacher with her class, consent and greeting', async () => {
    stubApi()
    renderAt()

    const shobha = await card('Shobha')
    expect(within(shobha).getByText('Shobha · Grade 3')).toBeInTheDocument()
    expect(within(shobha).getByText(/Voice consent given/)).toBeInTheDocument()
    expect(within(shobha).getByText('Greeting: 9.6 s')).toBeInTheDocument()
    expect(within(shobha).getByRole('button', { name: 'Clear consent for Shobha' })).toBeInTheDocument()

    const monika = await card('Monika')
    expect(
      within(monika).getByText('No voice consent: at her next visit she sees the Welcome and records her greeting.'),
    ).toBeInTheDocument()
    expect(within(monika).queryByRole('button', { name: /Clear consent/ })).not.toBeInTheDocument()
  })

  it('asks first, then clears her consent and greeting', async () => {
    const user = userEvent.setup()
    const { calls } = stubApi()
    renderAt()

    const shobha = await card('Shobha')
    await user.click(within(shobha).getByRole('button', { name: 'Clear consent for Shobha' }))
    expect(
      within(shobha).getByText("Clear Shobha's voice consent? Her greeting is deleted, and at her next visit she records it again."),
    ).toBeInTheDocument()
    expect(calls.filter((c) => c.method === 'POST')).toHaveLength(0)

    await user.click(within(shobha).getByRole('button', { name: 'Yes, clear it' }))

    expect(await within(shobha).findByText('Cleared')).toBeInTheDocument()
    expect(
      within(shobha).getByText('No voice consent: at her next visit she sees the Welcome and records her greeting.'),
    ).toBeInTheDocument()
    expect(within(shobha).queryByText(/Greeting:/)).not.toBeInTheDocument()
    const post = calls.find((c) => c.method === 'POST')
    expect(post?.url).toBe('/api/admin/teachers/t-shobha/clear-consent')
    expect(post?.body?.clientRequestId).toEqual(expect.any(String))
  })

  it('Cancel sends nothing and keeps her consent', async () => {
    const user = userEvent.setup()
    const { calls } = stubApi()
    renderAt()

    const shobha = await card('Shobha')
    await user.click(within(shobha).getByRole('button', { name: 'Clear consent for Shobha' }))
    await user.click(within(shobha).getByRole('button', { name: 'Cancel' }))

    expect(within(shobha).queryByText(/Her greeting is deleted/)).not.toBeInTheDocument()
    expect(within(shobha).getByText(/Voice consent given/)).toBeInTheDocument()
    expect(calls.filter((c) => c.method === 'POST')).toHaveLength(0)
  })

  it('says so when clearing fails, and keeps her consent', async () => {
    const user = userEvent.setup()
    stubApi({ clearFails: true })
    renderAt()

    const shobha = await card('Shobha')
    await user.click(within(shobha).getByRole('button', { name: 'Clear consent for Shobha' }))
    await user.click(within(shobha).getByRole('button', { name: 'Yes, clear it' }))

    expect(await within(shobha).findByRole('alert')).toHaveTextContent('Could not clear it. Try again.')
    expect(within(shobha).getByText(/Voice consent given/)).toBeInTheDocument()
  })
})
