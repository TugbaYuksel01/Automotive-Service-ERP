import { useEffect, useState, useRef } from 'react';
import { Sparkles, ArrowRight, CalendarDays, Clock, CheckCircle, AlertTriangle, Loader2 } from 'lucide-react';
import { randevuApi } from '@/services/api';
import { useSignalR } from '@/hooks/useSignalR';
import { useToast } from '@/contexts/ToastContext';
import type { RandevuOlusturDto } from '@/types';

export function GuestAppointmentPage() {
  const { 
    connectionId, aiMesaji, hataMesaji, basariMesaji, isAnalizBekleniyor, 
    setAnalizBekleniyor, resetMesajlar, setAiMesaji, setBasariMesaji, setHataMesaji 
  } = useSignalR();
  
  const { showError, showSuccess } = useToast();
  const [loading, setLoading] = useState(false);
  
  // YENİ: Formu gizlemek için tuttuğumuz state
  const [islemBasarili, setIslemBasarili] = useState(false);
  
  // YENİ: Sayfa açıldığında sabit bir şifre üretir
  const [idempotencyKey, setIdempotencyKey] = useState(() => crypto.randomUUID());
  
  // YENİ: React'in state yavaşlığını beklemeden anında çift tıklamayı engelleyen kilit
  const isSubmittingRef = useRef(false);
  
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
    
    // Anında Kilit: Eğer zaten istek atılıyorsa, ikinci tıklamayı salisesinde reddet
    if (isSubmittingRef.current) return;

    if (!seciliTarih || !seciliSaat) {
      showError('Eksik Bilgi', 'Lütfen randevu tarihi ve saatini seçiniz.');
      return;
    }

    // Kilidi kapat
    isSubmittingRef.current = true;
    setLoading(true);
    resetMesajlar();
    setAnalizBekleniyor(true);

    try {
      const birlesikTarih = new Date(`${seciliTarih}T${seciliSaat}`).toISOString();

      const payload: RandevuOlusturDto = {
        ...form,
        Tarih: birlesikTarih,
        SignalRConnectionId: connectionId || form.SignalRConnectionId || "senkron-istek",
      };

      // Sabit Idempotency Key'i gönderiyoruz
      const response = await randevuApi.misafir(payload, {
        headers: {
          'Idempotency-Key': idempotencyKey
        }
      });
      
      setAnalizBekleniyor(false);

      const gelenMesaj = (response as any).data?.mesaj || (response as any).data?.Mesaj || "Yapay zeka analizi tamamlandı.";
      const randevuId = (response as any).data?.randevuId || (response as any).data?.RandevuId;
      
      setAiMesaji(gelenMesaj);
      
      if (randevuId) {
         const basariMetni = `Randevu detaylarınız sisteme başarıyla kaydedildi. (No: ${randevuId})`;
         setBasariMesaji(basariMetni);
         showSuccess('Randevu Onaylandı', basariMetni);
         
         setIdempotencyKey(crypto.randomUUID()); // Sonraki işlemler için şifreyi yenile
         setIslemBasarili(true); // İŞLEM BAŞARILI DURUMUNU AKTİF ET (Formu gizleyecek)
      }

    } catch (error: any) {
      setAnalizBekleniyor(false);
      
      let hataMetni = error.response?.data?.mesaj || error.response?.data?.Mesaj;
      
      // Eğer backend 409 (Conflict) dönerse, bu mükerrer işlemdir.
      if (error.response?.status === 409) {
          hataMetni = "Bu randevu zaten oluşturuldu (Mükerrer işlem).";
      } else {
          // Kapasite dolu veya başka bir hataysa, kullanıcının tekrar denemesi için YENİ şifre ver
          setIdempotencyKey(crypto.randomUUID());

          if (!hataMetni) {
            if (error.response?.status === 404) {
              hataMetni = "API adresi bulunamadı. Backend'i kontrol edin.";
            } else {
              hataMetni = "Sunucuya bağlanılamadı.";
            }
          }
      }
      
      setHataMesaji(hataMetni);
      showError('İşlem Başarısız', hataMetni);
    } finally {
      // İşlem bitince kilitleri aç
      setLoading(false);
      isSubmittingRef.current = false;
    }
  };

  return (
    <div className="mx-auto max-w-3xl px-4 py-12">
      <div className="mb-8 text-center">
        <h1 className="text-3xl font-bold text-ink-900 mb-2">Hızlı Randevu Oluştur</h1>
        <p className="text-sm text-ink-500">Kayıt olmadan saniyeler içinde aracınız için servis randevusu alın.</p>
      </div>

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

        {aiMesaji && !hataMesaji && !aiMesaji.includes("BİLGİLENDİRME") && !aiMesaji.includes("ACİL DURUM") && (
          <div className="rounded-2xl border border-accent-200 bg-accent-50 p-4 flex gap-3 shadow-sm animate-in fade-in slide-in-from-bottom-4">
            <Sparkles className="h-6 w-6 text-accent-600 shrink-0" />
            <div className="flex-1">
              <h3 className="text-sm font-semibold text-accent-900 mb-1">Yapay Zeka Analizi</h3>
              <p className="text-sm text-accent-800 leading-relaxed whitespace-pre-wrap">{aiMesaji}</p>
            </div>
          </div>
        )}

        {aiMesaji && aiMesaji.includes("BİLGİLENDİRME") && (
          <div className="rounded-2xl border border-amber-300 bg-amber-50 p-4 flex gap-3 shadow-sm animate-in fade-in slide-in-from-bottom-4">
            <AlertTriangle className="h-6 w-6 text-amber-600 shrink-0" />
            <div className="flex-1">
              <h3 className="text-sm font-semibold text-amber-900 mb-1">Yapay Zeka Tespit & Süre Uyarısı</h3>
              <p className="text-sm text-amber-800 leading-relaxed whitespace-pre-wrap">{aiMesaji}</p>
            </div>
          </div>
        )}

        {aiMesaji && aiMesaji.includes("ACİL DURUM") && (
          <div className="rounded-2xl border border-red-300 bg-red-50 p-4 flex gap-3 shadow-sm animate-in fade-in slide-in-from-bottom-4">
            <AlertTriangle className="h-6 w-6 text-red-600 shrink-0 animate-bounce" />
            <div className="flex-1">
              <h3 className="text-sm font-semibold text-red-900 mb-1">Kritik Vaka / VIP Öncelik</h3>
              <p className="text-sm text-red-800 leading-relaxed whitespace-pre-wrap">{aiMesaji}</p>
            </div>
          </div>
        )}

        {hataMesaji && (
          <div className="rounded-2xl border border-red-200 bg-red-50 p-4 flex gap-3 shadow-sm animate-in fade-in zoom-in">
            <AlertTriangle className="h-6 w-6 text-red-600 shrink-0" />
            <div className="flex-1">
              <h3 className="text-sm font-semibold text-red-900 mb-1">İşlem Başarısız</h3>
              <p className="text-sm text-red-800 leading-relaxed whitespace-pre-wrap">{hataMesaji}</p>
            </div>
          </div>
        )}

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

      {/* İŞLEM BAŞARILIYSA FORMU GİZLE */}
      {!islemBasarili && (
        <form onSubmit={handleSubmit} className="card p-6 sm:p-8 space-y-8 animate-in fade-in">
          
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
      )}

      {/* İŞLEM BAŞARILIYSA GÖRÜNECEK YENİ BUTON */}
      {islemBasarili && (
        <div className="mt-6 text-center animate-in fade-in zoom-in">
          <button 
            onClick={() => window.location.reload()} 
            className="btn-secondary px-6 py-2"
          >
            Yeni Bir Randevu Daha Al
          </button>
        </div>
      )}
    </div>
  );
}
