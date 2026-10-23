export type ListenState = 'idle' | 'loading' | 'playing' | 'paused' | 'ended'

/**
 * P1 label rule: "Listen" at the start; "Loading…" while loading; "Pause" while playing; "Resume" whenever playback
 * is part-way; "Listen again" after the end. The button's accessible name is this text (C-01).
 */
export function listenLabelKey(state: ListenState) {
  switch (state) {
    case 'idle':
      return 'parent.listen.listen'
    case 'loading':
      return 'parent.listen.loading'
    case 'playing':
      return 'parent.listen.pause'
    case 'paused':
      return 'parent.listen.resume'
    case 'ended':
      return 'parent.listen.again'
  }
}
