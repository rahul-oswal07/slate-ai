import { useCallback, useState, type ReactNode } from 'react'
import { AnnounceContext } from './useAnnounce'

/** One polite live region for the app: "Attendance saved", "Changes discarded" (A-02). */
export function AnnouncerProvider({ children }: { children: ReactNode }) {
  const [message, setMessage] = useState('')
  const announce = useCallback((next: string) => {
    // Clear first so the same message twice is still read out.
    setMessage('')
    window.setTimeout(() => setMessage(next), 50)
  }, [])

  return (
    <AnnounceContext value={announce}>
      {children}
      <div aria-live="polite" className="sr-only" data-testid="announcer">
        {message}
      </div>
    </AnnounceContext>
  )
}
