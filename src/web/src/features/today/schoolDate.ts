import { useSyncExternalStore } from 'react'
import { currentClass, subscribeClass } from '../auth/currentClass'
import { loadCachedMe } from '../auth/session'
import { istToday } from './schoolTime'

/**
 * The school day the demo teacher chose to act as (T-134, D-120), or none: the real day. Kept in this browser only
 * (localStorage, so /demo's teacher phone shares it) and sent on every API call as {@link SCHOOL_DATE_HEADER}; the
 * server applies it only for the demo class. Always a day before the real IST today: choosing today or later clears it.
 * It counts only while the admin switch is on for the class she has open (T-115: the demo class's `demo.schoolDate`,
 * read from the cached /api/me so `api()` knows synchronously); `TeacherLayout` clears it when /api/me says the switch
 * is off.
 */
const KEY = 'slate.schoolDate'

/** The request header the server reads (`SchoolDate.Header`). */
export const SCHOOL_DATE_HEADER = 'X-School-Date'

const listeners = new Set<() => void>()

function isPastDay(value: string | null): value is string {
  return value !== null && /^\d{4}-\d{2}-\d{2}$/.test(value) && value < istToday()
}

/** The admin switch, as the last /api/me said, for the class she has open: only the demo class has it (T-115). */
function switchedOn() {
  return currentClass(loadCachedMe())?.demo?.schoolDate === true
}

/** The chosen past day, or null for the real day (always null while the admin switch is off). */
export function chosenSchoolDate(): string | null {
  if (!switchedOn()) {
    return null
  }
  const stored = localStorage.getItem(KEY)
  return isPastDay(stored) ? stored : null
}

/** The day the app acts as today: the chosen one, else the real IST today. */
export function schoolToday(): string {
  return chosenSchoolDate() ?? istToday()
}

/** Acts as `date` from now on; null, today or a later day goes back to the real day. */
export function setSchoolDate(date: string | null) {
  if (isPastDay(date)) {
    localStorage.setItem(KEY, date)
  } else {
    localStorage.removeItem(KEY)
  }
  listeners.forEach((listener) => listener())
}

/** Re-reads the chosen day after /api/me changed the switch. */
export function refreshSchoolDate() {
  listeners.forEach((listener) => listener())
}

/** Moves the chosen day by `days` (−1/+1 on Today, −7/+7 on the week screens). */
export function stepSchoolDate(days: number) {
  const date = new Date(`${schoolToday()}T12:00:00+05:30`)
  date.setUTCDate(date.getUTCDate() + days)
  setSchoolDate(istToday(date))
}

function subscribe(listener: () => void) {
  listeners.add(listener)
  // Another page of this browser (the /demo frame and the app) changed it.
  const onStorage = (event: StorageEvent) => {
    if (event.key === KEY || event.key === null) {
      listener()
    }
  }
  window.addEventListener('storage', onStorage)
  // Another class open (T-115): the chosen day counts only in the demo class.
  const unsubscribeClass = subscribeClass(listener)
  return () => {
    listeners.delete(listener)
    window.removeEventListener('storage', onStorage)
    unsubscribeClass()
  }
}

/** The chosen past day, or null; re-renders when it changes. */
export function useSchoolDate(): string | null {
  return useSyncExternalStore(subscribe, chosenSchoolDate)
}

/** The day the app acts as today; re-renders when it changes. */
export function useSchoolToday(): string {
  return useSchoolDate() ?? istToday()
}
