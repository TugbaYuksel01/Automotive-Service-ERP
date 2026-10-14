import { useState, useEffect } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { Mail, Lock, LogIn, ArrowLeft, Wrench, Cpu, Activity, Zap } from 'lucide-react';
import { useAuth } from '@/contexts/AuthContext';
import { useToast } from '@/contexts/ToastContext';
import { dashboardPath } from '@/lib/jwt';
import type { LoginDto } from '@/types';

export function LoginPage() {
  const { login, logout } = useAuth(); 
  const { showSuccess, showError } = useToast();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from;
  
  const [form, setForm] = useState<LoginDto>({ Email: '', Sifre: '' });
  const [loading, setLoading] = useState(false);

  // Sahte yapay zeka analiz logları (Matrix / Hacker efekti)
  const [logs, setLogs] = useState<string[]>([]);
  
  useEffect(() => {
    const aiMessages = [
      "Sistem başlatılıyor...",
      "RabbitMQ bağlantısı kuruldu.",
      "MassTransit tüketicileri (consumers) dinleniyor.",
      "Yapay Zeka Triage Motoru devrede.",
      "Groq API bağlantısı aktif.",
      "Araç arıza tespit modelleri yüklendi.",
      "OtoServis Pro sistemleri çevrimiçi."
    ];
    let currentIndex = 0;
    
    const interval = setInterval(() => {
      if (currentIndex < aiMessages.length) {
        setLogs(prev => [...prev, aiMessages[currentIndex]]);
        currentIndex++;
      } else {
        // Döngüye sokalım ki hareketlilik bitmesin
        setLogs([]);
        currentIndex = 0;
      }
    }, 1500);

    return () => clearInterval(interval);
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!form.Email.trim() || !form.Sifre.trim()) {
      showError('Eksik bilgi', 'Lütfen tüm alanları doldurun.');
      return;
    }
    
    setLoading(true);
    
    try {
      const session = await login(form);

      // --- MİSAFİR ENGELLEME (GÜVENLİK KALKANI) ---
      if (session.Rol === 'Misafir' || session.Rol === 'misafir') {
        if (logout) await logout(); 
        
        showError(
          'Kayıtlı Değilsiniz', 
          'Bu e-posta adresi ile daha önce sadece misafir randevusu oluşturulmuş. Sisteme giriş yapabilmek için lütfen "Kayıt Ol" sayfasından müşteri hesabınızı oluşturun.'
        );
        return; 
      }
      // --------------------------------------------

      showSuccess('Giriş başarılı', 'Hoş geldiniz.');
      navigate(from || dashboardPath(session.Rol), { replace: true });
      
    } catch (error: unknown) {
      showError(
        'Giriş başarısız', 
        error instanceof Error ? error.message : 'Bilgilerinizi kontrol edip tekrar deneyin.'
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-[calc(100vh-4rem)] flex items-center justify-center px-4 py-8 bg-ink-50">
      
      {/* 2 Sütunlu Ana Grid (Küçük ekranda alt alta, MD'de yan yana) */}
      <div className="w-full max-w-5xl grid md:grid-cols-2 gap-8 items-stretch">
        
        {/* SOL SÜTUN: Canlı Yapay Zeka Analiz Ekranı */}
        <div className="hidden md:flex flex-col bg-ink-950 rounded-2xl overflow-hidden shadow-2xl relative border border-ink-800">
          
          {/* Yanıp sönen "Canlı" göstergesi ve Başlık */}
          <div className="bg-black/40 p-4 border-b border-ink-800 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Cpu className="w-5 h-5 text-brand-400" />
              <h3 className="text-ink-200 font-bold text-sm tracking-widest uppercase">
                Yapay Zeka Triage Modülü
              </h3>
            </div>
            <div className="flex items-center gap-2 bg-ink-900 px-3 py-1 rounded-full border border-ink-800">
              <div className="w-2.5 h-2.5 rounded-full bg-green-500 animate-pulse shadow-[0_0_8px_rgba(34,197,94,0.8)]" />
              <span className="text-green-500 text-xs font-bold uppercase tracking-wider">Canlı</span>
            </div>
          </div>

          {/* Animasyonlu Orta Kısım */}
          <div className="flex-1 p-8 flex flex-col items-center justify-center relative">
             {/* Arka plan ızgara deseni */}
             <div className="absolute inset-0 opacity-[0.05]" style={{ backgroundImage: `url("data:image/svg+xml,%3Csvg width='20' height='20' xmlns='http://www.w3.org/2000/svg'%3E%3Ccircle cx='2' cy='2' r='2' fill='%23ffffff'/%3E%3C/svg%3E")` }} />
             
             {/* Merkezi Dönen İkonlar */}
             <div className="relative mb-8 z-10">
                <div className="absolute inset-0 bg-brand-500 blur-[40px] opacity-20 rounded-full animate-pulse" />
                <div className="relative bg-ink-900 border border-ink-700 p-6 rounded-full shadow-lg">
                  <Wrench className="w-12 h-12 text-brand-400 animate-[spin_4s_linear_infinite]" />
                  <Activity className="w-6 h-6 text-green-400 absolute top-0 right-0 animate-bounce" />
                  <Zap className="w-6 h-6 text-yellow-400 absolute bottom-0 left-0 animate-pulse" />
                </div>
             </div>

             <div className="text-center space-y-2 mb-8 relative z-10">
               <h2 className="text-2xl font-black text-white">OtoServis Pro AI</h2>
               <p className="text-ink-400 text-sm">Arıza teşhislerinizi otonom hale getiren teknoloji.</p>
             </div>

             {/* Kayan Matrix Log Ekranı */}
             <div className="w-full bg-black/60 border border-ink-800 rounded-lg p-4 h-40 overflow-hidden relative z-10">
               <div className="absolute inset-0 bg-gradient-to-b from-transparent to-black/90 pointer-events-none z-20" />
               <div className="space-y-2 font-mono text-xs relative z-10">
                 {logs.map((log, index) => (
                   <div key={index} className="text-green-400 flex items-start gap-2 animate-fade-in-up">
                     <span className="text-ink-600">&gt;</span>
                     <span className="opacity-90">{log}</span>
                   </div>
                 ))}
                 <div className="text-green-400 flex items-start gap-2 animate-pulse">
                   <span className="text-ink-600">&gt;</span>
                   <span className="w-2 h-3 bg-green-400 inline-block" />
                 </div>
               </div>
             </div>
          </div>
        </div>

        {/* SAĞ SÜTUN: Giriş Formu (Değişmedi) */}
        <div className="flex flex-col justify-center">
          <div className="flex flex-col items-center mb-8 md:hidden">
            <div className="flex items-center justify-center w-16 h-16 rounded-2xl bg-gradient-to-br from-brand-600 to-brand-800 shadow-xl shadow-brand-600/20 mb-4">
              <Wrench className="h-8 w-8 text-white" />
            </div>
            <h1 className="text-2xl font-bold text-ink-900">Giriş Yap</h1>
            <p className="text-sm text-ink-500 mt-1">Hesabınıza erişmek için bilgilerinizi girin</p>
          </div>

          <div className="card p-8 bg-white border border-ink-200 shadow-sm rounded-2xl">
            <div className="hidden md:flex flex-col items-center mb-8">
              <h1 className="text-2xl font-bold text-ink-900">Hoş Geldiniz</h1>
              <p className="text-sm text-ink-500 mt-1">Yönetim paneline erişmek için giriş yapın.</p>
            </div>

            <form onSubmit={handleSubmit} className="space-y-5">
              <div>
                <label className="label-text font-semibold text-ink-700">E-posta Adresi</label>
                <div className="relative mt-1">
                  <Mail className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-ink-400" />
                  <input
                    name="Email"
                    type="email"
                    className="input-field pl-10 w-full"
                    value={form.Email}
                    onChange={(e) => setForm({ ...form, Email: e.target.value })}
                    placeholder="ornek@email.com"
                    required
                  />
                </div>
              </div>
              <div>
                <label className="label-text font-semibold text-ink-700">Şifre</label>
                <div className="relative mt-1">
                  <Lock className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-ink-400" />
                  <input
                    name="Sifre"
                    type="password"
                    className="input-field pl-10 w-full"
                    value={form.Sifre}
                    onChange={(e) => setForm({ ...form, Sifre: e.target.value })}
                    placeholder="••••••••"
                    required
                  />
                </div>
              </div>
              <div className="text-right">
                <Link to="/sifremi-unuttum" className="text-sm font-bold text-brand-600 hover:text-brand-700 transition-colors">
                  Şifremi unuttum
                </Link>
              </div>
              <button type="submit" disabled={loading} className="btn-primary w-full py-3 rounded-xl font-bold">
                <LogIn className="h-5 w-5 mr-2" />
                {loading ? 'Sistem Doğrulanıyor...' : 'Sisteme Giriş Yap'}
              </button>
            </form>
            <div className="mt-8 pt-6 border-t border-ink-100 text-center text-sm text-ink-500">
              Sistemde kaydınız yok mu?{' '}
              <Link to="/kayit" className="font-bold text-brand-600 hover:text-brand-700 transition-colors">
                Müşteri Hesabı Oluştur
              </Link>
            </div>
          </div>
          
          <div className="mt-6 text-center">
            <Link to="/" className="inline-flex items-center text-sm font-semibold text-ink-500 hover:text-ink-700 transition-colors">
              <ArrowLeft className="h-4 w-4 mr-1" />
              Ana sayfaya dön
            </Link>
          </div>
        </div>

      </div>
    </div>
  );
}