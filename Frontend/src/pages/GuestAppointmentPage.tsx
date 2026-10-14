import { useEffect, useState } from 'react';
import { Sparkles, ArrowRight, CalendarDays, Clock, CheckCircle, AlertTriangle, Loader2 } from 'lucide-react';
import { randevuApi } from '@/services/api';
import { useSignalR } from '@/hooks/useSignalR';
import { useToast } from '@/contexts/ToastContext';
import type { RandevuOlusturDto } from '@/types';

export function GuestAppointmentPage() {
  // Hook'tan dönen set fonksiyonlarını eksiksiz aldık
  const { 
    connectionId, aiMesaji, hataMesaji, basariMesaji, isAnalizBekleniyor, 
    setAnalizBekleniyor, resetMesajlar, setAiMesaji, setBasariMesaji, setHataMesaji 
  } = useSignalR();
  
  const { showError, showSuccess } = useToast();
  const [loading, setLoading] = useState(false);
  
  const [seciliTarih, setSeciliTarih] = useState('');
  const [seciliSaat, setSeciliSaat] = useState('');

  const [form, setForm] = useState<RandevuOlusturDto>({
    MusteriAdSoyad: '',
    MusteriTelefon: '',
    Email: '',
    AracPlaka: '',
    AracMarkaModel: '',
    AracUretimYili: new Date().getFullYear(),
    HizmetTipiId: 1, 
    Tarih: '',
    MusteriNotu: '',
    SignalRConnectionId: '',
  });

  useEffect(() => {
    if (connectionId) setForm((f) => ({ ...f, SignalRConnectionId: connectionId }));
  }, [connectionId]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!seciliTarih || !seciliSaat) {
      showError('Eksik Bilgi', 'Lütfen randevu tarihi ve saatini seçiniz.');
      return;
    }

    resetMesajlar();
    setAnalizBekleniyor(true);
    setLoading(true);

    try {
      const birlesikTarih = new Date(`${seciliTarih}T${seciliSaat}`).toISOString();

      const payload: RandevuOlusturDto = {
        ...form,
        Tarih: birlesikTarih,
        SignalRConnectionId: connectionId || form.SignalRConnectionId || "senkron-istek",
      };

      // 1. İSTEĞİ AT VE BEKLE (SENKRON ÇALIŞMA)
      const response = await randevuApi.misafir(payload);
      
      // 2. CEVAP GELDİĞİ GİBİ "ANALİZ EDİLİYOR" SPINNER'INI KAPAT
      setAnalizBekleniyor(false);

      // 3. BACKEND'DEN GELEN (200 OK) MESAJINI VE ID'SİNİ ALIP KUTULARA YERLEŞTİR
      const gelenMesaj = (response as any).data?.mesaj || (response as any).data?.Mesaj || "Yapay zeka analizi tamamlandı.";
      const randevuId = (response as any).data?.randevuId || (response as any).data?.RandevuId;
      
      setAiMesaji(gelenMesaj);
      if (randevuId) {
         const basariMetni = `Randevu detaylarınız sisteme başarıyla kaydedildi. (No: ${randevuId})`;
         setBasariMesaji(basariMetni);
         
         // İŞTE SAĞDAN ÇIKACAK BİLDİRİM KUTUSU BURADA TETİKLENİYOR:
         showSuccess('Randevu Onaylandı', basariMetni);
      }

    } catch (error: any) {
      // HATA (400 BAD REQUEST VEYA KAPASİTE DOLU) DURUMU:
      setAnalizBekleniyor(false);
      
      const hataMetni = error.response?.data?.mesaj || error.response?.data?.Mesaj || error.message || 'Bağlantı hatası.';
      
      setHataMesaji(hataMetni);
      showError('Randevu Alınamadı', hataMetni);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="mx-auto max-w-3xl px-4 py-12">
      <div className="mb-8 text-center">
        <h1 className="text-3xl font-bold text-ink-900 mb-2">Hızlı Randevu Oluştur</h1>
        <p className="text-sm text-ink-500">Kayıt olmadan saniyeler içinde aracınız için servis randevusu alın.</p>
      </div>

      {/* --- DURUM KUTULARI --- */}
      <div className="space-y-4 mb-6">
        
        {isAnalizBekleniyor && (
          <div className="rounded-2xl border border-blue-200 bg-blue-50 p-4 flex gap-3 shadow-sm animate-pulse">
            <Loader2 className="h-6 w-6 text-blue-600 shrink-0 animate-spin" />
            <div className="flex-1">
              <h3 className="text-sm font-semibold text-blue-900 mb-1">Analiz Ediliyor...</h3>
              <p className="text-sm text-blue-800 leading-relaxed">Yapay zeka arızanızı tespit ediyor ve atölye kapasitemizi kontrol ediyor. Lütfen bekleyin.</p>
            </div>
          </div>
        )}

        {/* STANDART MAVİ AI KUTUSU */}
        {aiMesaji && !hataMesaji && !aiMesaji.includes("BİLGİLENDİRME") && !aiMesaji.includes("ACİL DURUM") && (
          <div className="rounded-2xl border border-accent-200 bg-accent-50 p-4 flex gap-3 shadow-sm animate-in fade-in slide-in-from-bottom-4">
            <Sparkles className="h-6 w-6 text-accent-600 shrink-0" />
            <div className="flex-1">
              <h3 className="text-sm font-semibold text-accent-900 mb-1">Yapay Zeka Analizi</h3>
              <p className="text-sm text-accent-800 leading-relaxed whitespace-pre-wrap">{aiMesaji}</p>
            </div>
          </div>
        )}

        {/* SARI BİLGİLENDİRME KUTUSU (Mesai/Süre Sığmazsa) */}
        {aiMesaji && aiMesaji.includes("BİLGİLENDİRME") && (
          <div className="rounded-2xl border border-amber-300 bg-amber-50 p-4 flex gap-3 shadow-sm animate-in fade-in slide-in-from-bottom-4">
            <AlertTriangle className="h-6 w-6 text-amber-600 shrink-0" />
            <div className="flex-1">
              <h3 className="text-sm font-semibold text-amber-900 mb-1">Yapay Zeka Tespit & Süre Uyarısı</h3>
              <p className="text-sm text-amber-800 leading-relaxed whitespace-pre-wrap">{aiMesaji}</p>
            </div>
          </div>
        )}

        {/* KIRMIZI ACİL DURUM KUTUSU */}
        {aiMesaji && aiMesaji.includes("ACİL DURUM") && (
          <div className="rounded-2xl border border-red-300 bg-red-50 p-4 flex gap-3 shadow-sm animate-in fade-in slide-in-from-bottom-4">
            <AlertTriangle className="h-6 w-6 text-red-600 shrink-0 animate-bounce" />
            <div className="flex-1">
              <h3 className="text-sm font-semibold text-red-900 mb-1">Kritik Vaka / VIP Öncelik</h3>
              <p className="text-sm text-red-800 leading-relaxed whitespace-pre-wrap">{aiMesaji}</p>
            </div>
          </div>
        )}

        {/* Kırmızı Red Kutusu (Kapasite Tamamen Doluysa - Catch Bloğundan Düşer) */}
        {hataMesaji && (
          <div className="rounded-2xl border border-red-200 bg-red-50 p-4 flex gap-3 shadow-sm animate-in fade-in zoom-in">
            <AlertTriangle className="h-6 w-6 text-red-600 shrink-0" />
            <div className="flex-1">
              <h3 className="text-sm font-semibold text-red-900 mb-1">Kapasite Dolu</h3>
              <p className="text-sm text-red-800 leading-relaxed whitespace-pre-wrap">{hataMesaji}</p>
            </div>
          </div>
        )}

        {/* Yeşil Onay Kutusu (Yer varsa) */}
        {basariMesaji && !hataMesaji && (
          <div className="rounded-2xl border border-green-200 bg-green-50 p-4 flex gap-3 shadow-sm animate-in fade-in zoom-in">
            <CheckCircle className="h-6 w-6 text-green-600 shrink-0" />
            <div className="flex-1">
              <h3 className="text-sm font-semibold text-green-900 mb-1">Randevunuz Onaylandı</h3>
              <p className="text-sm text-green-800 leading-relaxed whitespace-pre-wrap">{basariMesaji}</p>
            </div>
          </div>
        )}
      </div>

      <form onSubmit={handleSubmit} className="card p-6 sm:p-8 space-y-8">
        
        {/* İletişim Bilgileri Bölümü */}
        <div>
          <h2 className="text-lg font-semibold text-ink-900 mb-4 border-b border-ink-100 pb-2">Kişisel Bilgiler</h2>
          <div className="grid sm:grid-cols-2 gap-4">
            <div>
              <label className="label-text">Adınız Soyadınız</label>
              <input
                name="MusteriAdSoyad"
                type="text"
                className="input-field mt-1"
                value={form.MusteriAdSoyad}
                onChange={(e) => setForm({ ...form, MusteriAdSoyad: e.target.value })}
                placeholder="Örn: Ayşe Demir"
                required
              />
            </div>
            <div>
              <label className="label-text">Telefon Numaranız</label>
              <input
                name="MusteriTelefon"
                type="tel"
                className="input-field mt-1"
                value={form.MusteriTelefon}
                onChange={(e) => setForm({ ...form, MusteriTelefon: e.target.value })}
                placeholder="05XX XXX XX XX"
                required
              />
            </div>
            <div className="sm:col-span-2">
              <label className="label-text">E-posta Adresiniz</label>
              <input
                name="Email"
                type="email"
                className="input-field mt-1"
                value={form.Email}
                onChange={(e) => setForm({ ...form, Email: e.target.value })}
                placeholder="Fatura ve bildirimler için"
                required
              />
            </div>
          </div>
        </div>

        {/* Araç ve Servis Bilgileri Bölümü */}
        <div>
          <h2 className="text-lg font-semibold text-ink-900 mb-4 border-b border-ink-100 pb-2">Araç ve Servis Detayları</h2>
          <div className="grid sm:grid-cols-2 gap-4">
            <div>
              <label className="label-text">Araç Plakası</label>
              <input
                name="AracPlaka"
                type="text"
                className="input-field mt-1 uppercase"
                value={form.AracPlaka}
                onChange={(e) => setForm({ ...form, AracPlaka: e.target.value.toUpperCase() })}
                placeholder="34 ABC 123"
                required
              />
            </div>
            <div>
              <label className="label-text">Marka ve Model</label>
              <input
                name="AracMarkaModel"
                type="text"
                className="input-field mt-1"
                value={form.AracMarkaModel}
                onChange={(e) => setForm({ ...form, AracMarkaModel: e.target.value })}
                placeholder="Örn: Fiat Egea"
                required
              />
            </div>
            <div className="sm:col-span-2">
              <label className="label-text">Üretim Yılı</label>
              <input
                name="AracUretimYili"
                type="number"
                min="1990"
                max={new Date().getFullYear() + 1}
                className="input-field mt-1"
                value={form.AracUretimYili}
                onChange={(e) => setForm({ ...form, AracUretimYili: Number(e.target.value) })}
                required
              />
            </div>

            <div className="sm:col-span-2">
              <h3 className="text-sm font-medium text-ink-900 mb-3">Randevu Zamanı</h3>
              <div className="grid sm:grid-cols-2 gap-4">
                <div className="relative group">
                  <CalendarDays className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-brand-500 group-focus-within:text-brand-600 transition-colors" />
                  <input
                    type="date"
                    min={new Date().toISOString().split('T')[0]}
                    className="input-field pl-10 h-12 cursor-pointer"
                    value={seciliTarih}
                    onChange={(e) => setSeciliTarih(e.target.value)}
                    required
                  />
                </div>
                <div className="relative group">
                  <Clock className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-brand-500 group-focus-within:text-brand-600 transition-colors" />
                  <input
                    type="time"
                    className="input-field pl-10 h-12 cursor-pointer"
                    value={seciliSaat}
                    onChange={(e) => setSeciliSaat(e.target.value)}
                    required
                  />
                </div>
              </div>
            </div>

            <div className="sm:col-span-2 mt-2">
              <label className="label-text">Arıza Şikayetiniz / Eklemek İstedikleriniz</label>
              <textarea
                name="MusteriNotu"
                className="input-field mt-1 min-h-[100px] resize-none"
                value={form.MusteriNotu}
                onChange={(e) => setForm({ ...form, MusteriNotu: e.target.value })}
                placeholder="Lütfen şikayetinizi detaylıca belirtin. Yapay zeka sistemimiz talebinizi analiz edecektir."
                required
              />
            </div>
          </div>
        </div>

        <button type="submit" disabled={loading || isAnalizBekleniyor} className="btn-primary w-full text-lg py-3">
          {loading || isAnalizBekleniyor ? 'İşleniyor...' : 'Randevuyu Tamamla'}
          {!loading && !isAnalizBekleniyor && <ArrowRight className="h-5 w-5 ml-2 inline-block" />}
        </button>
      </form>
    </div>
  );
}
