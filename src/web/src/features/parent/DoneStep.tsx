import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Icon } from '../../components/Icon'
import { PrimaryButton } from './ParentParts'
import type { ParentCatchUp } from './types'

/** P4 Done: "Well done" whatever the answer (D-20); the teacher sees it; bring the page. No score anywhere. */
export function DoneStep({ page }: { page: ParentCatchUp }) {
  const { t } = useTranslation()
  const [finished, setFinished] = useState(false)
  return (
    <div className="flex flex-col items-center gap-4 py-4 text-center">
      <Icon name="star" size={88} className="fill-illustration-warm text-illustration-warm" />
      <p className="text-display">{t('parent.done.wellDone', { child: page.childName })}</p>
      <p className="text-body-lg">{t('parent.done.teacherSees', { teacher: page.teacherName })}</p>
      {page.homework && (
        <p className="text-caption text-ink-muted">{t('parent.done.bringPage', { page: page.homework.page })}</p>
      )}
      {finished ? (
        <output className="block text-body">{t('parent.done.closeHint')}</output>
      ) : (
        <PrimaryButton onClick={() => setFinished(true)}>{t('parent.done.finish')}</PrimaryButton>
      )}
    </div>
  )
}
