import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Wrench,
  Calendar,
  Sparkles,
  ShieldCheck,
  Clock,
  TrendingUp,
  Car,
  CheckCircle2,
  ArrowRight,
  Cpu,
  Zap,
} from 'lucide-react';
import { hizmetApi } from '@/services/api';
import type { HizmetTipi } from '@/types';
import { LoadingSpinner } from '@/components/Loading';
import { useToast } from '@/contexts/ToastContext';

export function LandingPage() {
  const [hizmetler, setHizmetler] = useState<HizmetTipi[]>([]);
  const [loading, setLoading] = useState(true);
  const { showWarning } = useToast();

  // 1. API Çağrısı: Sayfa yüklendiğinde hizmetleri getirir
  useEffect(() => {
    hizmetApi
      .listele()
      .then(setHizmetler)
      .catch(() => setHizmetler([]))
      .finally(() => setLoading(false));
  }, []);

  // 2. Demo Uyarısı: Sadece ilk girişte toast bildirimini tetikler
  useEffect(() => {
    const uyariGosterildi = sessionStorage.getItem('demoUyari');
    if (!uyariGosterildi) {
      showWarning(
        '⚠️ Demo Sistem Uyarısı',
        'Bu site bir portfolyo / test ortamıdır. Lütfen gerçek kişisel bilgilerinizi, telefonunuzu veya plakanızı girmeyiniz!'
      );
      sessionStorage.setItem('demoUyari', 'true');
    }
  }, [showWarning]);

  return (
    <div>
      {/* Hero */}
      <section className="relative overflow-hidden bg-gradient-to-br from-ink-950 via-ink-900 to-brand-950">
        {/* Grid pattern overlay */}
        <div
          className="absolute inset-0 opacity-[0.07]"
          style={{
            backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' xmlns='http://www.w3.org/2000/svg'%3E%3Cpath d='M0 0h60v60H0z' fill='none' stroke='%23ffffff' stroke-width='0.5'/%3E%3C/svg%3E")`,
          }}
        />
        {/* Glow orbs */}
        <div className="absolute top-1/4 -left-20 w-72 h-72 bg-brand-600/30 rounded-full blur-3xl" />
        <div className="absolute bottom-0 right-0 w-96 h-96 bg-accent-500/20 rounded-full blur-3xl" />

        <div className="relative mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 pt-20 pb-24 sm:pt-28 sm:pb-32">
          
          {/* DÜZELTME BURADA: lg:grid-cols-2 yerine md:grid-cols-2 kullandık! Böylece ekranın küçülse bile o boşluk dolacak ve 2 sütun kalacak. */}
          <div className="grid md:grid-cols-2 gap-8 lg:gap-12 items-center">
            
            {/* SOL TARAF: Yazılar */}
            <div>
              {/* Badge */}
              <div className="inline-flex items-center gap-2 rounded-full bg-white/10 backdrop-blur-sm border border-white/20 px-3 py-1.5 mb-6">
                <Sparkles className="h-3.5 w-3.5 text-accent-400" />
                <span className="text-xs font-medium text-white/90">Yapay Zeka Destekli Servis</span>
              </div>

              <h1 className="font-display text-4xl sm:text-5xl lg:text-6xl font-bold text-white leading-[1.1] text-balance">
                Aracınızın Bakımı
                <span className="block bg-gradient-to-r from-brand-400 to-accent-400 bg-clip-text text-transparent">
                  Akıllı Ellerde
                </span>
              </h1>

              <p className="mt-6 text-lg text-ink-300 leading-relaxed max-w-xl">
                Online randevu alın, uzman ustalarımız aracınızı incelesin. Yapay zeka destekli analiz ile aracınızın durumu hakkında anlık bilgilendirme alın.
              </p>

              <div className="mt-8 flex flex-wrap gap-3">
                <Link to="/randevu" className="btn-accent">
                  <Calendar className="h-5 w-5" />
                  Hemen Randevu Al
                </Link>
                <Link to="/hizmetler" className="inline-flex items-center justify-center gap-2 rounded-xl border border-white/20 bg-white/5 backdrop-blur-sm px-5 py-3 text-sm font-semibold text-white transition-all hover:bg-white/10">
                  Hizmetleri Keşfet
                  <ArrowRight className="h-4 w-4" />
                </Link>
              </div>

              {/* Stats */}
              <div className="mt-12 grid grid-cols-3 gap-6">
                {[
                  { value: '10K+', label: 'Mutlu Müşteri' },
                  { value: '25+', label: 'Uzman Usta' },
                  { value: '%99', label: 'Memnuniyet' },
                ].map((stat) => (
                  <div key={stat.label}>
                    <p className="text-2xl sm:text-3xl font-bold text-white">{stat.value}</p>
                    <p className="text-xs text-ink-400 mt-1">{stat.label}</p>
                  </div>
                ))}
              </div>
            </div>

            {/* SAĞ TARAF: Fotoğraf ve AI Modülü */}
            {/* DÜZELTME BURADA: hidden lg:block yerine hidden md:block kullandık. Artık fotoğraf ekran görüntüsündeki sağ boşluğa tam oturacak! */}
            <div className="relative hidden md:block">
              <div className="absolute -inset-4 bg-gradient-to-br from-brand-600/20 to-accent-500/20 rounded-3xl blur-2xl" />
              <div className="relative rounded-3xl overflow-hidden shadow-2xl border border-white/10 animate-float">
                <img
                  src="https://images.pexels.com/photos/3807517/pexels-photo-3807517.jpeg?auto=compress&cs=tinysrgb&h=650&w=940"
                  alt="Oto Servis"
                  className="w-full h-[350px] lg:h-[420px] object-cover"
                />
                <div className="absolute inset-0 bg-gradient-to-t from-ink-950/80 to-transparent" />
                
                {/* Floating AI badge */}
                <div className="absolute bottom-4 left-4 right-4 glass rounded-xl p-4 border border-white/20">
                  <div className="flex items-center gap-3">
                    <div className="flex items-center justify-center w-10 h-10 rounded-lg bg-gradient-to-br from-brand-500 to-brand-700">
                      <Cpu className="h-5 w-5 text-white" />
                    </div>
                    <div className="flex-1">
                      <p className="text-sm font-semibold text-ink-900">AI Analiz Aktif</p>
                      <p className="text-xs text-ink-500">Aracınız anlık olarak analiz ediliyor</p>
                    </div>
                    <div className="flex items-center gap-1">
                      <div className="w-2 h-2 rounded-full bg-success-500 animate-pulse" />
                      <span className="text-xs text-success-600 font-medium">Canlı</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>

          </div>
        </div>

        {/* Wave separator */}
        <svg className="absolute bottom-0 left-0 right-0 w-full" viewBox="0 0 1440 80" fill="none" preserveAspectRatio="none">
          <path d="M0 80L60 72C120 64 240 48 360 42.7C480 37 600 43 720 48C840 53 960 56 1080 53.3C1200 51 1320 43 1380 39L1440 35V80H0Z" fill="#f6f7f9" />
        </svg>
      </section>

      {/* Features */}
      <section className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-20">
        <div className="text-center mb-12">
          <p className="text-sm font-semibold text-brand-600 uppercase tracking-wide mb-2">Neden Biz?</p>
          <h2 className="text-3xl sm:text-4xl font-bold text-ink-900">Akıllı Servis Deneyimi</h2>
        </div>

        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {[
            {
              icon: Sparkles,
              title: 'Yapay Zeka Analizi',
              desc: 'Aracınızın durumu AI tarafından analiz edilir, sonuçlar anında size bildirilir.',
              color: 'from-brand-500 to-brand-700',
            },
            {
              icon: Calendar,
              title: 'Kolay Randevu',
              desc: 'Misafir veya kayıtlı kullanıcı olarak saniyeler içinde randevu oluşturun.',
              color: 'from-accent-500 to-accent-700',
            },
            {
              icon: ShieldCheck,
              title: 'Güvenli Servis',
              desc: 'Uzman ustalar ve modern ekipmanlarla güvenle aracınızı emanet edin.',
              color: 'from-success-500 to-success-700',
            },
            {
              icon: Clock,
              title: 'Hızlı İşlem',
              desc: 'Önceden planlanmış servis süresi ile bekleme sürenizi minimize edin.',
              color: 'from-brand-600 to-brand-800',
            },
            {
              icon: TrendingUp,
              title: 'Şeffaf Fiyatlandırma',
              desc: 'Önceden görebileceğiniz net fiyatlar ile sürpriz maliyet yok.',
              color: 'from-accent-600 to-accent-800',
            },
            {
              icon: Zap,
              title: 'Anlık Bildirimler',
              desc: 'SignalR teknolojisi ile servis sürecini gerçek zamanlı takip edin.',
              color: 'from-success-600 to-success-800',
            },
          ].map((feature) => {
            const Icon = feature.icon;
            return (
              <div key={feature.title} className="card card-hover p-6">
                <div className={`flex items-center justify-center w-12 h-12 rounded-xl bg-gradient-to-br ${feature.color} mb-4 shadow-lg`}>
                  <Icon className="h-6 w-6 text-white" />
                </div>
                <h3 className="text-lg font-semibold text-ink-900 mb-2">{feature.title}</h3>
                <p className="text-sm text-ink-600 leading-relaxed">{feature.desc}</p>
              </div>
            );
          })}
        </div>
      </section>

      {/* Services preview */}
      <section className="bg-white py-20">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between mb-12 gap-4">
            <div>
              <p className="text-sm font-semibold text-brand-600 uppercase tracking-wide mb-2">Hizmetlerimiz</p>
              <h2 className="text-3xl sm:text-4xl font-bold text-ink-900">Servis Seçenekleri</h2>
            </div>
            <Link to="/hizmetler" className="btn-ghost">
              Tümünü Gör
              <ArrowRight className="h-4 w-4" />
            </Link>
          </div>

          {loading ? (
            <LoadingSpinner label="Hizmetler yükleniyor..." />
          ) : (
            <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-6">
              {hizmetler.slice(0, 6).map((hizmet) => (
                <div key={hizmet.Id} className="card card-hover p-6">
                  <div className="flex items-start justify-between mb-4">
                    <div className="flex items-center justify-center w-10 h-10 rounded-lg bg-brand-100">
                      <Wrench className="h-5 w-5 text-brand-600" />
                    </div>
                    <span className="text-lg font-bold text-ink-900">
                      {new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY', maximumFractionDigits: 0 }).format(hizmet.Fiyat)}
                    </span>
                  </div>
                  <h3 className="text-lg font-semibold text-ink-900 mb-1">{hizmet.Ad}</h3>
                  {hizmet.GerekenUzmanlik && (
                    <p className="text-xs text-ink-400 mb-3">Uzmanlık: {hizmet.GerekenUzmanlik}</p>
                  )}
                  <div className="flex items-center gap-4 text-xs text-ink-500">
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
          )}
        </div>
      </section>

      {/* How it works */}
      <section className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-20">
        <div className="text-center mb-12">
          <p className="text-sm font-semibold text-brand-600 uppercase tracking-wide mb-2">Nasıl Çalışır?</p>
          <h2 className="text-3xl sm:text-4xl font-bold text-ink-900">3 Basit Adım</h2>
        </div>

        <div className="grid md:grid-cols-3 gap-8">
          {[
            { step: '01', title: 'Randevu Oluştur', desc: 'Aracınızın bilgilerini girin, uygun hizmeti seçin ve tarih belirleyin.', icon: Calendar },
            { step: '02', title: 'Servis Süreci', desc: 'Uzman ustamız aracınızı inceler, yapay zeka analizi başlar.', icon: Wrench },
            { step: '03', title: 'Anlık Sonuç', desc: 'AI analiz sonuçları bildirim olarak ekranınıza gelir.', icon: CheckCircle2 },
          ].map((item) => {
            const Icon = item.icon;
            return (
              <div key={item.step} className="relative">
                <div className="card p-8">
                  <span className="text-4xl font-bold font-display text-brand-200">{item.step}</span>
                  <div className="flex items-center justify-center w-12 h-12 rounded-xl bg-gradient-to-br from-brand-600 to-brand-800 my-4 shadow-lg">
                    <Icon className="h-6 w-6 text-white" />
                  </div>
                  <h3 className="text-lg font-semibold text-ink-900 mb-2">{item.title}</h3>
                  <p className="text-sm text-ink-600 leading-relaxed">{item.desc}</p>
                </div>
              </div>
            );
          })}
        </div>
      </section>

      {/* CTA */}
      <section className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 pb-20">
        <div className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-brand-700 via-brand-800 to-ink-950 p-8 sm:p-12">
          <div className="absolute top-0 right-0 w-64 h-64 bg-accent-500/20 rounded-full blur-3xl" />
          <div className="absolute bottom-0 left-0 w-48 h-48 bg-brand-400/20 rounded-full blur-3xl" />
          <div className="relative flex flex-col sm:flex-row items-center justify-between gap-6">
            <div>
              <h2 className="text-2xl sm:text-3xl font-bold text-white mb-2">Aracınız için hemen randevu alın</h2>
              <p className="text-ink-300">Yapay zeka destekli analiz ile aracınızı güvende tutun.</p>
            </div>
            <Link to="/randevu" className="btn-accent whitespace-nowrap">
              <Calendar className="h-5 w-5" />
              Randevu Al
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}