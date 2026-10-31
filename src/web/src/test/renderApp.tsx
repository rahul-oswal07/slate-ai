import { QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { App } from '../app/App'
import { createQueryClient } from '../app/queryClient'

export type FakeRoute = (request: { method: string; body: unknown }) => { status: number; body?: unknown; headers?: Record<string, string> } | 'offline'

/** Replaces fetch with routes keyed by "METHOD /path". Unknown routes are 404. Returns the calls made. */
export function fakeApi(routes: Record<string, FakeRoute>) {
  const calls: { key: string; body: unknown }[] = []
  vi.stubGlobal(
    'fetch',
    vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const method = init?.method ?? 'GET'
      const key = `${method} ${String(input)}`
      const body: unknown = typeof init?.body === 'string' ? JSON.parse(init.body) : undefined
      calls.push({ key, body })
      const result = routes[key]?.({ method, body }) ?? { status: 404 }
      if (result === 'offline') {
        throw new TypeError('Failed to fetch')
      }
      return new Response(result.body === undefined ? null : JSON.stringify(result.body), {
        status: result.status,
        headers: { 'Content-Type': 'application/json', ...result.headers },
      })
    }),
  )
  return calls
}

export function renderApp(path: string) {
  return render(
    <QueryClientProvider client={createQueryClient()}>
      <MemoryRouter initialEntries={[path]}>
        <App />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

export const teacher = {
  teacherId: '6abf3cc9-9aa8-5786-a833-62325a6418f4',
  displayName: 'Shobha',
  classId: '0a1e03b3-23fa-5a7d-87e3-490d6ddae335',
  setupComplete: true,
}
