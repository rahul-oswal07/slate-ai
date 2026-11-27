import type { ClassInfo, Me } from './session'

/**
 * Which of her classes the app shows (T-115). She picks it in the dropdown on Today; the choice is kept in this browser
 * (localStorage, one key per teacher, so a shared phone keeps each teacher's own) and every screen, the header and the
 * bottom nav follow it. A class she is no longer in, or no choice yet, falls back to her default (`me.classId`: the
 * class she is the main teacher of first).
 */
const keyFor = (teacherId: string) => `slate.class.${teacherId}`

const listeners = new Set<() => void>()

/** Her classes, default first. A `me` cached before T-115 has only the one class in its top-level fields. */
export function classesOf(me: Me | null | undefined): ClassInfo[] {
  if (!me) {
    return []
  }
  if (me.classes) {
    return me.classes
  }
  return me.classId
    ? [
        {
          id: me.classId,
          name: '',
          grade: me.grade ?? null,
          medium: me.medium ?? null,
          classTeacherName: me.classTeacherName ?? null,
          demo: me.demo ?? null,
        },
      ]
    : []
}

export function storedChoice(teacherId: string): string | null {
  try {
    return localStorage.getItem(keyFor(teacherId))
  } catch {
    return null
  }
}

/** The class the app shows: her choice if she is still in it, else her default; null without a class. */
export function currentClass(
  me: Me | null | undefined,
  chosen: string | null = me ? storedChoice(me.teacherId) : null,
): ClassInfo | null {
  const classes = classesOf(me)
  return (
    classes.find((c) => c.id === chosen) ?? classes.find((c) => c.id === me?.classId) ?? classes[0] ?? null
  )
}

/** Shows `classId` from now on, on every screen. */
export function selectClass(teacherId: string, classId: string) {
  try {
    localStorage.setItem(keyFor(teacherId), classId)
  } catch {
    // Storage unavailable (private mode): the app still works and shows her default class.
  }
  listeners.forEach((listener) => listener())
}

/** Calls `listener` when the chosen class changes, here or in another tab of this browser. */
export function subscribeClass(listener: () => void) {
  listeners.add(listener)
  const onStorage = (event: StorageEvent) => {
    if (event.key === null || event.key.startsWith('slate.class.')) {
      listener()
    }
  }
  window.addEventListener('storage', onStorage)
  return () => {
    listeners.delete(listener)
    window.removeEventListener('storage', onStorage)
  }
}
