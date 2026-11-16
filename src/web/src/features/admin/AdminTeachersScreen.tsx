import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AdminPage, AdminQuery } from './AdminPage'
import { type AdminTeacher, adminTime, useAdminTeachers, useClearConsent } from './useAdmin'

/**
 * /admin/teachers (T-133; open without a key, D-112): each teacher's voice consent and greeting. Clear consent deletes
 * her greeting (D-44) and clears Teacher.ConsentAt, so at her next visit she sees the first-run Welcome and records her
 * greeting with her consent again. It asks first.
 */
export function AdminTeachersScreen() {
  const { t } = useTranslation()
  return <AdminPage title={t('admin.teachers.title')}><Teachers /></AdminPage>
}

function Teachers() {
  const { t } = useTranslation()
  const teachers = useAdminTeachers()
  return (
    <AdminQuery query={teachers}>
      {(list) =>
        list.length === 0 ? (
          <p className="text-body text-ink-muted">{t('admin.teachers.empty')}</p>
        ) : (
          <>
            <p className="text-body text-ink-muted">{t('admin.teachers.intro')}</p>
            <ul className="flex flex-col gap-3">
              {list.map((teacher) => (
                <TeacherCard key={teacher.id} teacher={teacher} />
              ))}
            </ul>
          </>
        )
      }
    </AdminQuery>
  )
}

function TeacherCard({ teacher }: { teacher: AdminTeacher }) {
  const { t, i18n } = useTranslation()
  const clear = useClearConsent()
  const [confirming, setConfirming] = useState(false)
  const cancelRef = useRef<HTMLButtonElement>(null)

  // The Clear button goes away while she is asked, so keyboard focus moves to the safe answer.
  useEffect(() => {
    if (confirming) {
      cancelRef.current?.focus()
    }
  }, [confirming])

  return (
    <li className="flex flex-col gap-2 rounded-card border-[1.5px] border-edge bg-surface p-3">
      <h2 className="text-label">{teacher.name}</h2>
      {teacher.classLabel && <p className="text-caption text-ink-muted">{teacher.classLabel}</p>}

      {teacher.consentAt ? (
        <>
          <p className="text-body">
            {t('admin.teachers.consentGiven', { date: adminTime(teacher.consentAt, i18n.language) })}
          </p>
          <p className="text-caption text-ink-muted">
            {teacher.greetingSeconds === null
              ? t('admin.teachers.noGreeting')
              : t('admin.teachers.greeting', { seconds: teacher.greetingSeconds })}
          </p>
        </>
      ) : (
        <p className="text-body text-ink-muted">{t('admin.teachers.noConsent')}</p>
      )}

      {clear.isSuccess && <output className="text-caption text-primary-strong">{t('admin.teachers.cleared')}</output>}
      {clear.isError && (
        <p role="alert" className="text-caption text-error">
          {t('admin.teachers.failed')}
        </p>
      )}

      {teacher.consentAt &&
        (confirming ? (
          <div className="flex flex-col gap-2 rounded-card border-l-4 border-l-error bg-error-bg p-3">
            <p className="text-body">{t('admin.teachers.confirm', { name: teacher.name })}</p>
            <div className="flex gap-2">
              <button
                type="button"
                disabled={clear.isPending}
                onClick={() => clear.mutate(teacher.id, { onSettled: () => setConfirming(false) })}
                className="btn-primary w-auto flex-1"
              >
                {t('admin.teachers.yes')}
              </button>
              <button
                ref={cancelRef}
                type="button"
                onClick={() => setConfirming(false)}
                className="btn-secondary w-auto flex-1"
              >
                {t('admin.teachers.cancel')}
              </button>
            </div>
          </div>
        ) : (
          <button
            type="button"
            onClick={() => {
              clear.reset()
              setConfirming(true)
            }}
            className="btn-secondary w-auto self-start"
            aria-label={t('admin.teachers.clearFor', { name: teacher.name })}
          >
            {t('admin.teachers.clear')}
          </button>
        ))}
    </li>
  )
}
