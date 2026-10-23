import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/client'
import type { Me } from '../auth/session'
import { istToday } from '../today/schoolTime'
import type { AiCall, DemoInfo, DemoReset, FamilyMessage } from './types'

/** The parent phone and "Under the hood" refresh this often while the demo is open. */
export const DEMO_POLL_MS = 5000

const RESET_AT_KEY = 'slate.demo.resetAt'

/** When this browser last reset the demo; the AI panel lists calls from then. Per viewer, so browser storage. */
export function storedResetAt(): string | null {
  try {
    return localStorage.getItem(RESET_AT_KEY)
  } catch {
    return null
  }
}

function storeResetAt(value: string) {
  try {
    localStorage.setItem(RESET_AT_KEY, value)
  } catch {
    // Private mode: the panel then shows the latest calls instead.
  }
}

/** One-tap sign-in as the demo teacher, then who the story follows. */
export function useDemoInfo() {
  return useQuery({
    queryKey: ['demo', 'info'],
    queryFn: async () => {
      await api<Me>('/api/demo/login', { method: 'POST' })
      return api<DemoInfo>('/api/demo')
    },
    staleTime: Infinity,
    retry: false,
  })
}

export function useFamily(childId: string) {
  return useQuery({
    queryKey: ['demo', 'family', childId],
    queryFn: () => api<FamilyMessage[]>(`/api/demo/family/${childId}`),
    enabled: childId !== '',
    refetchInterval: DEMO_POLL_MS,
  })
}

export function useAiCalls(since: string | null) {
  return useQuery({
    queryKey: ['demo', 'ai-calls', since],
    queryFn: () => api<AiCall[]>(since ? `/api/demo/ai-calls?since=${encodeURIComponent(since)}` : '/api/demo/ai-calls'),
    refetchInterval: DEMO_POLL_MS,
  })
}

export function useResetDemo() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () =>
      api<DemoReset>('/api/demo/reset', { method: 'POST', body: { clientRequestId: crypto.randomUUID() } }),
    onSuccess: (reset) => {
      storeResetAt(reset.resetAt)
      void queryClient.invalidateQueries({ queryKey: ['demo', 'family'] })
    },
  })
}

/** "Bring in today's attendance": the simulated SATS register arrives now (D-45). */
export function useBringAttendance(classId: string) {
  return useMutation({
    mutationFn: () =>
      api<unknown>('/api/demo/sats-feed', { method: 'POST', body: { classId, date: istToday(), mode: 'arrive' } }),
  })
}
