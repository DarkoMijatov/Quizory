export const apiBase = import.meta.env.VITE_API_URL ?? 'http://localhost:5000/api';

export async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${apiBase}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      'X-User-Id': localStorage.getItem('quizory-user-id') ?? '',
      'X-Organization-Id': localStorage.getItem('quizory-org-id') ?? '',
      'Accept-Language': localStorage.getItem('quizory-language') ?? 'sr',
      ...(init?.headers ?? {})
    }
  });
  if (!res.ok) throw new Error(await res.text());
  if (res.status === 204) return undefined as T;
  return res.json();
}
