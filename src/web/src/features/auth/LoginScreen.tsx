import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useRef, useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Navigate, useNavigate } from 'react-router'
import { api, ApiError, NetworkError } from '../../api/client'
import { Icon } from '../../components/Icon'
import { useFocusOnMount } from '../../components/useFocusOnMount'
import { useOnline } from '../../components/useOnline'
import { isPin, loadRemembered, normalisePhone, saveCachedMe, saveRemembered, type Me } from './session'
import { meQueryKey, useMe } from './useMe'

type LockedBody = { lockedUntil: string }

type LoginError = { field: 'phone' | 'pin'; message: string }

function isLockedBody(body: unknown): body is LockedBody {
  return typeof body === 'object' && body !== null && typeof (body as LockedBody).lockedUntil === 'string'
}

/**
 * Teacher sign-in (T-47, D-40). First time on a phone: mobile number + PIN. After that the phone is
 * remembered and only the PIN is asked, with "Not you? Change phone". No registration.
 */
export function LoginScreen() {
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const online = useOnline()
  const me = useMe()
  const headingRef = useFocusOnMount<HTMLHeadingElement>()
  const pinRef = useRef<HTMLInputElement>(null)

  const [remembered, setRemembered] = useState(loadRemembered)
  const [phone, setPhone] = useState('')
  const [pin, setPin] = useState('')
  const [error, setError] = useState<LoginError | null>(null)

  const signIn = useMutation({
    mutationFn: (body: { phone: string; pin: string }) =>
      api<Me>('/api/auth/pin', { method: 'POST', body }),
    onSuccess: (signedIn, body) => {
      saveRemembered({ phone: body.phone, displayName: signedIn.displayName })
      saveCachedMe(signedIn)
      queryClient.setQueryData(meQueryKey, signedIn)
      void navigate(signedIn.setupComplete ? '/' : '/welcome', { replace: true })
    },
    onError: (failure) => {
      setPin('')
      setError({ field: 'pin', message: messageFor(failure) })
      pinRef.current?.focus()
    },
  })

  function messageFor(failure: Error) {
    if (failure instanceof NetworkError) {
      return t('auth.error.offline')
    }
    if (failure instanceof ApiError && failure.status === 429 && isLockedBody(failure.body)) {
      const time = new Intl.DateTimeFormat(i18n.language, { hour: 'numeric', minute: '2-digit' })
        .format(new Date(failure.body.lockedUntil))
      return t('auth.error.locked', { time })
    }
    if (failure instanceof ApiError && (failure.status === 401 || failure.status === 400)) {
      return t('auth.error.wrong')
    }
    return t('auth.error.generic')
  }

  function submit(event: FormEvent) {
    event.preventDefault()
    const number = remembered?.phone ?? normalisePhone(phone)
    if (number === null) {
      setError({ field: 'phone', message: t('auth.error.phone') })
      return
    }
    if (!isPin(pin)) {
      setError({ field: 'pin', message: t('auth.error.pin') })
      pinRef.current?.focus()
      return
    }
    setError(null)
    signIn.mutate({ phone: number, pin })
  }

  function changePhone() {
    saveRemembered(null)
    setRemembered(null)
    setPin('')
    setError(null)
    headingRef.current?.focus()
  }

  if (me.data && !signIn.isPending && !signIn.isSuccess) {
    return <Navigate to={me.data.setupComplete ? '/' : '/welcome'} replace />
  }

  const inputClass =
    'mt-1 block min-h-target w-full rounded-button border-2 border-border bg-surface px-3 text-body-lg text-ink'

  return (
    <section aria-labelledby="login-title" className="mx-auto max-w-md">
      <div className="mb-2 mt-4 flex justify-center text-primary-strong">
        <Icon name="book" size={44} strokeWidth={1.6} />
      </div>
      <h1 id="login-title" ref={headingRef} tabIndex={-1} className="text-center text-title">
        {remembered ? t('auth.returning.heading', { teacher: remembered.displayName }) : t('auth.title')}
      </h1>
      <p className="mb-5 mt-1 text-center text-body text-ink-muted">
        {remembered ? t('auth.returning.lead') : t('auth.lead')}
      </p>

      {!online && (
        <output className="mb-4 flex items-start gap-2 rounded-card border-[1.5px] border-edge border-l-4 border-l-warning bg-surface p-3">
          <Icon name="cloudOff" size={18} className="mt-1 shrink-0 text-warning" />
          <span className="text-body text-ink">{t('auth.error.offline')}</span>
        </output>
      )}

      <form noValidate onSubmit={submit} className="flex flex-col gap-4">
        {!remembered && (
          <label className="block text-label">
            {t('auth.phone')}
            <input
              type="tel"
              inputMode="tel"
              autoComplete="tel-national"
              value={phone}
              onChange={(event) => setPhone(event.target.value)}
              aria-invalid={error?.field === 'phone'}
              aria-errormessage={error?.field === 'phone' ? 'login-error' : undefined}
              className={inputClass}
            />
          </label>
        )}

        <div>
          <label htmlFor="login-pin" className="block text-label">
            {t('auth.pin')}
          </label>
          <input
            id="login-pin"
            ref={pinRef}
            type="password"
            inputMode="numeric"
            pattern="[0-9]*"
            maxLength={6}
            autoComplete="current-password"
            value={pin}
            onChange={(event) => setPin(event.target.value.replace(/\D/g, ''))}
            aria-describedby="pin-hint"
            aria-invalid={error?.field === 'pin'}
            aria-errormessage={error?.field === 'pin' ? 'login-error' : undefined}
            className={`${inputClass} tracking-[0.5em]`}
          />
          <span id="pin-hint" className="mt-1 block text-caption text-ink-muted">
            {t('auth.pinHint')}
          </span>
        </div>

        {error && (
          <p id="login-error" role="alert" className="flex items-start gap-2 text-caption text-error">
            <Icon name="alert" size={18} className="mt-0.5 shrink-0" />
            {error.message}
          </p>
        )}

        <button
          type="submit"
          disabled={signIn.isPending}
          className="min-h-target-primary w-full rounded-button bg-primary font-extrabold text-label text-on-primary disabled:opacity-70"
        >
          {signIn.isPending ? t('auth.submitting') : t('auth.submit')}
        </button>

        {remembered && (
          <button
            type="button"
            onClick={changePhone}
            className="min-h-target w-full text-label text-primary-strong"
          >
            {t('auth.changePhone')}
          </button>
        )}
      </form>
    </section>
  )
}
