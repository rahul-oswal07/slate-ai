import { render, screen } from '@testing-library/react'
import { I18nextProvider } from 'react-i18next'
import { initI18n } from '../../i18n'
import { AiLabel, StepDots } from './ParentParts'

const i18n = initI18n()

function renderEn(ui: React.ReactElement) {
  return i18n.changeLanguage('en').then(() => render(<I18nextProvider i18n={i18n}>{ui}</I18nextProvider>))
}

describe('StepDots, the chalk tally (D-77)', () => {
  it('shows “2/4” and tells a screen reader “Step 2 of 4”', async () => {
    await renderEn(<StepDots step={2} />)

    expect(screen.getByText('2/4')).toHaveAttribute('aria-hidden', 'true')
    expect(screen.getByText('Step 2 of 4')).toHaveClass('sr-only')
  })

  it('draws four strokes: done, current in amber, the rest dashed, and no slash before the end', async () => {
    const { container } = await renderEn(<StepDots step={2} />)

    const svg = container.querySelector('svg')
    expect(svg).toHaveAttribute('aria-hidden', 'true')
    expect(container.querySelectorAll('[data-stroke="done"]')).toHaveLength(1)
    expect(container.querySelectorAll('[data-stroke="current"]')).toHaveLength(1)
    expect(container.querySelectorAll('[data-stroke="todo"]')).toHaveLength(2)
    expect(container.querySelector('[data-stroke="slash"]')).toBeNull()
  })

  it('on the last step all four are done and a slash crosses them', async () => {
    const { container } = await renderEn(<StepDots step={4} />)

    expect(container.querySelectorAll('[data-stroke="done"]')).toHaveLength(4)
    expect(container.querySelector('[data-stroke="current"]')).toBeNull()
    expect(container.querySelector('[data-stroke="slash"]')).not.toBeNull()
  })
})

describe('AiLabel, the quiet pill (NFR-08, D-77)', () => {
  it('says the same sentence as before, with the bot icon', async () => {
    const { container } = await renderEn(<AiLabel teacher="Shobha" />)

    expect(screen.getByText('AI-assisted, approved by Shobha teacher')).toBeInTheDocument()
    expect(container.querySelector('svg')).not.toBeNull()
  })
})
