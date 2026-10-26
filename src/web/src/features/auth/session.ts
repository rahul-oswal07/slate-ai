/** The signed-in teacher, as returned by GET /api/me. */
export type Me = {
  teacherId: string
  displayName: string
  classId: string | null
  setupComplete: boolean
  /** Her recorded greeting (T0 step 1), or null. Missing in a `me` cached before greetings existed. */
  greeting?: Greeting | null
}

/** Where to play her greeting (`/api/audio/{id}`) and how long it is. */
export type Greeting = { url: string; seconds: number }

/** Who last signed in on this phone, so the next sign-in asks for the PIN only. */
export type RememberedTeacher = { phone: string; displayName: string }

const REMEMBERED_KEY = 'slate.teacher'
const CACHED_ME_KEY = 'slate.me'

function read<T>(key: string, isValid: (value: unknown) => value is T): T | null {
  try {
    const raw = localStorage.getItem(key)
    if (raw === null) {
      return null
    }
    const value: unknown = JSON.parse(raw)
    return isValid(value) ? value : null
  } catch {
    return null
  }
}

function write(key: string, value: unknown) {
  try {
    if (value === null) {
      localStorage.removeItem(key)
    } else {
      localStorage.setItem(key, JSON.stringify(value))
    }
  } catch {
    // Storage unavailable (private mode): the app still works, it just won't remember.
  }
}

function isObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function isRemembered(value: unknown): value is RememberedTeacher {
  return isObject(value) && typeof value.phone === 'string' && typeof value.displayName === 'string'
}

function isMe(value: unknown): value is Me {
  return (
    isObject(value) &&
    typeof value.teacherId === 'string' &&
    typeof value.displayName === 'string' &&
    (typeof value.classId === 'string' || value.classId === null) &&
    typeof value.setupComplete === 'boolean'
  )
}

export const loadRemembered = () => read(REMEMBERED_KEY, isRemembered)
export const saveRemembered = (teacher: RememberedTeacher | null) => write(REMEMBERED_KEY, teacher)

/** Last known /api/me, so the app opens offline (NFR-02). */
export const loadCachedMe = () => read(CACHED_ME_KEY, isMe)
export const saveCachedMe = (me: Me | null) => write(CACHED_ME_KEY, me)

/** Digits of a 10-digit Indian mobile number, or null. Mirrors the API's PhoneNumber.Normalise. */
export function normalisePhone(input: string): string | null {
  let digits = input.replace(/\D/g, '')
  if (digits.length === 12 && digits.startsWith('91')) {
    digits = digits.slice(2)
  } else if (digits.length === 11 && digits.startsWith('0')) {
    digits = digits.slice(1)
  }
  return digits.length === 10 ? digits : null
}

export const isPin = (pin: string) => /^\d{4,6}$/.test(pin)
