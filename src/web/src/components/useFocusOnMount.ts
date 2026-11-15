import { useEffect, useRef } from 'react'

/**
 * Moves focus to the screen heading when a screen opens, so it never drops to <body> (A-04). `enabled` false leaves
 * focus to something else on the screen (e.g. Today's mic after Re-record, T-114).
 */
export function useFocusOnMount<T extends HTMLElement>(enabled = true) {
  const ref = useRef<T>(null)
  useEffect(() => {
    if (enabled) {
      ref.current?.focus()
    }
    // Only on mount: a later change of `enabled` must not move focus.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])
  return ref
}
