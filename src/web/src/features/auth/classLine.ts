import type { TFunction } from 'i18next'
import { isLanguage } from '../../i18n'
import type { Me } from './session'

/**
 * "Grade 3 · Kannada medium · Shobha's class" (T-135): the class the app is for, as the header and Profile show it.
 * Whose class it is names the class's main teacher (D-79), so a co-teacher sees it too; left out when the server didn't
 * send it (a `me` cached before T-135). Null without a grade or medium (no class).
 */
export function classLine(me: Me | null | undefined, t: TFunction): string | null {
  if (me?.grade == null || !me.medium) {
    return null
  }
  const medium = isLanguage(me.medium) ? t(`language.${me.medium}`) : me.medium
  const grade = t('profile.classValue', { grade: me.grade, medium })
  return me.classTeacherName ? `${grade} · ${t('app.classOf', { name: me.classTeacherName })}` : grade
}
