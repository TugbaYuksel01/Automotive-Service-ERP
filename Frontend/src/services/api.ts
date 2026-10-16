import axios from 'axios';
import { apiMessage, toPascalKeys } from '@/lib/json';
import type {
  HizmetTipi,
  Istasyon,
  KayitliRandevuTalebiDto,
  LoginDto,
  PersonelEkleDto,
  Randevu,
  RandevuOlusturDto,
  RandevuTamamlamaDto,
  RegisterDto,
  SifreSifirlaDto,
  SifremiUnuttumDto,
  SureUzatDto,
  Usta,
  YedekParca,
  
} from '@/types';

export const API_BASE = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/$/, '') || '';




export const api = axios.create({
  baseURL: `${API_BASE}/api`,
  headers: { 'Content-Type': 'application/json' },
  transformRequest: [
    (data, headers) => {
      if (headers) headers['Content-Type'] = 'application/json';
      if (data === undefined || data === null) return data;
      if (typeof data === 'string') return data;
      // DTO property adlarını (Sifre, Email, AdSoyad...) birebir PascalCase gönder.
      return JSON.stringify(data);
    },
  ],
});

api.interceptors.response.use(
    (response) => {
        // Gelen cevap 200/201 (Başarılı) ise hiç dokunmadan geçir
        return response;
    },
    (error) => {
        // Eğer dönen bir hata varsa ve bu hatanın kodu 429 ise burası çalışır
        if (error.response && error.response.status === 429) {
            
            // Projende react-toastify veya benzeri bir bildirim aracı varsa onu kullanabilirsin
            // Örnek: toast.warning("Çok hızlı işlem yapıyorsunuz, lütfen biraz bekleyin.");
            
            // Eğer yoksa şimdilik basit bir tarayıcı uyarısı da iş görür:
            alert("Çok hızlı işlem yapıyorsunuz, lütfen biraz bekleyip tekrar deneyin.");
        }

        // 404, 500 gibi diğer hataların normal şekilde fırlatılmasına izin ver
        return Promise.reject(error);
    }
);


