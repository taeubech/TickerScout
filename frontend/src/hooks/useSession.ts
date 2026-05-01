import { useState } from 'react'

const SESSION_STORAGE_KEY = 'tickerscout.sessionId'

function getOrCreateSessionId(): string {
  let sessionId = sessionStorage.getItem(SESSION_STORAGE_KEY)

  if (!sessionId) {
    sessionId = crypto.randomUUID()
    sessionStorage.setItem(SESSION_STORAGE_KEY, sessionId)
  }

  return sessionId
}

export function useSession(): string {
  const [sessionId] = useState<string>(getOrCreateSessionId)
  return sessionId
}
