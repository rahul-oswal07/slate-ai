import { setSchoolToday } from '../features/today/schoolTime'

/** The server answered with an error status. */
export class ApiError extends Error {
  readonly status: number
  readonly body: unknown

  constructor(status: number, body: unknown) {
    super(`API returned ${status}`)
    this.status = status
    this.body = body
  }
}

/** The request never reached the server (offline, DNS, dropped connection). */
export class NetworkError extends Error {
  constructor() {
    super('Network unavailable')
  }
}

type Method = 'GET' | 'POST' | 'PUT' | 'DELETE'

/** JSON (or a multipart `form`) over fetch with the session cookie. 204 resolves to undefined. */
export async function api<T>(
  path: string,
  options: { method?: Method; body?: unknown; form?: FormData; headers?: Record<string, string> } = {},
): Promise<T> {
  let response: Response
  try {
    response = await fetch(path, {
      method: options.method ?? 'GET',
      credentials: 'same-origin',
      headers: {
        ...options.headers,
        ...(options.body === undefined ? {} : { 'Content-Type': 'application/json' }),
      },
      body: options.form ?? (options.body === undefined ? undefined : JSON.stringify(options.body)),
    })
  } catch {
    throw new NetworkError()
  }

  // Only a successful response without the header means "no fixed date": an error page from a proxy or gateway
  // never went through the API and must not switch a fixed demo date back to the phone's clock.
  const schoolToday = response.headers.get('X-School-Today')
  if (schoolToday !== null || response.ok) {
    setSchoolToday(schoolToday)
  }

  if (!response.ok) {
    let body: unknown = null
    try {
      body = await response.json()
    } catch {
      // Empty or non-JSON error body.
    }
    throw new ApiError(response.status, body)
  }

  return response.status === 204 ? (undefined as T) : ((await response.json()) as T)
}
