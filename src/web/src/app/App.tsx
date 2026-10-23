import { Route, Routes } from 'react-router'
import { LoginScreen } from '../features/auth/LoginScreen'
import { TeacherGate } from '../features/auth/TeacherGate'
import { ApproveScreen } from '../features/approve/ApproveScreen'
import { PackageScreen } from '../features/approve/PackageScreen'
import { DemoScreen } from '../features/demo/DemoScreen'
import { ConfirmLessonScreen } from '../features/lessons/ConfirmLessonScreen'
import { BackTodayScreen } from '../features/back/BackTodayScreen'
import { ChildScreen } from '../features/missed/ChildScreen'
import { MissedScreen } from '../features/missed/MissedScreen'
import { ParentScreen } from '../features/parent/ParentScreen'
import { EveningTemplateScreen } from '../features/setup/EveningTemplateScreen'
import { WelcomeScreen } from '../features/setup/WelcomeScreen'
import { TodayScreen } from '../features/today/TodayScreen'
import { WeekScreen } from '../features/week/WeekScreen'
import { AppShell } from './AppShell'
import { NotFound } from './NotFound'
import { TeacherLayout } from './TeacherLayout'

export function App() {
  return (
    <Routes>
      {/* The family's page: no teacher header, sign-in or language switcher (P1–P4). */}
      <Route path="p/:token" element={<ParentScreen />} />
      {/* The judges' demo shell: both phones side by side, signs in as the demo teacher itself (FR-14). */}
      <Route path="demo" element={<DemoScreen />} />
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
            <Route path="missed" element={<MissedScreen />} />
            <Route path="missed/:childId" element={<ChildScreen />} />
            <Route path="back" element={<BackTodayScreen />} />
            <Route path="week" element={<WeekScreen />} />
          </Route>
        </Route>
        <Route path="*" element={<NotFound />} />
      </Route>
    </Routes>
  )
}
