import { useEffect, useState } from 'react';
import { Sparkles, CalendarDays, Clock, Car, Info, CheckCircle, AlertTriangle, Loader2 } from 'lucide-react';
import { randevuApi } from '@/services/api';
import { useSignalR } from '@/hooks/useSignalR';
import { useToast } from '@/contexts/ToastContext';
import type { KayitliRandevuTalebiDto } from '@/types';

export function CustomerAppointmentPage() {
  // Hook'tan senkron modelin tüm metodlarını çekiyoruz
  const { 
    connectionId, aiMesaji, hataMesaji, basariMesaji, isAnalizBekleniyor, 
    setAnalizBekleniyor, resetMesajlar, setAiMesaji, setBasariMesaji, setHataMesaji 
  } = useSignalR();
  
  const { showSuccess, showError } = useToast();
  const [yeniArac, setYeniArac] = useState(false);
  const [loading, setLoading] = useState(false);
  
  // Tarih ve saati ayrı tutarak daha iyi bir UX sunuyoruz
  const [seciliTarih, setSeciliTarih] = useState('');
  const [seciliSaat, setSeciliSaat] = useState('');

  const [form, setForm] = useState<KayitliRandevuTalebiDto>({
    AracPlaka: '',
    AracMarkaModel: null,
    AracUretimYili: null,
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
      // Tarih ve saati birleştirip ISO formatına çeviriyoruz
      const birlesikTarih = new Date(`${seciliTarih}T${seciliSaat}`).toISOString();

      const payload: KayitliRandevuTalebiDto = {
        AracPlaka: form.AracPlaka,
        AracMarkaModel: yeniArac ? form.AracMarkaModel : null,
        AracUretimYili: yeniArac ? form.AracUretimYili : null,
        Tarih: birlesikTarih,
        MusteriNotu: form.MusteriNotu,
        SignalRConnectionId: connectionId || form.SignalRConnectionId || "senkron-istek",
      };
      
      // 1. İSTEĞİ AT VE BEKLE (SENKRON ÇALIŞMA)
      const response = await randevuApi.kayitliMusteri(payload);
      
      // 2. CEVAP GELDİĞİ GİBİ SPINNER'I KAPAT
      setAnalizBekleniyor(false);

      // 3. BACKEND'DEN GELEN (200 OK) MESAJINI VE ID'SİNİ ALIP KUTULARA YERLEŞTİR
      const gelenMesaj = (response as any).data?.mesaj || (response as any).data?.Mesaj || "Yapay zeka analizi tamamlandı.";
      const randevuId = (response as any).data?.randevuId || (response as any).data?.RandevuId;
      
      setAiMesaji(gelenMesaj);
      if (randevuId) {
         const basariMetni = `Randevu detaylarınız sisteme başarıyla kaydedildi. (No: ${randevuId})`;
         setBasariMesaji(basariMetni);
         showSuccess('Randevu Onaylandı', basariMetni);
      }
    } catch (error: any) {
      // HATA (400 BAD REQUEST VEYA KAPASİTE DOLU) DURUMU:
      setAnalizBekleniyor(false);
      
      const hataMetni = error.response?.data?.mesaj || error.response?.data?.Mesaj || error.message || 'Bağlantı hatası.';
      
      setHataMesaji(hataMetni);
      showError('İşlem Başarısız', hataMetni);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="mx-auto max-w-2xl px-4 py-12">
      <div className="mb-8 text-center">
        <h1 className="text-3xl font-bold text-ink-900 mb-2">Servis Randevusu Oluştur</h1>
        <p className="text-sm text-ink-500">Aracınızın şikayetini yazın, gerisini uzmanlarımıza bırakın.</p>
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
        
        {/* Araç Bilgileri */}
        <div>
          <label className="label-text">Araç Plakası</label>
          <div className="relative mt-1">
            <Car className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-ink-400" />
            <input
              name="AracPlaka"
              className="input-field pl-10 uppercase font-medium tracking-wide"
              value={form.AracPlaka}
              onChange={(e) => setForm({ ...form, AracPlaka: e.target.value.toUpperCase() })}
              placeholder="Örn: 34 ABC 123"
              required
            />
          </div>
        </div>

        <div className="bg-ink-50/50 p-4 rounded-xl border border-ink-100 transition-all">
          <label className="flex items-center gap-3 cursor-pointer select-none">
            <input 
              type="checkbox" 
              className="w-4 h-4 rounded text-brand-600 border-ink-300 focus:ring-brand-500"
              checked={yeniArac} 
              onChange={(e) => setYeniArac(e.target.checked)} 
            />
            <span className="text-sm font-medium text-ink-700">Farklı / Yeni bir araçla geliyorum</span>
          </label>
          
          {yeniArac && (
            <div className="mt-5 grid sm:grid-cols-2 gap-4 border-t border-ink-200 pt-5 animate-in fade-in">
              <div>
                <label className="label-text">Marka ve Model</label>
                <input
                  name="AracMarkaModel"
                  className="input-field mt-1"
                  value={form.AracMarkaModel ?? ''}
                  onChange={(e) => setForm({ ...form, AracMarkaModel: e.target.value })}
                  placeholder="Örn: Volkswagen Golf"
                  required={yeniArac}
                />
              </div>
              <div>
                <label className="label-text">Üretim Yılı</label>
                <input
                  name="AracUretimYili"
                  type="number"
                  min="1990"
                  max={new Date().getFullYear() + 1}
                  className="input-field mt-1"
                  value={form.AracUretimYili ?? ''}
                  onChange={(e) => setForm({ ...form, AracUretimYili: e.target.value ? Number(e.target.value) : null })}
                  placeholder="Örn: 2020"
                  required={yeniArac}
                />
              </div>
            </div>
          )}
        </div>

        {/* Modern Tarih ve Saat Seçici */}
        <div>
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

        <div>
          <div className="flex items-center gap-2 mb-2">
            <label className="label-text mb-0">Arıza Şikayetiniz / Müşteri Notu</label>
            <div className="group relative cursor-help">
              <Info className="h-4 w-4 text-ink-400" />
              <div className="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 hidden w-64 rounded bg-ink-900 px-3 py-2 text-xs text-white group-hover:block">
                Yapay zeka asistanımız yazdıklarınızı analiz edip ön hazırlık yapar.
              </div>
            </div>
          </div>
          <textarea
            name="MusteriNotu"
            className="input-field min-h-[120px] resize-none leading-relaxed"
            value={form.MusteriNotu}
            onChange={(e) => setForm({ ...form, MusteriNotu: e.target.value })}
            placeholder="Aracınızdaki sorunu (ses, titreme, arıza lambası vb.) detaylıca anlatabilirsiniz..."
            required
          />
        </div>

        <button type="submit" disabled={loading || isAnalizBekleniyor} className="btn-primary w-full text-lg py-3.5 shadow-lg shadow-brand-500/25 hover:shadow-brand-500/40">
          {loading || isAnalizBekleniyor ? 'İşleniyor...' : 'Randevuyu Onayla'}
        </button>
      </form>
    </div>
  );
}
