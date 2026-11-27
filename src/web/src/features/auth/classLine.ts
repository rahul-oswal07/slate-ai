import type { TFunction } from 'i18next'
import { isLanguage } from '../../i18n'
import type { ClassInfo } from './session'

/**
 * "3A · Grade 3 · Kannada medium · Shobha's class" (T-135, T-115): the class the app is showing, as the header and
 * Profile say it. The section name leads when the class has one, so two sections of a grade tell apart. Whose class it
 * is names the class's main teacher (D-79), so a co-teacher sees it too; left out when the server didn't send it (a `me`
 * cached before T-135). Null without a grade or medium (no class).
 */
export function classLine(group: ClassInfo | null | undefined, t: TFunction): string | null {
  const label = classLabel(group, t)
  return label && group?.classTeacherName ? `${label} · ${t('app.classOf', { name: group.classTeacherName })}` : label
}

/** "3A · Grade 3 · Kannada medium" (T-115): one option of the class dropdown on Today. Null without a grade or medium. */
export function classLabel(group: ClassInfo | null | undefined, t: TFunction): string | null {
  if (group?.grade == null || !group.medium) {
    return null
  }
  const medium = isLanguage(group.medium) ? t(`language.${group.medium}`) : group.medium
  const grade = t('profile.classValue', { grade: group.grade, medium })
  return group.name ? `${group.name} · ${grade}` : grade
}
