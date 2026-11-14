import { useCallback, useEffect, useRef, useState, type ReactNode } from 'react'
import { AnnounceContext } from './useAnnounce'

/** One polite live region for the app: "Attendance saved", "Changes discarded" (A-02). */
export function AnnouncerProvider({ children }: { children: ReactNode }) {
  const [message, setMessage] = useState('')
  const timer = useRef<number | undefined>(undefined)
  const announce = useCallback((next: string) => {
    // Clear first so the same message twice is still read out.
    setMessage('')
    window.clearTimeout(timer.current)
    timer.current = window.setTimeout(() => setMessage(next), 50)
  }, [])

  // Nothing is left to fire once the app is gone (in tests it fired after the page was torn down).
  useEffect(() => () => window.clearTimeout(timer.current), [])

  return (
    <AnnounceContext value={announce}>
      {children}
      <div aria-live="polite" className="sr-only" data-testid="announcer">
        {message}
      </div>
    </AnnounceContext>
  )
}
