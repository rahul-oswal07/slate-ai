/** Every admin page: the nav on each page and the list on `/admin` both come from here. */
export const ADMIN_PAGES = [
  { path: '/admin/greetings', label: 'admin.nav.greetings', description: 'admin.index.greetings' },
  { path: '/admin/lesson-audio', label: 'admin.nav.lessonAudio', description: 'admin.index.lessonAudio' },
  { path: '/admin/lesson-links', label: 'admin.nav.lessonLinks', description: 'admin.index.lessonLinks' },
] as const
