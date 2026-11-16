import { useId } from 'react'
import { useTranslation } from 'react-i18next'
import { AdminPage, AdminQuery } from './AdminPage'
import { type AdminDemo, adminTime, useAdminDemo, useSetSchoolDateSwitch } from './useAdmin'

/**
 * /admin/demo (T-134; open without a key, D-112): the switch that lets the demo teacher act as a past school day, to
 * make test data (D-120). One per server, saved in the database, off until switched on. Off: no bar in the app and the
 * server ignores any chosen day.
 */
export function AdminDemoScreen() {
  const { t } = useTranslation()
  const demo = useAdminDemo()
  return (
    <AdminPage title={t('admin.demo.title')}>
      <AdminQuery query={demo}>{(data) => <SchoolDateSwitch demo={data} />}</AdminQuery>
    </AdminPage>
  )
}

function SchoolDateSwitch({ demo }: { demo: AdminDemo }) {
  const { t, i18n } = useTranslation()
  const set = useSetSchoolDateSwitch()
  const titleId = useId()
  const aboutId = useId()

  return (
    <section aria-labelledby={titleId} className="flex flex-col gap-3 rounded-card border-[1.5px] border-edge bg-surface p-3">
      <div className="flex items-start justify-between gap-3">
        <h2 id={titleId} className="text-label">
          {t('admin.demo.schoolDate')}
        </h2>
        <button
          type="button"
          role="switch"
          aria-checked={demo.schoolDate}
          aria-labelledby={titleId}
          aria-describedby={aboutId}
          disabled={!demo.demoEnabled || set.isPending}
          onClick={() => set.mutate(!demo.schoolDate)}
          className={`relative inline-flex h-8 w-14 shrink-0 items-center rounded-full border-2 border-border transition-colors disabled:opacity-40 ${
            demo.schoolDate ? 'bg-primary' : 'bg-canvas'
          }`}
        >
          <span
            aria-hidden="true"
            className={`inline-block h-6 w-6 rounded-full bg-surface shadow transition-transform ${
              demo.schoolDate ? 'translate-x-6' : 'translate-x-0.5'
            }`}
          />
        </button>
      </div>
      <p id={aboutId} className="text-body text-ink-muted">
        {t('admin.demo.about')}
      </p>
      <p className="text-body">{t(demo.schoolDate ? 'admin.demo.on' : 'admin.demo.off')}</p>
      {!demo.demoEnabled && <p className="text-caption text-ink-muted">{t('admin.demo.demoOff')}</p>}
      <p className="text-caption text-ink-muted">
        {demo.updatedAt
          ? t('admin.demo.updatedAt', { date: adminTime(demo.updatedAt, i18n.language) })
          : t('admin.demo.never')}
      </p>
      {set.isSuccess && <output className="text-caption text-primary-strong">{t('admin.demo.saved')}</output>}
      {set.isError && (
        <p role="alert" className="text-caption text-error">
          {t('admin.demo.failed')}
        </p>
      )}
    </section>
  )
}
