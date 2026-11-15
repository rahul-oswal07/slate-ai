import type { CatchUpPackage, PackageStatus } from './types'

/** Lower comes first: what she must check, then what failed (Try again), then what is out of her hands (T-100). */
const RANK: Record<PackageStatus, number> = {
  needsReview: 0,
  ready: 0,
  failed: 1,
  generating: 2,
  approved: 3,
  sent: 4,
}

/** The week's catch-ups by action needed (T-100, G1). Stable: within a rank, the API's order (language, target) stays. */
export function byActionNeeded(packages: readonly CatchUpPackage[]): CatchUpPackage[] {
  return packages
    .map((pkg, index) => ({ pkg, index }))
    .sort((a, b) => RANK[a.pkg.status] - RANK[b.pkg.status] || a.index - b.index)
    .map(({ pkg }) => pkg)
}
