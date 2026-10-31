// School dates and times are India Standard Time, whatever the phone's zone (spec §4).
const IST = 'Asia/Kolkata'

// A demo date fixed by the server (Demo:Today, T-75; staging only), read from each API response's X-School-Today.
let fixedToday: string | null = null

/** Use the server's fixed school date, or the phone's clock again with null. */
export function setSchoolToday(date: string | null) {
  fixedToday = date
}

/** Today's date in IST as yyyy-mm-dd: the server's fixed demo date if it has one, else the phone's clock. */
export function istToday(now?: Date) {
  if (!now && fixedToday) {
    return fixedToday
  }
  return new Intl.DateTimeFormat('en-CA', { timeZone: IST }).format(now ?? new Date())
}

/** "Wednesday, 21 October" in the UI language. */
export function formatSchoolDate(date: string, language: string) {
  return new Intl.DateTimeFormat(language, { timeZone: IST, weekday: 'long', day: 'numeric', month: 'long' })
    .format(new Date(`${date}T12:00:00+05:30`))
}

/** A clock time in the UI language, from an ISO instant or an "HH:mm:ss" IST time on `date`. */
export function formatSchoolTime(value: string, language: string, date?: string) {
  const instant = value.includes('T') ? new Date(value) : new Date(`${date}T${value}+05:30`)
  return new Intl.DateTimeFormat(language, { timeZone: IST, hour: 'numeric', minute: '2-digit' }).format(instant)
}
