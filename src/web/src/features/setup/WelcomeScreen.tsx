import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { api, NetworkError } from '../../api/client'
import { Icon, type IconName } from '../../components/Icon'
import { useFocusOnMount } from '../../components/useFocusOnMount'
import { saveCachedMe, type Me } from '../auth/session'
import { meQueryKey, useMe } from '../auth/useMe'

const promises: { icon: IconName; key: string }[] = [
  { icon: 'clock', key: 'setup.welcome.sats' },
  { icon: 'checkCircle', key: 'setup.welcome.approval' },
  { icon: 'lock', key: 'setup.welcome.privacy' },
]

/**
 * T0 Set up, Welcome (state `0`, docs/design/T0-setup.md). Shown once, until Teacher.ConsentAt is
 * set. Start finishes setup and opens the evening-message step (T-44); the greeting step comes later.
 */
export function WelcomeScreen() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const me = useMe()
  const headingRef = useFocusOnMount<HTMLHeadingElement>()
  // One ID per screen visit, so a retried Start is the same request.
  const clientRequestId = useRef(crypto.randomUUID())

  const start = useMutation({
    mutationFn: () =>
      api<Me>('/api/me/welcome', { method: 'POST', body: { clientRequestId: clientRequestId.current } }),
    onSuccess: (updated) => {
      saveCachedMe(updated)
      // Navigate first: once `me` says setup is complete, this route's gate sends her to Today, and that redirect
      // must not win over the evening-message step.
      void navigate('/setup/evening', { replace: true })
      queryClient.setQueryData(meQueryKey, updated)
    },
  })

  return (
    <section aria-labelledby="welcome-title" className="mx-auto max-w-md">
      <div className="mb-2 mt-4 flex justify-center text-primary-strong">
        <Icon name="book" size={44} strokeWidth={1.6} />
      </div>
      <h1 id="welcome-title" ref={headingRef} tabIndex={-1} className="text-center text-title">
        {t('setup.welcome.heading', { teacher: me.data?.displayName ?? '' })}
      </h1>
      <p className="mb-5 mt-1 text-center text-body text-ink-muted">{t('setup.welcome.lead')}</p>

      <ul className="flex flex-col gap-3">
        {promises.map(({ icon, key }) => (
          <li key={key} className="flex items-start gap-3 rounded-card border-[1.5px] border-edge bg-surface p-[14px]">
            <Icon name={icon} className="mt-0.5 shrink-0 text-primary-strong" />
            <span className="text-body">{t(key)}</span>
          </li>
        ))}
      </ul>

      <div className="sticky bottom-0 mt-4 border-t border-divider bg-canvas pb-3 pt-3">
        {start.isError && (
          <p role="alert" className="mb-2 flex items-start gap-2 text-caption text-error">
            <Icon name="alert" size={18} className="mt-0.5 shrink-0" />
            {start.error instanceof NetworkError ? t('setup.welcome.offline') : t('auth.error.generic')}
          </p>
        )}
        <button
          type="button"
          onClick={() => start.mutate()}
          disabled={start.isPending}
          className="min-h-target-primary w-full rounded-button bg-primary font-extrabold text-label text-on-primary disabled:opacity-70"
        >
          {t('setup.welcome.start')}
        </button>
      </div>
    </section>
  )
}
