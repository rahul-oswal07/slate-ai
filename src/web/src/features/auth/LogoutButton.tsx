import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocation, useNavigate } from 'react-router'
import { api, ApiError } from '../../api/client'
import { Icon } from '../../components/Icon'
import { useAnnounce } from '../../components/useAnnounce'
import { saveCachedMe } from './session'
import { meQueryKey } from './useMe'

/**
 * Small "Log out" button in the app header, shown on every screen except sign-in. Ends the session, forgets the cached
 * teacher and every query's data, and opens the sign-in screen. The remembered phone stays, so the next sign-in
 * on this phone asks for the PIN only. Offline it stays signed in (the server can't end the session) and says so.
 */
export function LogoutButton() {
  const { t } = useTranslation()
  const { pathname } = useLocation()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const announce = useAnnounce()

  const [pending, setPending] = useState(false)

  async function logout() {
    setPending(true)
    try {
      await api<void>('/api/auth/logout', { method: 'POST' })
    } catch (failure) {
      // A 401 means the session was already over, which is what we wanted; anything else keeps her signed in.
      if (!(failure instanceof ApiError && failure.status === 401)) {
        setPending(false)
        announce(t('offline.noNetwork'))
        return
      }
    }
    saveCachedMe(null)
    queryClient.clear()
    queryClient.setQueryData(meQueryKey, null)
    void navigate('/login', { replace: true })
  }

  // The sign-in screen is the only one a signed-out teacher can see. Not reading `me` here on purpose: the welcome
  // screen's hand-off to the evening step is timing-sensitive to who else watches that query.
  if (pathname === '/login') {
    return null
  }

  return (
    <button
      type="button"
      onClick={() => void logout()}
      disabled={pending}
      className="btn-link -mr-2 shrink-0 px-2 text-caption no-underline"
    >
      <Icon name="logOut" size={18} />
      {t('auth.logout')}
    </button>
  )
}
