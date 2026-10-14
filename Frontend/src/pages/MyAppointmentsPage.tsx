import { useCallback, useEffect, useState } from 'react';
import { Calendar, Trash2, Car, Wrench, Clock, FileText } from 'lucide-react';
import { randevuApi } from '@/services/api';
import { useToast } from '@/contexts/ToastContext';
import { EmptyState } from '@/components/States';
import { LoadingSpinner } from '@/components/Loading';
import { RandevuDurumuColors, RandevuDurumuLabels, type Randevu, type RandevuDurumu } from '@/types';

export function MyAppointmentsPage() {
  const { showSuccess, showError } = useToast();
  const [items, setItems] = useState<any[]>([]); 
  const [loading, setLoading] = useState(true);

  const load = useCallback(() => {
    setLoading(true);
    randevuApi
      .benimRandevularim()
      .then(setItems)
      .catch((err) => showError('Liste alınamadı', err.message))
      .finally(() => setLoading(false));
  }, [showError]);

  useEffect(() => {
    load();
  }, [load]);

  const iptal = async (id: number) => {
    if (!window.confirm(`Bu randevuyu iptal etmek istediğinize emin misiniz?`)) return;
    try {
      await randevuApi.iptal(id);
      showSuccess('Randevunuz başarıyla iptal edildi.');
      load(); 
    } catch (error: any) {
      showError('İptal edilemedi', error?.response?.data || 'Randevuya 2 saatten az kalmış olabilir.');
    }
  };

  if (loading) return <LoadingSpinner fullPage label="Randevular yükleniyor..." />;

  return (
    <div className="mx-auto max-w-4xl px-4 py-12">
      <h1 className="text-3xl font-bold mb-8 text-gray-800">Randevularım</h1>
      
      {items.length === 0 ? (
        <EmptyState
          icon={Calendar}
          title="Randevu bulunamadı"
          description="Sistemde aktif veya geçmiş bir randevunuz görünmüyor."
        />
      ) : (
        <div className="space-y-6">
          {items.map((r: any, index) => {
            
            // Arka plan işlemleri için ID'yi saklıyoruz ama EKRANA BASMIYORUZ
            const id = r.id ?? r.Id; 
            const durum = r.durum ?? r.Durum ?? 0;
            const isIptal = durum === 3;
            
            // Tarih Formatlama
            const rawTarih = r.tarih ?? r.Tarih;
            let tarih = 'Tarih Belirtilmedi';
            if (rawTarih && !Number.isNaN(Date.parse(rawTarih))) {
                tarih = new Date(rawTarih).toLocaleString('tr-TR', { 
                    day: '2-digit', month: '2-digit', year: 'numeric', 
                    hour: '2-digit', minute: '2-digit' 
                });
            }

            // Temiz Veri Çekimi (Sadece İsimler ve Plakalar - ID YOK!)
            const aracPlaka = r.arac?.plaka ?? r.arac?.Plaka ?? r.Arac?.Plaka ?? 'Belirtilmedi';
            const hizmetAd = r.hizmetTipi?.ad ?? r.hizmetTipi?.Ad ?? r.HizmetTipi?.Ad ?? 'Belirtilmedi';
            const musteriNotu = r.musteriNotu ?? r.MusteriNotu;

            return (
              <div 
                key={id || index} 
                className={`card p-6 flex flex-col gap-4 border shadow-sm transition-all rounded-2xl relative overflow-hidden
                  ${isIptal ? 'bg-gray-50 border-red-100 opacity-80' : 'bg-white border-gray-200'}`}
              >
                {/* Durum Rozeti (Sağ Üst Köşede) */}
                <div className="absolute top-5 right-5">
                  <span className={`rounded-full border px-4 py-1.5 text-xs font-bold shadow-sm ${RandevuDurumuColors[durum] ?? 'bg-gray-100 text-gray-700 border-gray-200'}`}>
                    {RandevuDurumuLabels[durum] ?? `Bekliyor`}
                  </span>
                </div>

                {/* İçerik Izgarası (Grid Düzeni - Sütunlu) */}
                <div className={`grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-6 mt-6 sm:mt-2 ${isIptal ? 'grayscale' : ''}`}>
                  
                  {/* Araç Sütunu */}
                  <div className="flex items-start gap-3">
                    <div className="p-2.5 bg-blue-50 text-blue-600 rounded-xl">
                      <Car className="w-5 h-5" />
                    </div>
                    <div>
                      <span className="text-xs font-bold text-gray-400 uppercase tracking-wider block mb-1">Araç Plakası</span>
                      <span className={`text-base font-semibold ${isIptal ? 'text-gray-500 line-through' : 'text-gray-900'}`}>
                        {aracPlaka}
                      </span>
                    </div>
                  </div>

                  {/* Hizmet Sütunu */}
                  <div className="flex items-start gap-3">
                    <div className="p-2.5 bg-indigo-50 text-indigo-600 rounded-xl">
                      <Wrench className="w-5 h-5" />
                    </div>
                    <div>
                      <span className="text-xs font-bold text-gray-400 uppercase tracking-wider block mb-1">Hizmet Türü</span>
                      <span className={`text-base font-semibold ${isIptal ? 'text-gray-500 line-through' : 'text-gray-900'}`}>
                        {hizmetAd}
                      </span>
                    </div>
                  </div>

                  {/* Tarih Sütunu */}
                  <div className="flex items-start gap-3">
                    <div className="p-2.5 bg-amber-50 text-amber-600 rounded-xl">
                      <Clock className="w-5 h-5" />
                    </div>
                    <div>
                      <span className="text-xs font-bold text-gray-400 uppercase tracking-wider block mb-1">Randevu Zamanı</span>
                      <span className={`text-base font-semibold ${isIptal ? 'text-gray-500 line-through' : 'text-gray-900'}`}>
                        {tarih}
                      </span>
                    </div>
                  </div>
                </div>

                {/* Müşteri Notu Bölümü (Eğer varsa) */}
                {musteriNotu && (
                  <div className={`mt-3 pt-4 border-t border-gray-100 ${isIptal ? 'grayscale' : ''}`}>
                    <span className="text-xs font-bold text-gray-400 uppercase tracking-wider flex items-center gap-1.5 mb-2">
                      <FileText className="w-4 h-4" /> Bırakılan Not
                    </span>
                    <p className="text-sm text-gray-700 bg-gray-50/80 p-3 rounded-lg border border-gray-100 italic">
                      "{musteriNotu}"
                    </p>
                  </div>
                )}

                {/* İşlem Butonları Bölümü */}
                {durum === 0 && (
                  <div className="mt-2 pt-4 border-t border-gray-100 flex justify-end">
                    <button 
                      className="flex items-center gap-2 text-red-600 hover:text-white hover:bg-red-500 border border-red-200 bg-red-50 px-5 py-2 rounded-xl text-sm font-semibold transition-all shadow-sm hover:shadow" 
                      onClick={() => iptal(id)}
                    >
                      <Trash2 className="w-4 h-4" />
                      Randevuyu İptal Et
                    </button>
                  </div>
                )}
                
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
