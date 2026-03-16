// API 기본 URL — 환경변수가 없으면 로컬 개발 서버로 폴백
const rawUrl = (import.meta.env.VITE_API_URL as string | undefined) ?? ''
const BASE_URL = (rawUrl.trim() || 'http://localhost:5244').replace(/\/$/, '')

// 에러 응답 본문({ code, message })을 파싱하여 의미있는 메시지를 포함한 Error를 반환
async function toApiError(res: Response, method: string, path: string): Promise<Error> {
  try {
    const body = await res.json() as { code?: string; message?: string }
    const msg = body?.message ?? body?.code ?? `${method} ${path} failed: ${res.status}`
    return new Error(msg)
  } catch {
    return new Error(`${method} ${path} failed: ${res.status}`)
  }
}

export const apiClient = {
  async get<T>(path: string): Promise<T> {
    const res = await fetch(`${BASE_URL}${path}`)

    if (!res.ok) throw await toApiError(res, 'GET', path)

    return res.json() as Promise<T>
  },

  async post<T>(path: string, body: unknown): Promise<T> {
    const res = await fetch(`${BASE_URL}${path}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    })

    if (!res.ok) throw await toApiError(res, 'POST', path)

    return res.json() as Promise<T>
  },

  async put<T>(path: string, body: unknown): Promise<T> {
    const res = await fetch(`${BASE_URL}${path}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    })

    if (!res.ok) throw await toApiError(res, 'PUT', path)

    return res.json() as Promise<T>
  },

  async delete(path: string): Promise<void> {
    const res = await fetch(`${BASE_URL}${path}`, { method: 'DELETE' })

    if (!res.ok) throw await toApiError(res, 'DELETE', path)
  },
}
