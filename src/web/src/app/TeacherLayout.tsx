import { Outlet } from 'react-router'
import { BottomNav } from './BottomNav'

/** Screens after setup: content above the fixed bottom nav. */
export function TeacherLayout() {
  return (
    <div className="pb-[72px]">
      <Outlet />
      <BottomNav />
    </div>
  )
}
