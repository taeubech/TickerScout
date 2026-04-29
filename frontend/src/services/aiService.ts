const AI_PROMPT_URL = 'https://localhost:7283'

interface AiPromptResponse {
  reply: string
}

export async function sendPrompt(prompt: string): Promise<string> {
  const response = await fetch(`${AI_PROMPT_URL}/api/ai/prompt`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ prompt }),
  })

  if (!response.ok) {
    throw new Error(`Server error: ${response.status}`)
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
