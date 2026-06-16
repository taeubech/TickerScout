const AI_API_BASE_URL = import.meta.env.VITE_AI_API_URL?.replace(/\/$/, '') ?? ''

interface AiPromptResponse {
  reply: string
}

export async function sendPrompt(prompt: string, sessionId?: string): Promise<string> {
  const response = await fetch(`${AI_API_BASE_URL}/api/ai/prompt`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ prompt, sessionId }),
  })

  if (!response.ok) {
    let message = `Server error: ${response.status}`
    try {
      const errorData = await response.json() as Record<string, unknown>
      if (typeof errorData.detail === 'string' && errorData.detail) {
        message += ` - ${errorData.detail}`
      }
    } catch {
      // ignore JSON parse errors
    }
    throw new Error(message)
  }

  const data: unknown = await response.json()
  if (
    typeof data !== 'object' ||
    data === null ||
    !('reply' in data) ||
    typeof (data as Record<string, unknown>).reply !== 'string'
  ) {
    throw new Error('Unexpected response format from server')
  }

  return (data as AiPromptResponse).reply
}
