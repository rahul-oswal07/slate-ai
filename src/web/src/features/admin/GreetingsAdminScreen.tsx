import { useTranslation } from 'react-i18next'
import { AdminPage, AdminQuery } from './AdminPage'
import { ClipPlayer } from './ClipPlayer'
import { adminTime, useAdminGreetings } from './useAdmin'

/**
 * /admin/greetings#key=… (T-66, D-83): every teacher's recorded greeting, to listen to. Linked from nowhere, no
 * teacher sign-in; the key in the link is the only way in.
 */
export function GreetingsAdminScreen() {
  const { t } = useTranslation()
  return <AdminPage title={t('admin.greetings.title')}>{(key) => <Greetings adminKey={key} />}</AdminPage>
}

function Greetings({ adminKey: key }: { adminKey: string }) {
  const { t, i18n } = useTranslation()
  const greetings = useAdminGreetings(key)

  return (
    <AdminQuery query={greetings}>
      {(list) =>
        list.length === 0 ? (
          <p className="text-body text-ink-muted">{t('admin.greetings.empty')}</p>
        ) : (
          <>
            <p className="text-body text-ink-muted">{t('admin.greetings.count', { count: list.length })}</p>
            <ul className="flex flex-col gap-3">
              {list.map((greeting) => (
                <li key={greeting.clipId} className="rounded-card border-[1.5px] border-edge bg-surface p-3">
                  <p className="text-label">{greeting.teacherName}</p>
                  <p className="text-caption text-ink-muted">
                    {t('admin.greetings.meta', {
                      seconds: greeting.seconds,
                      date: adminTime(greeting.recordedAt, i18n.language),
                    })}
                  </p>
                  <ClipPlayer
                    adminKey={key}
                    url={`/api/admin/greetings/${greeting.clipId}/audio`}
                    playLabel={t('admin.greetings.play', { name: greeting.teacherName })}
                    playerLabel={t('admin.greetings.player', { name: greeting.teacherName })}
                  />
                </li>
              ))}
            </ul>
          </>
        )
      }
    </AdminQuery>
  )
}
