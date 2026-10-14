import { useEffect, useState } from 'react';
import { Calendar, User, Clock, Car, Wrench } from 'lucide-react';
import { randevuApi } from '@/services/api';
import { RandevuDurumuColors, RandevuDurumuLabels, type Randevu, type RandevuDurumu } from '@/types';
import { EmptyState } from '@/components/States';
import { LoadingSpinner } from '@/components/Loading';

export function DailyAppointmentsList({ type }: { type: 'admin' | 'usta' }) {
  const [randevular, setRandevular] = useState<Randevu[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    const fetchFn = type === 'admin' ? randevuApi.gunlukTum : randevuApi.ustaninGunluk;
    
    fetchFn()
      .then((data) => setRandevular(Array.isArray(data) ? data : []))
      .catch(() => setRandevular([]))
      .finally(() => setLoading(false));
  }, [type]);

  if (loading) return <LoadingSpinner label="Günlük randevular yükleniyor..." />;

  if (!randevular || randevular.length === 0) {
    return (
      <EmptyState
        icon={Calendar}
        title="Bugün Randevu Yok"
        description={type === 'admin' ? "Sisteme yansıyan bugüne ait hiçbir randevu bulunmuyor." : "Bugün için size atanmış bir randevu bulunmuyor. Çayınızı yudumlayabilirsiniz!"}
      />
    );
  }

  return (
    <div className="space-y-4 max-h-[500px] overflow-y-auto pr-2 scrollbar-thin scrollbar-thumb-ink-300">
        {randevular.map((r) => {
        if (!r) return null;
        const id = r.Id;
        const durum = (r.Durum ?? 0) as RandevuDurumu;
        const tarihStr = r.Tarih && !Number.isNaN(Date.parse(r.Tarih))
          ? new Date(r.Tarih).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
          : '--:--';
        
        return (
          <div key={id} className="card p-5 flex flex-col sm:flex-row sm:items-start gap-4 transition-all hover:shadow-md border-l-4" 
               style={{ borderLeftColor: durum === 0 ? '#f59e0b' : durum === 1 ? '#3b82f6' : durum === 2 ? '#10b981' : '#ef4444' }}>
            
            {/* Saat ve Icon Kutusu */}
            <div className="flex flex-col items-center justify-center bg-ink-50 px-4 py-3 rounded-xl border border-ink-100 min-w-[80px]">
              <Clock className="w-5 h-5 text-brand-600 mb-1" />
              <span className="font-bold text-lg text-ink-900">{tarihStr}</span>
            </div>

            <div className="flex-1">
              <div className="flex items-center gap-2 mb-1.5">
                <span className="font-bold text-lg text-ink-900">
                  {r.Arac?.Plaka || 'Plaka Yok'}
                </span>
                <span className="text-sm font-semibold text-ink-400">#{id}</span>
                
                {/* Sadece Admin ekranındaysa ustanın adını etiket olarak göster */}
                {type === 'admin' && r.Usta && (
                  <span className="inline-flex items-center gap-1 text-xs bg-brand-50 text-brand-700 px-2.5 py-1 rounded-lg border border-brand-100 font-medium">
                    <Wrench className="w-3 h-3" /> {r.Usta.AdSoyad}
                  </span>
                )}
              </div>
              
              <div className="flex items-center gap-4 text-sm font-medium text-ink-700 mb-3 flex-wrap">
                <span className="flex items-center gap-1">
                  <Car className="w-4 h-4 text-ink-400" /> {r.HizmetTipi?.Ad || 'Genel Kontrol'}
                </span>
                <span className="flex items-center gap-1">
                  <User className="w-4 h-4 text-ink-400" /> {r.Kullanici?.AdSoyad || 'Misafir Müşteri'} ({r.Kullanici?.Telefon || 'Telefon yok'})
                </span>
              </div>
              
              {r.MusteriNotu && (
                <p className="text-sm text-ink-600 bg-ink-50 p-3 rounded-lg border border-ink-100 italic leading-relaxed">
                  " {r.MusteriNotu} "
                </p>
              )}
            </div>
            
            <div className="flex flex-col items-end gap-2 shrink-0 mt-2 sm:mt-0">
              <span className={`rounded-full border px-4 py-1.5 text-xs font-bold uppercase tracking-wider ${RandevuDurumuColors[durum] ?? ''}`}>
                {RandevuDurumuLabels[durum] ?? `Durum ${durum}`}
              </span>
            </div>
          </div>
        );
      })}
    </div>
  );
}