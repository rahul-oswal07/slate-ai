import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { fetchAdminClip } from './useAdmin'

/**
 * Download → fetch the clip → save it as `fileName` (T-68). The bytes go through an object URL that is freed straight
 * after, so a failed fetch shows an error instead of a broken download.
 */
export function DownloadClip({
  url,
  fileName,
  label,
}: {
  url: string
  fileName: string
  label: string
}) {
  const { t } = useTranslation()
  const [state, setState] = useState<'idle' | 'loading' | 'failed'>('idle')

  async function download() {
    setState('loading')
    try {
      const href = URL.createObjectURL(await fetchAdminClip(url))
      const link = document.createElement('a')
      link.href = href
      link.download = fileName
      link.click()
      URL.revokeObjectURL(href)
      setState('idle')
    } catch {
      setState('failed')
    }
  }

  return (
    <>
      <button
        type="button"
        onClick={() => void download()}
        disabled={state === 'loading'}
        aria-label={label}
        className="mt-2 min-h-target w-full rounded-button border-2 border-primary bg-surface font-extrabold text-label text-primary-strong"
      >
        {t(state === 'loading' ? 'admin.loadingAudio' : 'admin.download')}
      </button>
      {state === 'failed' && (
        <p role="alert" className="mt-2 text-caption text-error">
          {t('admin.downloadFailed')}
        </p>
      )}
    </>
  )
}
