import { Wrench, Calendar, LayoutGrid, LogOut, LogIn, User, Menu, X, Shield, Search } from 'lucide-react';import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { dashboardPath } from '@/lib/jwt';

export function Navbar() {
  const { user, logout, isAuthenticated } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [scrolled, setScrolled] = useState(false);

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 8);
    window.addEventListener('scroll', onScroll);
    return () => window.removeEventListener('scroll', onScroll);
  }, []);

  useEffect(() => {
    setMobileOpen(false);
  }, [location.pathname]);

  const handleLogout = async () => {
    await logout();
    navigate('/');
  };

  // --- KESİN VE NET ROL KONTROLLERİ ---
  // 1. Ziyaretçi: Sisteme henüz giriş yapmamış herkes (Senin istediğin sade ekran)
  const isGuest = !isAuthenticated;
  
  // 2. Kayıtlı Müşteri: Giriş yapmış ve rolü Musteri olanlar (Ekran görüntüsündeki halin)
  const isMusteri = isAuthenticated && user?.Rol === 'Musteri';
  
  // 3. Personel: Admin, Usta, Vezne vb.
  const isStaff = isAuthenticated && !isMusteri;
  
  const panel = dashboardPath(user?.Rol);

  const isActive = (path: string) =>
    path === '/' ? location.pathname === '/' : location.pathname.startsWith(path);

  // Linkler için ortak stil tanımları
  const linkBaseClass = "flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium transition-all";
  const mobileLinkBaseClass = "flex items-center gap-2 rounded-lg px-3 py-2.5 text-sm font-medium transition-all";
  const activeClass = "bg-brand-50 text-brand-700";
  const inactiveClass = "text-ink-600 hover:bg-ink-100 hover:text-ink-900";

  return (
    <header
      className={`fixed top-0 left-0 right-0 z-50 transition-all duration-300 ${
        scrolled ? 'glass shadow-sm border-b border-ink-200/60' : 'bg-white border-b border-ink-100'
      }`}
    >
      <nav className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex h-16 items-center justify-between">
          
          {/* SOL: LOGO (Tıklanınca Ana Sayfaya Gider) */}
          <Link to="/" className="flex items-center gap-2 group">
            <div className="flex items-center justify-center w-10 h-10 rounded-xl bg-gradient-to-br from-brand-600 to-brand-800 shadow-lg shadow-brand-600/20 group-hover:scale-105 transition-transform">
              <Wrench className="h-5 w-5 text-white" />
            </div>
            <div className="hidden sm:block">
              <span className="font-display font-bold text-ink-900 text-lg leading-none">OtoServis</span>
              <span className="block text-xs text-ink-500 leading-none mt-0.5">Akıllı Bakım Platformu</span>
            </div>
          </Link>

          {/* ORTA: DİNAMİK LİNKLER (MASAÜSTÜ) */}
          <div className="hidden md:flex items-center gap-1">
            
            {/* Sadece Ziyaretçiler Görebilir (Sade Görünüm) */}
            {isGuest && (
              <>
                <Link to="/hizmetler" className={`${linkBaseClass} ${isActive('/hizmetler') ? activeClass : inactiveClass}`}>
                  <Wrench className="h-4 w-4" /> Hizmetler
                </Link>
                <Link to="/randevu" className={`${linkBaseClass} ${isActive('/randevu') ? activeClass : inactiveClass}`}>
                  <Calendar className="h-4 w-4" /> Misafir Randevu Al
                </Link>
                <Link to="/takip" className={`${linkBaseClass} ${isActive('/takip') ? activeClass : inactiveClass}`}>
                  <Search className="h-4 w-4" /> Randevu Sorgula
                </Link>
              </>
            )}

            {/* Sadece Kayıtlı Müşteriler Görebilir (Ekran görüntüsündeki gibi) */}
            {isMusteri && (
              <>
                <Link to="/" className={`${linkBaseClass} ${isActive('/') ? activeClass : inactiveClass}`}>
                  <LayoutGrid className="h-4 w-4" /> Ana Sayfa
                </Link>
                <Link to="/hizmetler" className={`${linkBaseClass} ${isActive('/hizmetler') ? activeClass : inactiveClass}`}>
                  <Wrench className="h-4 w-4" /> Hizmetler
                </Link>
                <Link to="/randevu-olustur" className={`${linkBaseClass} ${isActive('/randevu-olustur') ? activeClass : inactiveClass}`}>
                  <Calendar className="h-4 w-4" /> Randevu Al
                </Link>
                <Link to="/randevularim" className={`${linkBaseClass} ${isActive('/randevularim') ? activeClass : inactiveClass}`}>
                  <Calendar className="h-4 w-4" /> Randevularım
                </Link>
              </>
            )}

            {/* Sadece Personeller Görebilir */}
            {isStaff && (
              <Link to={panel} className={`${linkBaseClass} ${isActive(panel) ? activeClass : inactiveClass}`}>
                <Shield className="h-4 w-4" /> Yönetim Paneli
              </Link>
            )}
          </div>

          {/* SAĞ: GİRİŞ / PROFİL BÖLÜMÜ (MASAÜSTÜ) */}
          <div className="hidden md:flex items-center gap-2">
            {(isMusteri || isStaff) ? (
              <>
                <Link to="/profil" className="flex items-center gap-2 px-3 py-1.5 rounded-lg bg-ink-100 hover:bg-ink-200 transition-colors cursor-pointer">
                  <div className="flex items-center justify-center w-7 h-7 rounded-full bg-brand-600 text-white text-xs font-semibold shadow-sm">
                    {(user?.AdSoyad || user?.Rol || 'U').charAt(0).toUpperCase()}
                  </div>
                  <span className="text-sm font-medium text-ink-700 max-w-[140px] truncate">
                    {user?.AdSoyad || user?.Rol}
                  </span>
                </Link>
                <button onClick={handleLogout} className="btn-ghost text-error-600 hover:bg-error-50 hover:text-error-700">
                  <LogOut className="h-4 w-4" /> Çıkış
                </button>
              </>
            ) : (
              /* Ziyaretçi İçin Sadece Giriş Yap ve Kayıt Ol Butonları */
              <>
                <Link to="/giris" className="btn-ghost">
                  <LogIn className="h-4 w-4" /> Giriş Yap
                </Link>
                <Link to="/kayit" className="btn-primary">
                  <User className="h-4 w-4" /> Kayıt Ol
                </Link>
              </>
            )}
          </div>

          {/* MOBİL MENÜ BUTONU */}
          <button
            className="md:hidden p-2 rounded-lg text-ink-700 hover:bg-ink-100"
            onClick={() => setMobileOpen((v) => !v)}
          >
            {mobileOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
          </button>
        </div>

        {/* MOBİL AÇILIR MENÜ */}
        {mobileOpen && (
          <div className="md:hidden pb-4 animate-fade-in">
            <div className="flex flex-col gap-1">
              
              {isGuest && (
                <>
                  <Link to="/hizmetler" className={`${mobileLinkBaseClass} ${isActive('/hizmetler') ? activeClass : inactiveClass}`}>
                    <Wrench className="h-4 w-4" /> Hizmetler
                  </Link>
                  <Link to="/randevu" className={`${mobileLinkBaseClass} ${isActive('/randevu') ? activeClass : inactiveClass}`}>
                    <Calendar className="h-4 w-4" /> Misafir Randevu Al
                  </Link>
                </>
              )}

              {isMusteri && (
                <>
                  <Link to="/" className={`${mobileLinkBaseClass} ${isActive('/') ? activeClass : inactiveClass}`}>
                    <LayoutGrid className="h-4 w-4" /> Ana Sayfa
                  </Link>
                  <Link to="/hizmetler" className={`${mobileLinkBaseClass} ${isActive('/hizmetler') ? activeClass : inactiveClass}`}>
                    <Wrench className="h-4 w-4" /> Hizmetler
                  </Link>
                  <Link to="/randevu-olustur" className={`${mobileLinkBaseClass} ${isActive('/randevu-olustur') ? activeClass : inactiveClass}`}>
                    <Calendar className="h-4 w-4" /> Randevu Al
                  </Link>
                  <Link to="/randevularim" className={`${mobileLinkBaseClass} ${isActive('/randevularim') ? activeClass : inactiveClass}`}>
                    <Calendar className="h-4 w-4" /> Randevularım
                  </Link>
                  <Link to="/profil" className="text-gray-600 hover:text-blue-600 font-medium">
                    Profilim
                  </Link>
                </>
              )}

              {isStaff && (
                <Link to={panel} className={`${mobileLinkBaseClass} ${isActive(panel) ? activeClass : inactiveClass}`}>
                  <Shield className="h-4 w-4" /> Yönetim Paneli
                </Link>
              )}

              <div className="mt-2 pt-2 border-t border-ink-200">
                {(isMusteri || isStaff) ? (
                  <>
                    <Link to="/profil" className="w-full btn-ghost justify-start mb-1 text-ink-700">
                      <User className="h-4 w-4" /> Profilim
                    </Link>
                    <button onClick={handleLogout} className="w-full btn-ghost justify-start text-error-600 hover:bg-error-50">
                      <LogOut className="h-4 w-4" /> Çıkış Yap
                    </button>
                  </>
                ) : (
                  <div className="flex gap-2 px-1">
                    <Link to="/giris" className="flex-1 btn-secondary justify-center">Giriş Yap</Link>
                    <Link to="/kayit" className="flex-1 btn-primary justify-center">Kayıt Ol</Link>
                  </div>
                )}
              </div>
            </div>
          </div>
        )}
      </nav>
    </header>
  );
}