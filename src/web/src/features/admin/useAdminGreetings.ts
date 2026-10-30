import { useQuery } from '@tanstack/react-query'
import { ApiError, NetworkError, api } from '../../api/client'

/** One teacher's recorded greeting, as `GET /api/admin/greetings` lists it. */
export interface AdminGreeting {
  clipId: string
  teacherName: string
  seconds: number
  recordedAt: string
}

const KEY_HEADER = 'X-Admin-Key'

/** The key after `#key=` in the page's link. It stays in the fragment, so it never reaches a server log. */
export function adminKey(hash: string): string | null {
  return new URLSearchParams(hash.replace(/^#/, '')).get('key') || null
}

/** Every greeting; a missing or wrong key is a 404 (T-66). */
export function useAdminGreetings(key: string | null) {
  return useQuery({
    queryKey: ['admin', 'greetings', key],
    queryFn: () => api<AdminGreeting[]>('/api/admin/greetings', { headers: { [KEY_HEADER]: key ?? '' } }),
    enabled: key !== null,
    retry: false,
  })
}

/** One greeting's audio as a blob: an `<audio src>` can't send the key header. */
export async function fetchGreetingAudio(key: string, clipId: string): Promise<Blob> {
  let response: Response
  try {
    response = await fetch(`/api/admin/greetings/${clipId}/audio`, { headers: { [KEY_HEADER]: key } })
  } catch {
    throw new NetworkError()
  }
  if (!response.ok) {
    throw new ApiError(response.status, null)
  }
  return response.blob()
}
