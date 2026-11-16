import { QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { createQueryClient } from '../../app/queryClient'
import { initI18n } from '../../i18n'
import { fakeApi } from '../../test/renderApp'
import { AdminDemoScreen } from './AdminDemoScreen'
import type { AdminDemo } from './useAdmin'

const i18n = initI18n()

function stubApi(initial: AdminDemo, { fails = false } = {}) {
  let state = initial
  return fakeApi({
    'GET /api/admin/demo': () => ({ status: 200, body: state }),
    'PUT /api/admin/demo/school-date': ({ body }) => {
      if (fails) {
        return { status: 500 }
      }
      state = { ...state, schoolDate: (body as { on: boolean }).on, updatedAt: '2026-11-16T05:00:00Z' }
      return { status: 200, body: state }
    },
  })
}

function renderPage() {
  render(
    <QueryClientProvider client={createQueryClient()}>
      <MemoryRouter initialEntries={['/admin/demo']}>
        <AdminDemoScreen />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('AdminDemoScreen (T-134, D-120)', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('starts off and switches going back to a past day on and off, saving each change', async () => {
    const user = userEvent.setup()
    const calls = stubApi({ demoEnabled: true, schoolDate: false, updatedAt: null })
    renderPage()

    const toggle = await screen.findByRole('switch', { name: 'Go back to a past day' })
    expect(toggle).toHaveAttribute('aria-checked', 'false')
    expect(screen.getByText('Never switched on this server.')).toBeInTheDocument()

    await user.click(toggle)
    expect(await screen.findByText('Saved')).toBeInTheDocument()
    expect(toggle).toHaveAttribute('aria-checked', 'true')
    expect(screen.getByText(/^Last changed /)).toBeInTheDocument()

    await user.click(toggle)
    await screen.findByText('Saved')
    expect(toggle).toHaveAttribute('aria-checked', 'false')

    const puts = calls.filter((c) => c.key === 'PUT /api/admin/demo/school-date')
    expect(puts.map((c) => (c.body as { on: boolean }).on)).toEqual([true, false])
    expect(puts.every((c) => typeof (c.body as { clientRequestId: string }).clientRequestId === 'string')).toBe(true)
  })

  it('says so when saving fails, and keeps the switch as it was', async () => {
    const user = userEvent.setup()
    stubApi({ demoEnabled: true, schoolDate: false, updatedAt: null }, { fails: true })
    renderPage()

    const toggle = await screen.findByRole('switch', { name: 'Go back to a past day' })
    await user.click(toggle)

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not save it. Try again.')
    expect(toggle).toHaveAttribute('aria-checked', 'false')
  })

  it('cannot be switched when the demo is off on this server', async () => {
    stubApi({ demoEnabled: false, schoolDate: false, updatedAt: null })
    renderPage()

    expect(await screen.findByRole('switch', { name: 'Go back to a past day' })).toBeDisabled()
    expect(screen.getByText('The demo is off on this server, so there is nothing to switch.')).toBeInTheDocument()
  })
})
