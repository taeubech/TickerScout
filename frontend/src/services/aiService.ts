const AI_PROMPT_URL = '/api/ai/prompt'

export async function sendPrompt(prompt: string): Promise<string> {
  const response = await fetch(AI_PROMPT_URL, {
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
    !('response' in data) ||
    typeof (data as Record<string, unknown>).response !== 'string'
  ) {
    throw new Error('Unexpected response format from server')
  }

  return (data as { response: string }).response
}
