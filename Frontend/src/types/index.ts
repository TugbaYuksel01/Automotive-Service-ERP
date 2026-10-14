export const ROLES = [
  'SuperAdmin',
  'Admin',
  'Musteri',
  'Usta',
  'Veznedar',
  'DepoSorumlusu',
] as const;

export type Role = (typeof ROLES)[number];

export type RandevuDurumu = 0 | 1 | 2 | 3 | 4;

// Durum numaralarına göre ekranda yazacak şık Türkçe metinler
export const RandevuDurumuLabels: Record<number, string> = {
  0: 'Bekliyor',
  1: 'İşlemde',
  2: 'Tamamlandı',
  3: 'İptal Edildi',
  4: 'Teslim Edildi'
};

// Durum numaralarına göre eşleşen Tailwind renkleri (Arka plan, Yazı Rengi ve Çerçeve)
export const RandevuDurumuColors: Record<number, string> = {
  0: 'bg-blue-50 text-blue-700 border-blue-200',       // Bekliyor (Mavi)
  1: 'bg-amber-50 text-amber-700 border-amber-200',    // İşlemde (Turuncu/Sarı)
  2: 'bg-green-50 text-green-700 border-green-200',    // Tamamlandı (Yeşil)
  3: 'bg-red-50 text-red-700 border-red-200 line-through', // İptal Edildi (Kırmızı ve üstü çizili)
  4: 'bg-purple-50 text-purple-700 border-purple-200'  // Teslim Edildi (Mor)
};


export const PERSONEL_ROLLER: Role[] = ['Usta', 'Veznedar', 'DepoSorumlusu'];

export interface Kullanici {
  Id: number;
  AdSoyad?: string | null;
  Telefon?: string | null;
  Email?: string | null;
  Rol: Role | string;
}

export interface Arac {
  Id: number;
  KullaniciId: number;
  Plaka?: string | null;
  MarkaModel?: string | null;
  UretimYili: number;
}

export interface HizmetTipi {
  Id: number;
  Ad: string;
  Fiyat: number;
  UstaSureDakika: number;
  LiftSureDakika: number;
  GerekenUzmanlik?: string | null;
}

export interface Istasyon {
  Id: number;
  Ad: string;
  MusaitMi: boolean;
}

export interface Usta {
  Id: number;
  AdSoyad: string;
  UzmanlikAlani: string;
}

export interface YedekParca {
  Id: number;
  ParcaAdi?: string | null;
  BirimFiyat: number;
  StokMiktari: number;
  KritikStokSeviyesi: number;
  UyumluHizmetTipiId: number;
}

export interface Fatura {
  Id: number;
  RandevuId: number;
  ToplamParcaMaliyeti: number;
  IscilikUcreti: number;
  GenelToplam: number;
  KesimTarihi: string;
  PdfDosyaYolu?: string | null;
  OdendiMi: boolean;
  OdemeTarihi?: string | null;
  OdemeYontemi?: string | null;
  TahsilEdenKullaniciId?: number | null;
}

export interface Randevu {
  Id: number;
  AracId?: number | null;
  KullaniciId: number;
  HizmetTipiId: number;
  UstaId?: number | null;
  IstasyonId?: number | null;
  Tarih: string;
  MusteriNotu?: string | null;
  AcilMi: boolean;
  Durum: RandevuDurumu;
  GerceklesenBaslangic?: string | null;
  GerceklesenBitis?: string | null;
  TeslimTarihi?: string | null;
  TeslimEdenKullaniciId?: number | null;
  Arac?: Arac;
  Kullanici?: Kullanici;
  HizmetTipi?: HizmetTipi;
  Usta?: Usta;
  Istasyon?: Istasyon;
  Fatura?: Fatura;
}

export interface LoginDto {
  Email: string;
  Sifre: string;
}

export interface RegisterDto {
  AdSoyad: string;
  Email: string;
  Sifre: string;
  Rol?: string;
}

export interface SifremiUnuttumDto {
  Email: string;
}

export interface SifreSifirlaDto {
  Email: string;
  Token: string;
  YeniSifre: string;
}

export interface PersonelEkleDto {
  AdSoyad: string;
  Email: string;
  Telefon?: string | null;
  Sifre: string;
  Rol: string;
  uzmanlikAlani:string
}

export interface RandevuOlusturDto {
  MusteriId?: number | null;
  MusteriAdSoyad: string;
  MusteriTelefon: string;
  Email: string;
  AracPlaka: string;
  AracMarkaModel: string;
  AracUretimYili: number;
  HizmetTipiId: number;
  Tarih: string;
  MusteriNotu: string;
  SignalRConnectionId: string;
}

export interface KayitliRandevuTalebiDto {
  AracPlaka: string;
  AracMarkaModel?: string | null;
  AracUretimYili?: number | null;
  Tarih: string;
  MusteriNotu: string;
  SignalRConnectionId: string;
}

export interface SureUzatDto {
  EkSureDakika: number;
  Sebep: string;
}

export interface KullanilanParcaDto {
  YedekParcaId: number;
  KullanilanMiktar: number;
}

export interface RandevuTamamlamaDto {
  RandevuId: number;
  KullanilanParcalar: KullanilanParcaDto[];
}

export interface RandevuDetayDto {
  RandevuNo: number;
  Tarih: string;
  MusteriBilgisi: string;
  AracPlaka: string;
  HizmetAdi: string;
  ToplamTutar: number;
  SorumluUsta: string;
}

export interface AiTalepDto {
  MusteriSikayeti: string;
  Plaka: string;
  SignalRConnectionId: string;
}

export interface KrediKartiDto {
  KartSahibiAdSoyad: string;
  KartNumarasi: string;
  SonKullanmaTarihi: string;
  Cvv: string;
}

export interface ApiMesaj {
  Mesaj?: string;
  Hata?: string;
  Token?: string;
  RandevuId?: number;
  FaturaId?: number;
  FaturaTutari?: number;
  DetayliHata?: string;
}

export interface AiAnalysisMessage {
  RandevuId?: number;
  Baslik?: string;
  Icerik?: string;
  raw?: string;
}
