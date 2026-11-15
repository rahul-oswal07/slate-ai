import { render, screen } from '@testing-library/react'
import { I18nextProvider } from 'react-i18next'
import { initI18n } from '../../i18n'
import { ExercisesTag } from './ExercisesTag'

const i18n = initI18n()

describe('ExercisesTag (T-128)', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en')
  })

  it('says "Book exercises" for a lesson with the book’s exercises', () => {
    render(<I18nextProvider i18n={i18n}><ExercisesTag lesson={{ hasExercises: true }} /></I18nextProvider>)

    expect(screen.getByText('Book exercises')).toBeInTheDocument()
  })

  it('shows nothing for a lesson without them, or from an older API without the field', () => {
    const { container } = render(
      <I18nextProvider i18n={i18n}>
        <ExercisesTag lesson={{ hasExercises: false }} />
        <ExercisesTag lesson={{} as { hasExercises: boolean }} />
      </I18nextProvider>,
    )

    expect(container).toBeEmptyDOMElement()
  })

  it.each(['kn', 'te', 'ta', 'hi'])('has its words in %s', async (language) => {
    await i18n.changeLanguage(language)
    render(<I18nextProvider i18n={i18n}><ExercisesTag lesson={{ hasExercises: true }} /></I18nextProvider>)

    expect(screen.queryByText('Book exercises')).not.toBeInTheDocument()
    expect(screen.getByText(i18n.t('lesson.bookExercises'))).toBeInTheDocument()
  })
})
