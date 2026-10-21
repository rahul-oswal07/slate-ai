import { Route, Routes } from 'react-router'
import { LoginScreen } from '../features/auth/LoginScreen'
import { TeacherGate } from '../features/auth/TeacherGate'
import { WelcomeScreen } from '../features/setup/WelcomeScreen'
import { AppShell } from './AppShell'
import { Home } from './Home'
import { NotFound } from './NotFound'

export function App() {
  return (
    <Routes>
      <Route element={<AppShell />}>
        <Route path="login" element={<LoginScreen />} />
        <Route element={<TeacherGate need="setup" />}>
          <Route path="welcome" element={<WelcomeScreen />} />
        </Route>
        <Route element={<TeacherGate need="ready" />}>
          <Route index element={<Home />} />
        </Route>
        <Route path="*" element={<NotFound />} />
      </Route>
    </Routes>
  )
}