api.interceptors.request.use((config) => {
  const token = sessionStorage.getItem('token');
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

api.interceptors.response.use(
  (response) => {
    response.data = toPascalKeys(response.data);
    return response;
  },
  (error) => {
    const data = error.response?.data;
    const normalized = toPascalKeys(data);
    error.message = apiMessage(normalized, error.message || 'İstek başarısız oldu.');
    if (error.response) error.response.data = normalized;
    if (error.response?.status === 401) {
      sessionStorage.removeItem('token');
      sessionStorage.removeItem('user');
    }
    return Promise.reject(error);
  },
);



export function getStoredUser() {
  const raw = sessionStorage.getItem('user');
  try {
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

export function clearAuth() {
  sessionStorage.removeItem('token');
  sessionStorage.removeItem('user');
}



export const profilApi = {
  // GET: /api/Profil
  // Sadece Token gider, backend token'dan kim olduğunu bulup bilgileri döner
  getir: async () => {
    return await api.get('/api/Profil'); // Kendi endpoint'ine göre yolu (/api/Kullanici/profil vb.) uyarla
  },

  // PUT: /api/Profil/profil-guncelle
  // DTO (AdSoyad, Email, Telefon, YeniSifre) yollanır
  guncelle: async (veri: { 
    AdSoyad: string; 
    Email: string; 
    Telefon: string; 
    YeniSifre?: string 
  }) => {
    return await api.put('/api/Profil/profil-guncelle', veri);
  }
};

export const authApi = {
  register: (dto: RegisterDto) => api.post('/Auth/register', dto),
  login: (dto: LoginDto) => api.post('/Auth/login', dto),
  logout: () => api.post('/Auth/logout'),
  sifremiUnuttum: (dto: SifremiUnuttumDto) => api.post('/Auth/sifremi-unuttum', dto),
  sifreSifirla: (dto: SifreSifirlaDto) => api.post('/Auth/sifre-sifirla', dto),
};

export const adminApi = {
  adminEkle: (dto: PersonelEkleDto) => api.post('/Admin/admin-ekle', dto),
  personelEkle: (dto: PersonelEkleDto) => api.post('/Admin/personel-ekle', dto),
};

export const hizmetApi = {
  listele: async (): Promise<HizmetTipi[]> => {
    const res = await api.get('/HizmetTipleri');
    return Array.isArray(res.data) ? res.data : [];
  },
  ekle: (dto: Omit<HizmetTipi, 'Id'> & { Id?: number }) => api.post('/HizmetTipleri', dto),
};

export const istasyonApi = {
  listele: async (): Promise<Istasyon[]> => {
    const res = await api.get('/Istasyonlar');
    return Array.isArray(res.data) ? res.data : [];
  },
  ekle: (dto: Omit<Istasyon, 'Id'> & { Id?: number }) => api.post('/Istasyonlar', dto),
};

export const ustaApi = {
  listele: async (): Promise<Usta[]> => {
    const res = await api.get('/Ustalar');
    return Array.isArray(res.data) ? res.data : [];
  },
  ekle: (dto: Omit<Usta, 'Id'> & { Id?: number }) => api.post('/Ustalar', dto),
};

export const parcaApi = {
  tumStoklar: async (): Promise<YedekParca[]> => {
    const res = await api.get('/YedekParcalar/tum-stoklar');
    return Array.isArray(res.data) ? res.data : [];
  },
  kritikStoklar: async (): Promise<YedekParca[] | { Mesaj?: string }> => {
    const res = await api.get('/YedekParcalar/kritik-stoklar');
    return res.data;
  },
  ekle: (dto: Omit<YedekParca, 'Id'> & { Id?: number }) => api.post('/YedekParcalar', dto),
  guncelle: (id: number, dto: YedekParca) => api.put(`/YedekParcalar/${id}`, dto),
  sil: (id: number) => api.delete(`/YedekParcalar/${id}`),
};

export const randevuApi = {
 misafir: (dto: RandevuOlusturDto, config?: any) =>
    api.post(`/Randevular/${encodeURIComponent('Misafir Randevu')}`, dto,config),
    
  // BURASI DÜZELTİLDİ: 'Kayıtlı Müşteri Randevu' yerine 'Kayitli Randevu' yapıldı
  kayitliMusteri: (dto: KayitliRandevuTalebiDto, config?: any) =>
    api.post(`/Randevular/${encodeURIComponent('Kayitli Randevu')}`, dto,config),
    
  benimRandevularim: async (): Promise<Randevu[]> => {
    const res = await api.get('/Randevular/benim-randevularim');
    return Array.isArray(res.data) ? res.data : [];
  },
  durumSorgula: async (id: number, plaka: string) => {
    const res = await api.get(`/Randevular/sorgula`, { params: { id, plaka } });
    return res.data;
  },
  ustaninGunluk: async (): Promise<Randevu[]> => {
    const res = await api.get('/Randevular/ustanin-gunluk-randevulari');
    return Array.isArray(res.data) ? res.data : [];
  },
  gunlukTum: async (): Promise<Randevu[]> => {
    const res = await api.get('/Randevular/gunluk-tum-randevular');
    return Array.isArray(res.data) ? res.data : [];
  },
  iptal: (id: number) => api.delete(`/Randevular/${id}`),
  sureUzat: (id: number, dto: SureUzatDto) => api.put(`/Randevular/${id}/sure-uzat`, dto),
  baslat: (id: number) => api.put(`/Randevular/${id}/basla`),
  tamamla: (dto: RandevuTamamlamaDto) => api.post('/Randevular/tamamla', dto),
};
export const odemeApi = {
  // Eski fizikselOde (faturaId ile alıyordu, artık RandevuId ile alıyor)
  fizikselOde: (randevuId: number, odemeTuru: string) =>
    api.post(`/Odeme/randevu/${randevuId}/fiziksel-ode`, null, { params: { odemeTuru } }),
  
  teslimEt: (randevuId: number) => api.put(`/Odeme/${randevuId}/teslim-et`),
};

