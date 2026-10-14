import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Wrench, Clock, Car, TrendingUp, CheckCircle2, ShieldCheck } from 'lucide-react';
import { hizmetApi, ustaApi } from '@/services/api';
import type { HizmetTipi, Usta } from '@/types';
import { LoadingSpinner } from '@/components/Loading';
import { ErrorState } from '@/components/States';

export function ServicesPage() {
  const [hizmetler, setHizmetler] = useState<HizmetTipi[]>([]);
  const [ustalar, setUstalar] = useState<Usta[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadData = () => {
    setLoading(true);
    setError(null);
    Promise.all([hizmetApi.listele(), ustaApi.listele()])
      .then(([h, u]) => {
        setHizmetler(h);
        setUstalar(u);
      })
      .catch((err) => setError(err.message || 'Hizmetler yüklenemedi'))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    loadData();
  }, []);

  if (loading) return <LoadingSpinner label="Hizmetler yükleniyor..." fullPage />;
  if (error) return <ErrorState message={error} onRetry={loadData} />;

  const fmtPrice = (price: number) =>
    new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY', maximumFractionDigits: 0 }).format(price);

  return (
    <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-12">
      <div className="mb-10">
        <p className="text-sm font-semibold text-brand-600 uppercase tracking-wide mb-2">Hizmetler</p>
        <h1 className="text-3xl sm:text-4xl font-bold text-ink-900">Servis Kataloğu</h1>
        
      </div>

      <section className="mb-16">
        <div className="flex items-center gap-2 mb-6">
          <Wrench className="h-5 w-5 text-brand-600" />
          <h2 className="text-xl font-semibold text-ink-900">Hizmet Türleri</h2>
        </div>
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {hizmetler.map((hizmet) => (
            <div key={hizmet.Id} className="card card-hover p-6">
              <div className="flex items-start justify-between mb-4">
                <div className="flex items-center justify-center w-12 h-12 rounded-xl bg-gradient-to-br from-brand-500 to-brand-700 shadow-lg">
                  <Wrench className="h-6 w-6 text-white" />
                </div>
                <span className="text-xl font-bold text-ink-900">{fmtPrice(hizmet.Fiyat)}</span>
              </div>
              <h3 className="text-lg font-semibold text-ink-900 mb-2">{hizmet.Ad}</h3>
              {hizmet.GerekenUzmanlik && (
                <div className="inline-flex items-center gap-1 rounded-md bg-accent-50 text-accent-700 text-xs font-medium px-2 py-1 mb-3">
                  <ShieldCheck className="h-3 w-3" />
                  {hizmet.GerekenUzmanlik}
                </div>
              )}
              <div className="flex items-center gap-4 text-xs text-ink-500 pt-3 border-t border-ink-100">
                <span className="flex items-center gap-1">
                  <Clock className="h-3.5 w-3.5" />
                  Usta: {hizmet.UstaSureDakika} dk
                </span>
                <span className="flex items-center gap-1">
                  <Car className="h-3.5 w-3.5" />
                  Lift: {hizmet.LiftSureDakika} dk
                </span>
              </div>
            </div>
          ))}
        </div>
      </section>

     <section className="mb-16">
        <div className="flex items-center gap-2 mb-6">
          <CheckCircle2 className="h-5 w-5 text-brand-600" />
          <h2 className="text-xl font-semibold text-ink-900">Uzman Ustalarımız</h2>
        </div>
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {ustalar.map((usta) => (
            <div key={usta.Id} className="card p-5 flex items-start gap-4 border border-ink-100 shadow-sm">
              <div className="flex items-center justify-center w-14 h-14 rounded-2xl bg-gradient-to-br from-brand-600 to-brand-800 text-white font-bold text-xl flex-shrink-0 shadow-md">
                {usta.AdSoyad?.charAt(0).toUpperCase() ?? '?'}
              </div>
              <div className="min-w-0 flex-1">
                <div className="flex items-center justify-between">
                  <h3 className="text-base font-bold text-ink-900 truncate">{usta.AdSoyad}</h3>
                  <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-semibold bg-green-50 text-green-700 border border-green-200">
                    Aktif Usta
                  </span>
                </div>
                <p className="text-xs font-semibold text-brand-600 mt-0.5">{usta.UzmanlikAlani}</p>
                
                {/* Sanayi tarzı güven veren alt bilgi */}
                <div className="mt-3 pt-3 border-t border-ink-100 flex items-center justify-between text-xs text-ink-500">
                  <span>Deneyim: 15+ Yıl</span>
                  <span className="text-brand-700 font-medium">OtoServis Pro Uzmanı</span>
                </div>
              </div>
            </div>
          ))}
        </div>
      </section>

      <div className="card p-8 text-center bg-gradient-to-br from-brand-50 to-white">
        <TrendingUp className="h-8 w-8 text-brand-600 mx-auto mb-3" />
        <h2 className="text-xl font-semibold text-ink-900 mb-2">Hizmet seçtiniz mi?</h2>
        <Link to="/randevu" className="btn-primary">
          Misafir randevu al
        </Link>
      </div>
    </div>
  );
}
