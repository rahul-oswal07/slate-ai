import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { api, ApiError } from '../../api/client'
import { useAnnounce } from '../../components/useAnnounce'
import { saveCachedMe } from './session'
import { meQueryKey } from './useMe'

/**
 * Log out, for the profile menu (T-109; before that the header's own button). Ends the session, forgets the cached
 * teacher and every query's data, and opens the sign-in screen. The remembered phone stays, so the next sign-in
 * on this phone asks for the PIN only. Offline it stays signed in (the server can't end the session) and says so;
 * `logout` then resolves to false.
 */
export function useLogout() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const announce = useAnnounce()
  const [pending, setPending] = useState(false)

  async function logout(): Promise<boolean> {
    setPending(true)
    try {
      await api<void>('/api/auth/logout', { method: 'POST' })
    } catch (failure) {
      // A 401 means the session was already over, which is what we wanted; anything else keeps her signed in.
      if (!(failure instanceof ApiError && failure.status === 401)) {
        setPending(false)
        announce(t('offline.noNetwork'))
        return false
      }
    }
    saveCachedMe(null)
    queryClient.clear()
    queryClient.setQueryData(meQueryKey, null)
    void navigate('/login', { replace: true })
    return true
  }

  return { logout, pending }
}
