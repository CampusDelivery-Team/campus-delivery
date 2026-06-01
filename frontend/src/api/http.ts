export type HealthResponse = {
  application: string
  status: string
  environment: string
  serverTime: string
}

export type ApiResult<T> =
  | {
      ok: true
      data: T
    }
  | {
      ok: false
      message: string
    }

export async function getApi<T>(path: string): Promise<ApiResult<T>> {
  try {
    const response = await fetch(path)

    if (!response.ok) {
      return {
        ok: false,
        message: `HTTP ${response.status}`,
      }
    }

    return {
      ok: true,
      data: (await response.json()) as T,
    }
  } catch (error) {
    return {
      ok: false,
      message: error instanceof Error ? error.message : 'Unknown request error',
    }
  }
}
