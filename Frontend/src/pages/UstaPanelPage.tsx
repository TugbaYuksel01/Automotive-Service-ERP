import { useEffect, useState } from 'react';
import { Calendar, Wrench, CheckCircle2 } from 'lucide-react';
import { istasyonApi, parcaApi, randevuApi } from '@/services/api';
import { useToast } from '@/contexts/ToastContext';
import { DailyAppointmentsList } from '@/components/DailyAppointmentsList';
import type { Istasyon, KullanilanParcaDto, RandevuTamamlamaDto, SureUzatDto, YedekParca } from '@/types';

export function UstaPanelPage() {
  const { showSuccess, showError } = useToast();
  const [istasyonlar, setIstasyonlar] = useState<Istasyon[]>([]);
  const [parcalar, setParcalar] = useState<YedekParca[]>([]);
  const [randevuId, setRandevuId] = useState('');
  const [sure, setSure] = useState<SureUzatDto>({ EkSureDakika: 15, Sebep: '' });
  const [tamamla, setTamamla] = useState<RandevuTamamlamaDto>({
    RandevuId: 0,
    KullanilanParcalar: [{ YedekParcaId: 0, KullanilanMiktar: 1 }],
  });

  useEffect(() => {
    istasyonApi.listele().then(setIstasyonlar).catch(() => setIstasyonlar([]));
    parcaApi.tumStoklar().then(setParcalar).catch(() => setParcalar([]));
  }, []);

  const run = async (fn: () => Promise<unknown>, ok: string) => {
    try {
      const res = await fn();
      showSuccess(ok, (res as { data?: { Mesaj?: string } })?.data?.Mesaj);
      // İşlem başarılı olursa Usta tekrar en güncel iş listesini görsün diye sayfayı yenilemek iyi bir UX'tir
      setTimeout(() => window.location.reload(), 1500); 
    } catch (error: unknown) {
      showError('İşlem başarısız', error instanceof Error ? error.message : '');
    }
  };

  const updateParca = (index: number, patch: Partial<KullanilanParcaDto>) => {
    const list = [...tamamla.KullanilanParcalar];
    list[index] = { ...list[index], ...patch };
    setTamamla({ ...tamamla, KullanilanParcalar: list });
  };

  return (
    <div className="space-y-8 max-w-6xl mx-auto">
      
      {/* BAŞLIK */}
      <div>
        <h1 className="text-2xl font-bold text-ink-900">Usta Paneli</h1>
        <p className="text-sm text-ink-500 mt-1">Bugünkü işlerinizi görüntüleyin, istasyon durumlarını kontrol edin ve randevularınızı yönetin.</p>
      </div>

      {/* YENİ EKLENEN: GÜNLÜK İŞ LİSTESİ */}
      <section className="mb-8">
        <h2 className="text-xl font-bold text-ink-900 mb-4 flex items-center gap-2">
          <Calendar className="w-6 h-6 text-brand-600" /> Bugünkü İş Listem
        </h2>
        <DailyAppointmentsList type="usta" />
      </section>

      {/* ALT KISIM: 2 SÜTUNLU KONTROL PANELİ */}
      <div className="grid md:grid-cols-2 gap-8 items-start">
        
        {/* SOL SÜTUN: İstasyonlar ve İşlem Başlatma */}
        <div className="space-y-8">
          
          <section className="card p-6">
            <h2 className="font-semibold mb-4 text-ink-900">İstasyon Durumları</h2>
            <div className="grid sm:grid-cols-2 gap-3">
              {istasyonlar.map((i) => (
                <div key={i.Id} className={`rounded-xl border p-3 flex justify-between items-center transition-colors ${i.MusaitMi ? 'border-success-200 bg-success-50' : 'border-error-200 bg-error-50'}`}>
                  <span className="font-medium text-ink-900 text-sm">{i.Ad}</span>
                  <span className={`font-bold text-xs px-2 py-1 rounded-full ${i.MusaitMi ? 'bg-success-100 text-success-700' : 'bg-error-100 text-error-700'}`}>
                    {i.MusaitMi ? 'Müsait' : 'Dolu'}
                  </span>
                </div>
              ))}
            </div>
          </section>

          <section className="card p-6 space-y-5 border-t-4 border-t-brand-500 shadow-md">
            <div>
              <h2 className="text-lg font-bold flex items-center gap-2 text-ink-900">
                <Wrench className="w-5 h-5 text-brand-600" /> Randevu Başlat / Süre Uzat
              </h2>
              <p className="text-xs text-ink-500 mt-1">Yukarıdaki listeden ID'sini gördüğünüz randevuyu buradan başlatın.</p>
            </div>
            
            <div>
              <label className="label-text">Randevu Numarası (ID)</label>
              <input className="input-field text-lg font-semibold text-center tracking-widest" placeholder="Örn: 5" value={randevuId} onChange={(e) => setRandevuId(e.target.value)} />
            </div>
            
            <button className="btn-primary w-full py-3" type="button" onClick={() => run(() => randevuApi.baslat(Number(randevuId)), 'İşlem başarıyla başlatıldı!')}>
              İşlemi Başlat
            </button>
            
            <div className="pt-5 mt-5 border-t border-ink-100">
              <p className="text-sm font-semibold text-ink-700 mb-3">Beklenmedik bir sorun mu çıktı?</p>
              <div className="grid sm:grid-cols-2 gap-3">
                <div>
                  <label className="label-text">Ek Süre (Dk)</label>
                  <input name="EkSureDakika" type="number" className="input-field" value={sure.EkSureDakika} onChange={(e) => setSure({ ...sure, EkSureDakika: Number(e.target.value) })} />
                </div>
                <div>
                  <label className="label-text">Sebep Belirtin</label>
                  <input name="Sebep" className="input-field" placeholder="Örn: Paslı cıvata" value={sure.Sebep} onChange={(e) => setSure({ ...sure, Sebep: e.target.value })} />
                </div>
              </div>
              <button className="btn-secondary w-full mt-4" type="button" onClick={() => run(() => randevuApi.sureUzat(Number(randevuId), sure), 'Süre uzatıldı ve sistem otonom olarak güncellendi.')}>
                Sisteme Süre Uzatmasını Bildir
              </button>
            </div>
          </section>
        </div>

        {/* SAĞ SÜTUN: Tamamlama ve Fatura İşlemleri */}
        <div className="space-y-8">
          <section className="card p-6 space-y-5 border-t-4 border-t-success-500 shadow-md">
            <div>
              <h2 className="text-lg font-bold flex items-center gap-2 text-ink-900">
                <CheckCircle2 className="w-5 h-5 text-success-600" /> İşlemi Tamamla
              </h2>
              <p className="text-xs text-ink-500 mt-1">İşlem bitince kullanılan parçaları girin ve faturayı kesin.</p>
            </div>

            <div>
              <label className="label-text">Randevu Numarası (ID)</label>
              <input
                name="RandevuId"
                type="number"
                className="input-field text-lg font-semibold text-center tracking-widest"
                placeholder="Örn: 5"
                value={tamamla.RandevuId || ''}
                onChange={(e) => setTamamla({ ...tamamla, RandevuId: Number(e.target.value) })}
              />
            </div>

            <div className="space-y-3 pt-3">
              <label className="label-text font-semibold">Kullanılan Yedek Parçalar</label>
              {tamamla.KullanilanParcalar.map((p, idx) => (
                <div key={idx} className="grid grid-cols-[1fr_auto] gap-3 bg-ink-50 p-3 rounded-lg border border-ink-100 items-center">
                  <div>
                    <select
                      name="YedekParcaId"
                      className="input-field text-sm bg-white"
                      value={p.YedekParcaId}
                      onChange={(e) => updateParca(idx, { YedekParcaId: Number(e.target.value) })}
                    >
                      <option value={0}>Parça Seçiniz...</option>
                      {parcalar.map((yp) => (
                        <option key={yp.Id} value={yp.Id}>
                          {yp.ParcaAdi} (Stok: {yp.StokMiktari})
                        </option>
                      ))}
                    </select>
                  </div>
                  <div className="w-20">
                    <input
                      name="KullanilanMiktar"
                      type="number"
                      min="1"
                      className="input-field text-sm px-2 text-center"
                      value={p.KullanilanMiktar}
                      onChange={(e) => updateParca(idx, { KullanilanMiktar: Number(e.target.value) })}
                    />
                  </div>
                </div>
              ))}
              
              <button
                type="button"
                className="btn-ghost w-full border border-dashed border-ink-300 hover:border-brand-400 hover:text-brand-600 mt-2"
                onClick={() =>
                  setTamamla({
                    ...tamamla,
                    KullanilanParcalar: [...tamamla.KullanilanParcalar, { YedekParcaId: 0, KullanilanMiktar: 1 }],
                  })
                }
              >
                + Yeni Parça Satırı Ekle
              </button>
            </div>

            <button
              className="w-full mt-6 bg-success-600 hover:bg-success-700 text-white font-bold py-3.5 rounded-xl shadow-lg shadow-success-600/30 transition-all active:scale-[0.98] flex items-center justify-center gap-2"
              type="button"
              onClick={() =>
                run(
                  () =>
                    randevuApi.tamamla({
                      ...tamamla,
                      KullanilanParcalar: tamamla.KullanilanParcalar.filter((x) => x.YedekParcaId > 0),
                    }),
                  'İşlem tamamlandı ve fatura başarıyla kesildi!',
                )
              }
            >
              Tamamla ve Fatura Kes
            </button>
          </section>
        </div>

      </div>
    </div>
  );
}