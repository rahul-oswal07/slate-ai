import { useQuery } from '@tanstack/react-query'
import { api, ApiError, NetworkError } from '../../api/client'
import { loadCachedMe, saveCachedMe, type Me } from './session'

export const meQueryKey = ['me'] as const

/** The signed-in teacher, null when signed out. Offline it falls back to the last known teacher. */
export function useMe() {
  return useQuery<Me | null>({
    queryKey: meQueryKey,
    queryFn: async () => {
      try {
        const me = await api<Me>('/api/me')
        saveCachedMe(me)
        return me
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
          saveCachedMe(null)
          return null
        }
        const cached = error instanceof NetworkError ? loadCachedMe() : null
        if (cached) {
          return cached
        }
        throw error
      }
    },
    // Show the cached teacher at once, then check with the server.
    initialData: () => loadCachedMe() ?? undefined,
    initialDataUpdatedAt: 0,
    retry: false,
    staleTime: 5 * 60_000,
  })
}
