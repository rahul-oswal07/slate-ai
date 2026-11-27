import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef } from 'react'
import { Outlet } from 'react-router'
import { classesOf } from '../features/auth/currentClass'
import { useMe } from '../features/auth/useMe'
import { refreshSchoolDate, setSchoolDate, useSchoolDate } from '../features/today/schoolDate'
import { BottomNav } from './BottomNav'

/** Screens after setup: content above the fixed bottom nav; follows the demo's school-day switch (T-134). */
export function TeacherLayout() {
  useSchoolDateSwitch()
  useResetOnSchoolDateChange()
  return (
    <div className="pb-[72px]">
      <Outlet />
      <BottomNav />
    </div>
  )
}

/**
 * A new school day (T-134) is a different day everywhere: drop every cached answer so no screen shows another day's
 * register, ledger or catch-ups, and refetch what is on screen.
 */
function useResetOnSchoolDateChange() {
  const queryClient = useQueryClient()
  const chosen = useSchoolDate()
  const previous = useRef(chosen)
  useEffect(() => {
    if (previous.current !== chosen) {
      previous.current = chosen
      void queryClient.resetQueries({ predicate: (query) => query.queryKey[0] !== 'me' })
    }
  }, [chosen, queryClient])
}

/**
 * Follows the admin switch (T-134): when /api/me says it is off, forget any chosen day, so the bar goes and the header
 * stops; when it is on, re-read the chosen day. The switch belongs to the demo class (T-115): a teacher who has her
 * other class open keeps the chosen day for when she comes back to it.
 */
function useSchoolDateSwitch() {
  const me = useMe().data
  const on = classesOf(me).some((group) => group.demo?.schoolDate === true)
  useEffect(() => {
    if (me === undefined) {
      return
    }
    if (on) {
      refreshSchoolDate()
    } else {
      setSchoolDate(null)
    }
  }, [me, on])
}
