import type { Kullanici, Role } from '@/types';

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
const NAME_ID_CLAIM = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';

export function decodeJwt(token: string): Record<string, unknown> | null {
  try {
    const payload = token.split('.')[1];
    if (!payload) return null;
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
    const json = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join(''),
    );
    return JSON.parse(json);
  } catch {
    return null;
  }
}

export function userFromToken(token: string, extras?: Partial<Kullanici>): Kullanici | null {
  const decoded = decodeJwt(token);
  if (!decoded) return null;

  const idRaw = decoded[NAME_ID_CLAIM] ?? decoded.sub ?? decoded.nameid;
  const rolRaw = decoded[ROLE_CLAIM] ?? decoded.role ?? decoded.Rol;
  const Id = Number(idRaw);
  if (!Id || !rolRaw) return null;

  return {
    Id,
    Rol: String(rolRaw) as Role,
    AdSoyad: extras?.AdSoyad ?? null,
    Email: extras?.Email ?? null,
    Telefon: extras?.Telefon ?? null,
  };
}

export function dashboardPath(rol?: string | null): string {
  switch (rol) {
    case 'SuperAdmin':
    case 'Admin':
      return '/panel';
    case 'Usta':
      return '/usta';
    case 'Veznedar':
      return '/vezne';
    case 'DepoSorumlusu':
      return '/depo';
    case 'Musteri':
      return '/randevularim';
    default:
      return '/';
  }
}
