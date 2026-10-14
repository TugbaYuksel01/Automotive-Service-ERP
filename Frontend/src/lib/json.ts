function isPlainObject(value: unknown): value is Record<string, unknown> {
  return Boolean(value) && typeof value === 'object' && !Array.isArray(value);
}

export function toPascalKeys(value: unknown): unknown {
  if (Array.isArray(value)) return value.map(toPascalKeys);
  if (!isPlainObject(value)) return value;

  const out: Record<string, unknown> = {};
  for (const [key, nested] of Object.entries(value)) {
    const pascal = key.length ? key.charAt(0).toUpperCase() + key.slice(1) : key;
    out[pascal] = toPascalKeys(nested);
  }
  return out;
}

export function apiMessage(data: unknown, fallback: string): string {
  if (typeof data === 'string' && data.trim()) return data;
  if (!isPlainObject(data)) return fallback;
  const mesaj = data.Mesaj ?? data.Hata ?? data.DetayliHata ?? data.message ?? data.title;
  if (typeof mesaj === 'string' && mesaj.trim()) return mesaj;
  if (typeof data.errors === 'object' && data.errors) {
    const first = Object.values(data.errors as Record<string, unknown>)[0];
    if (Array.isArray(first) && typeof first[0] === 'string') return first[0];
  }
  return fallback;
}

export function pickToken(data: unknown): string | null {
  if (!isPlainObject(data)) return null;
  const token = data.Token ?? data.token;
  return typeof token === 'string' && token.length > 0 ? token : null;
}
