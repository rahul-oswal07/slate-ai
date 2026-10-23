import type { TFunction } from 'i18next'

/** A number as the stick picture shows it (D-07): bundles of a hundred, bundles of ten, loose sticks. */
export function splitSticks(n: number) {
  const value = Math.max(0, Math.min(999, Math.trunc(n)))
  return { hundreds: Math.floor(value / 100), tens: Math.floor(value / 10) % 10, ones: value % 10 }
}

/** The picture's text equivalent, from the same number (C-13): "3 bundles of a hundred, 4 bundles of ten, 7 loose sticks". */
export function sticksText(t: TFunction, n: number) {
  const { hundreds, tens, ones } = splitSticks(n)
  const parts = [
    hundreds > 0 ? t('parent.sticks.hundreds', { count: hundreds }) : null,
    tens > 0 ? t('parent.sticks.tens', { count: tens }) : null,
    ones > 0 ? t('parent.sticks.ones', { count: ones }) : null,
  ].filter((part): part is string => part !== null)
  return parts.length > 0 ? parts.join(', ') : t('parent.sticks.none')
}
