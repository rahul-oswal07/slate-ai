import { Route, Routes } from 'react-router'
import { LoginScreen } from '../features/auth/LoginScreen'
import { TeacherGate } from '../features/auth/TeacherGate'
import { WelcomeScreen } from '../features/setup/WelcomeScreen'
import { TodayScreen } from '../features/today/TodayScreen'
import { AppShell } from './AppShell'
import { ComingSoon } from './ComingSoon'
import { NotFound } from './NotFound'
import { TeacherLayout } from './TeacherLayout'

export function App() {
  return (
    <Routes>
      <Route element={<AppShell />}>
        <Route path="login" element={<LoginScreen />} />
        <Route element={<TeacherGate need="setup" />}>
          <Route path="welcome" element={<WelcomeScreen />} />
        </Route>
        <Route element={<TeacherGate need="ready" />}>
          <Route element={<TeacherLayout />}>
            <Route index element={<TodayScreen />} />
            <Route path="approve" element={<ComingSoon tab="approve" />} />
            <Route path="missed" element={<ComingSoon tab="missed" />} />
            <Route path="back" element={<ComingSoon tab="back" />} />
            <Route path="week" element={<ComingSoon tab="week" />} />
          </Route>
        </Route>
        <Route path="*" element={<NotFound />} />
      </Route>
    </Routes>
  )
}
