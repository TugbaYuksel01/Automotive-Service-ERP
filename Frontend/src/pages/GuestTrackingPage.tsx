import { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Search, Car, Clock, Wrench, CheckCircle2, AlertCircle } from 'lucide-react';
import { randevuApi } from '@/services/api';
import { RandevuDurumuLabels, RandevuDurumuColors } from '@/types';

export function GuestTrackingPage() {
  const [searchParams] = useSearchParams();
  
  // URL'den (E-postadan gelen linkten) ID'yi otomatik alıyoruz (Örn: /takip?id=15)
  const [form, setForm] = useState({
    id: searchParams.get('id') || '',
    plaka: ''
  });

  const [durum, setDurum] = useState<any>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSorgula = async (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    if (!form.id || !form.plaka) {
      setError('Lütfen randevu numarası ve plakanızı giriniz.');
      return;
    }

    setLoading(true);
    setError('');
    setDurum(null);

    try {
      const data = await randevuApi.durumSorgula(Number(form.id), form.plaka.trim().toUpperCase());
      setDurum(data);
    } catch (err: any) {
      setError(err?.response?.data?.Mesaj || 'Randevu bulunamadı veya plaka eşleşmedi.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="mx-auto max-w-2xl px-4 py-16">
      <div className="text-center mb-10">
        <h1 className="text-3xl font-bold text-gray-800 mb-2">Randevu Durum Sorgulama</h1>
        <p className="text-gray-500 text-sm">
          Şifresiz hızlı işlem. Servisteki aracınızın durumunu anlık olarak takip edin.
        </p>
      </div>

      <div className="card p-6 bg-white border border-gray-200 shadow-sm rounded-2xl mb-8">
        <form onSubmit={handleSorgula} className="flex flex-col sm:flex-row gap-4">
          <div className="flex-1">
            <label className="block text-xs font-bold text-gray-600 mb-1 uppercase">Randevu No</label>
            <input
              type="number"
              className="w-full px-4 py-3 bg-gray-50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-brand-500 outline-none"
              placeholder="Örn: 145"
              value={form.id}
              onChange={(e) => setForm({ ...form, id: e.target.value })}
            />
          </div>
          <div className="flex-1">
            <label className="block text-xs font-bold text-gray-600 mb-1 uppercase">Araç Plakası</label>
            <input
              type="text"
              className="w-full px-4 py-3 bg-gray-50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-brand-500 outline-none uppercase"
              placeholder="Örn: 35ABC123"
              value={form.plaka}
              onChange={(e) => setForm({ ...form, plaka: e.target.value })}
            />
          </div>
          <div className="flex items-end">
            <button
              type="submit"
              disabled={loading}
              className="w-full sm:w-auto px-8 py-3 bg-brand-600 text-white font-bold rounded-xl hover:bg-brand-700 transition flex items-center justify-center gap-2"
            >
              <Search className="w-5 h-5" />
              {loading ? 'Sorgulanıyor...' : 'Sorgula'}
            </button>
          </div>
        </form>
        {error && (
          <div className="mt-4 p-3 bg-red-50 text-red-700 text-sm font-medium rounded-lg flex items-center gap-2">
            <AlertCircle className="w-5 h-5" /> {error}
          </div>
        )}
      </div>

      {durum && (
        <div className="card p-6 border border-brand-100 bg-brand-50/30 rounded-2xl animate-fade-in space-y-6">
          <div className="flex items-center justify-between border-b border-brand-100 pb-4">
            <div>
              <p className="text-xs font-bold text-gray-500 uppercase tracking-wider">Randevu Numarası</p>
              <h2 className="text-2xl font-black text-brand-700">#{form.id}</h2>
            </div>
            
            {/* Index.ts'den gelen dinamik renkli durum rozeti */}
            <div className={`px-4 py-1.5 rounded-full text-sm font-bold border ${RandevuDurumuColors[durum.Durum as number]}`}>
              {RandevuDurumuLabels[durum.Durum as number]}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1">
              <span className="flex items-center gap-1.5 text-xs font-bold text-gray-500 uppercase">
                <Car className="w-4 h-4 text-brand-500" /> Araç
              </span>
              <p className="font-semibold text-gray-800">{durum.AracPlaka}</p>
            </div>
            <div className="space-y-1">
              <span className="flex items-center gap-1.5 text-xs font-bold text-gray-500 uppercase">
                <Clock className="w-4 h-4 text-brand-500" /> Tarih
              </span>
              <p className="font-semibold text-gray-800">{new Date(durum.Tarih).toLocaleDateString('tr-TR')}</p>
            </div>
            <div className="space-y-1">
              <span className="flex items-center gap-1.5 text-xs font-bold text-gray-500 uppercase">
                <Wrench className="w-4 h-4 text-brand-500" /> Hizmet
              </span>
              <p className="font-semibold text-gray-800">{durum.HizmetAdi}</p>
            </div>
            <div className="space-y-1">
              <span className="flex items-center gap-1.5 text-xs font-bold text-gray-500 uppercase">
                <CheckCircle2 className="w-4 h-4 text-brand-500" /> Tutar
              </span>
              <p className="font-semibold text-gray-800">
                {durum.Durum >= 2 ? `${durum.ToplamTutar} ₺` : 'İşlem Sonunda Belli Olacak'}
              </p>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}