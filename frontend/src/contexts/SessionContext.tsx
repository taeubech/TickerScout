import { createContext, useContext } from 'react'

export const SessionContext = createContext<string>('')

export function useSessionContext(): string {
  return useContext(SessionContext)
}
