import { useEffect, useState } from 'react'

/** The time now, ticking every `everyMs`, so a countdown or a "goes at" line moves on by itself. */
export function useNow(everyMs = 30_000) {
  const [now, setNow] = useState(() => Date.now())
  useEffect(() => {
    const id = window.setInterval(() => setNow(Date.now()), everyMs)
    return () => window.clearInterval(id)
  }, [everyMs])
  return now
}
