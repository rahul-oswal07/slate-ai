import { pkg } from './fixtures'
import { byActionNeeded } from './packageOrder'
import type { PackageStatus } from './types'

const card = (id: string, status: PackageStatus) => pkg({ id, status })

/** The Catch-ups tab, by action needed (T-100). */
describe('byActionNeeded', () => {
  it('what she must check first, then failed, then being written, approved and sent', () => {
    const sorted = byActionNeeded([
      card('sent', 'sent'),
      card('approved', 'approved'),
      card('generating', 'generating'),
      card('failed', 'failed'),
      card('ready', 'ready'),
    ])

    expect(sorted.map((p) => p.id)).toEqual(['ready', 'failed', 'generating', 'approved', 'sent'])
  })

  it('ready and needs review are both hers to check, so they keep the API’s order between them', () => {
    const sorted = byActionNeeded([card('kn', 'ready'), card('te', 'needsReview'), card('hi', 'ready')])

    expect(sorted.map((p) => p.id)).toEqual(['kn', 'te', 'hi'])
  })

  it('does not change the list it is given', () => {
    const list = [card('sent', 'sent'), card('ready', 'ready')]

    byActionNeeded(list)

    expect(list.map((p) => p.id)).toEqual(['sent', 'ready'])
  })
})
