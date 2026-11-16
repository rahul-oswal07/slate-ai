/** Every admin page: the nav on each page and the list on `/admin` both come from here. */
export const ADMIN_PAGES = [
  { path: '/admin/greetings', label: 'admin.nav.greetings', description: 'admin.index.greetings' },
  { path: '/admin/lesson-audio', label: 'admin.nav.lessonAudio', description: 'admin.index.lessonAudio' },
  { path: '/admin/lesson-links', label: 'admin.nav.lessonLinks', description: 'admin.index.lessonLinks' },
  { path: '/admin/children', label: 'admin.nav.children', description: 'admin.index.children' },
  { path: '/admin/teachers', label: 'admin.nav.teachers', description: 'admin.index.teachers' },
  { path: '/admin/demo', label: 'admin.nav.demo', description: 'admin.index.demo' },
  // Not an admin page: the demo's Under the hood on its own (T-126), listed here so it is easy to find (T-142).
  { path: '/demo/ai', label: 'admin.nav.underTheHood', description: 'admin.index.underTheHood' },
] as const
