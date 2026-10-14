import { Wrench, Mail, Phone, MapPin } from 'lucide-react';
import { Link } from 'react-router-dom';

export function Footer() {
  return (
    <footer className="bg-ink-950 text-ink-300 mt-20">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-12">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-8">
          {/* Brand */}
          <div className="md:col-span-1">
            <div className="flex items-center gap-2 mb-4">
              <div className="flex items-center justify-center w-10 h-10 rounded-xl bg-brand-600">
                <Wrench className="h-5 w-5 text-white" />
              </div>
              <span className="font-display font-bold text-white text-lg">OtoServis</span>
            </div>
            <p className="text-sm text-ink-400 leading-relaxed">
              Aracınız için akıllı bakım platformu. Yapay zeka destekli analiz, uzman ustalar ve hızlı servis hizmetleri.
            </p>
          </div>

          {/* Links */}
          <div>
            <h4 className="text-sm font-semibold text-white mb-4">Sayfalar</h4>
            <ul className="space-y-2 text-sm">
              <li><Link to="/" className="hover:text-brand-400 transition-colors">Ana Sayfa</Link></li>
              <li><Link to="/hizmetler" className="hover:text-brand-400 transition-colors">Hizmetler</Link></li>
              <li><Link to="/randevu" className="hover:text-brand-400 transition-colors">Randevu Al</Link></li>
              <li><Link to="/giris" className="hover:text-brand-400 transition-colors">Giriş Yap</Link></li>
            </ul>
          </div>

          {/* Services */}
          <div>
            <h4 className="text-sm font-semibold text-white mb-4">Hizmetler</h4>
            <ul className="space-y-2 text-sm text-ink-400">
              <li>Periyodik Bakım</li>
              <li>Motor Servisi</li>
              <li>Fren Sistemleri</li>
              <li>Boya & Kaporta</li>
              <li>Diyagnostik Analiz</li>
            </ul>
          </div>

          {/* Contact */}
          <div>
            <h4 className="text-sm font-semibold text-white mb-4">İletişim</h4>
            <ul className="space-y-3 text-sm">
              <li className="flex items-center gap-2">
                <Phone className="h-4 w-4 text-brand-400" />
                <span>0850 123 45 67</span>
              </li>
              <li className="flex items-center gap-2">
                <Mail className="h-4 w-4 text-brand-400" />
                <span>destek@otoservis.com</span>
              </li>
              <li className="flex items-center gap-2">
                <MapPin className="h-4 w-4 text-brand-400" />
                <span>Sanayi Mah. Servis Cad. No:1</span>
              </li>
            </ul>
          </div>
        </div>

        <div className="mt-10 pt-6 border-t border-ink-800 flex flex-col sm:flex-row items-center justify-between gap-4">
          <p className="text-xs text-ink-500">© 2026 OtoServis. Tüm hakları saklıdır.</p>
          <p className="text-xs text-ink-500">Yapay Zeka Destekli Servis Platformu</p>
        </div>
      </div>
    </footer>
  );
}
