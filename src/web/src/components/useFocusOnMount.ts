import { useEffect, useRef } from 'react'

/** Moves focus to the screen heading when a screen opens, so it never drops to <body> (A-04). */
export function useFocusOnMount<T extends HTMLElement>() {
  const ref = useRef<T>(null)
  useEffect(() => {
    ref.current?.focus()
  }, [])
  return ref
}
