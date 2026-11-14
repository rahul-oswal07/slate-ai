import { useTranslation } from 'react-i18next'
import { LevelTag } from './LevelTag'
import { useTodaysAnswers } from './useTodaysAnswers'

/**
 * Answers back (T-92, D-94): once today's message has gone, how many families got it, how many children finished the
 * picture question, and each child's answer and level, worked out by code. Updates by itself within seconds.
 */
export function AnswersCard({ classId, date }: { classId: string; date: string }) {
  const { t } = useTranslation()
  const answers = useTodaysAnswers(classId, date).data
  if (!answers || answers.sent === 0) {
    return null
  }

  return (
    <section aria-labelledby="answers-title" className="card flex flex-col gap-2 p-[14px]">
      <h2 id="answers-title" className="text-label">
        {t('answers.title', { sent: answers.sent, answered: answers.answered })}
      </h2>
      <table className="w-full table-fixed text-left text-body">
        <caption className="sr-only">{t('answers.caption')}</caption>
        <thead>
          <tr className="text-caption text-ink-muted">
            <th scope="col" className="py-1 pr-2 font-normal">{t('answers.child')}</th>
            <th scope="col" className="w-16 py-1 pr-2 text-center font-normal">{t('answers.answer')}</th>
            <th scope="col" className="w-[42%] py-1 font-normal">{t('answers.level')}</th>
          </tr>
        </thead>
        <tbody>
          {answers.rows.map((row) => (
            <tr key={row.childId} className="border-t border-divider">
              <th scope="row" className="py-2 pr-2 text-label">
                {row.firstName}
              </th>
              <td className="py-2 pr-2 text-center text-title font-extrabold">
                {row.answer ?? <span aria-label={t('answers.none')}>—</span>}
              </td>
              <td className="py-2">
                <LevelTag level={row.level} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  )
}
