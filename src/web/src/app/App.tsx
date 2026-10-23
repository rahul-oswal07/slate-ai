import { Route, Routes } from 'react-router'
import { LoginScreen } from '../features/auth/LoginScreen'
import { TeacherGate } from '../features/auth/TeacherGate'
import { ApproveScreen } from '../features/approve/ApproveScreen'
import { PackageScreen } from '../features/approve/PackageScreen'
import { ConfirmLessonScreen } from '../features/lessons/ConfirmLessonScreen'
import { EveningTemplateScreen } from '../features/setup/EveningTemplateScreen'
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
          <Route path="setup/evening" element={<EveningTemplateScreen />} />
          <Route element={<TeacherLayout />}>
            <Route index element={<TodayScreen />} />
            <Route path="lesson" element={<ConfirmLessonScreen />} />
            <Route path="approve" element={<ApproveScreen />} />
            <Route path="approve/:id" element={<PackageScreen />} />
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
