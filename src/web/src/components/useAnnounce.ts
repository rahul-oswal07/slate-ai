import { createContext, useContext } from 'react'

export const AnnounceContext = createContext<(message: string) => void>(() => {})

/** Say something in the app's polite live region (see AnnouncerProvider). */
export const useAnnounce = () => useContext(AnnounceContext)
