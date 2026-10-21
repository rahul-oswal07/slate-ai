// School dates and times are India Standard Time, whatever the phone's zone (spec §4).
const IST = 'Asia/Kolkata'

/** Today's date in IST as yyyy-mm-dd. */
export function istToday(now: Date = new Date()) {
  return new Intl.DateTimeFormat('en-CA', { timeZone: IST }).format(now)
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
