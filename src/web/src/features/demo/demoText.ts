import type { TFunction } from 'i18next'

/** The later of two ISO instants: this browser's own reset or anyone's, so "Under the hood" starts at the last one. */
export function later(a: string | null, b: string | null) {
  if (!a || !b) {
    return a ?? b
  }
  return new Date(a) >= new Date(b) ? a : b
}

/** "22 Oct", in the UI language. */
export function shortDate(date: string, language: string) {
  return new Intl.DateTimeFormat(language === 'en' ? 'en-IN' : language, {
    day: 'numeric',
    month: 'short',
    timeZone: 'UTC',
  }).format(new Date(`${date}T00:00:00Z`))
}

/** "Manjunath's family's phone · Kannada". */
export function familyTitle(t: TFunction, name: string, language: string) {
  return t('demo.parentPhone', { name, language: t(`missed.language.${language}`) })
}
