import { useMemo, useSyncExternalStore } from 'react'
import { currentClass, storedChoice, subscribeClass } from './currentClass'
import type { ClassInfo } from './session'
import { useMe } from './useMe'

/** The class the app shows; re-renders when she picks another. */
export function useCurrentClass(): ClassInfo | null {
  const me = useMe().data
  const teacherId = me?.teacherId
  const chosen = useSyncExternalStore(subscribeClass, () => (teacherId ? storedChoice(teacherId) : null))
  return useMemo(() => currentClass(me, chosen), [me, chosen])
}

/** The ID of the class the app shows, or '' (no class yet: queries stay disabled). */
export function useClassId(): string {
  return useCurrentClass()?.id ?? ''
}
