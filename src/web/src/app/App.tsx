import { Route, Routes } from 'react-router'
import { AppShell } from './AppShell'
import { Home } from './Home'
import { NotFound } from './NotFound'

export function App() {
  return (
    <Routes>
      <Route element={<AppShell />}>
        <Route index element={<Home />} />
        <Route path="*" element={<NotFound />} />
      </Route>
    </Routes>
  )
}
