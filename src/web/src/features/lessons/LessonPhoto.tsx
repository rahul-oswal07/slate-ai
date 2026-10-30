import { useEffect, useMemo, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useAnnounce } from '../../components/useAnnounce'
import { resizePhoto } from './resizePhoto'
import { setPhoto, usePhoto } from './useLessons'

/**
 * One photo of today's lesson (T-64): the textbook page or the board, never the children. It stays on the phone
 * and goes with her voice note, or alone through "Match from photo" when she can't speak.
 */
export function LessonPhoto({ date, onMatch, disabled }: { date: string; onMatch: () => void; disabled?: boolean }) {
  const { t } = useTranslation()
  const announce = useAnnounce()
  const photo = usePhoto(date)
  const input = useRef<HTMLInputElement>(null)
  const [busy, setBusy] = useState(false)
  const src = useMemo(() => (photo ? URL.createObjectURL(new Blob([photo.data], { type: photo.type })) : null), [photo])
  useEffect(() => () => {
    if (src) {
      URL.revokeObjectURL(src)
    }
  }, [src])

  async function choose(file: File | undefined) {
    if (!file) {
      return
    }
    setBusy(true)
    try {
      await setPhoto(date, await resizePhoto(file))
      announce(t('today.lesson.photo.added'))
    } finally {
      setBusy(false)
      if (input.current) {
        input.current.value = ''
      }
    }
  }

  async function remove() {
    await setPhoto(date, null)
    announce(t('today.lesson.photo.removed'))
  }

  const picker = (
    <input
      ref={input}
      type="file"
      accept="image/jpeg,image/png,image/webp"
      capture="environment"
      className="sr-only"
      tabIndex={-1}
      aria-hidden="true"
      data-testid="photo-input"
      onChange={(event) => void choose(event.target.files?.[0])}
    />
  )

  if (!src) {
    return (
      <div className="mt-1 text-center">
        {picker}
        <button type="button" onClick={() => input.current?.click()} disabled={busy} className="btn-link">
          {t('today.lesson.photo.add')}
        </button>
        <p className="text-caption text-ink-muted">{t('today.lesson.photo.hint')}</p>
      </div>
    )
  }

  return (
    <div className="mt-3 rounded-card border-[1.5px] border-edge bg-surface p-3 text-left">
      {picker}
      <div className="flex items-start gap-3">
        <img src={src} alt={t('today.lesson.photo.alt')} className="size-20 shrink-0 rounded-button object-cover" />
        <div className="min-w-0">
          <p className="text-body">{t('today.lesson.photo.withNote')}</p>
          <p className="mt-1 text-caption text-ink-muted">{t('today.lesson.photo.hint')}</p>
        </div>
      </div>
      <button
        type="button"
        onClick={onMatch}
        disabled={disabled || busy}
        className="mt-3 min-h-target-primary w-full rounded-button bg-primary font-extrabold text-label text-on-primary"
      >
        {t('today.lesson.photo.match')}
      </button>
      <div className="mt-1 flex justify-between gap-2">
        <button type="button" onClick={() => input.current?.click()} disabled={busy} className="btn-link">
          {t('today.lesson.photo.replace')}
        </button>
        <button type="button" onClick={() => void remove()} disabled={busy} className="btn-link">
          {t('today.lesson.photo.remove')}
        </button>
      </div>
    </div>
  )
}
