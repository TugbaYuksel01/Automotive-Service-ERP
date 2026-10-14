import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { User, Mail, Lock, ArrowLeft } from 'lucide-react';
import { useAuth } from '@/contexts/AuthContext';
import { useToast } from '@/contexts/ToastContext';
import type { RegisterDto } from '@/types';

export function RegisterPage() {
  const { register } = useAuth();
  const { showSuccess, showError } = useToast();
  const navigate = useNavigate();
  const [form, setForm] = useState<RegisterDto>({
    AdSoyad: '',
    Email: '',
    Sifre: '',
    Rol: 'Musteri',
  });
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      await register({ ...form, Rol: 'Musteri' });
      showSuccess('Kayıt Başarılı', 'Hesabınız oluşturuldu, giriş yapabilirsiniz.');
      navigate('/giris');
    } catch (error: unknown) {
      showError('Kayıt Başarısız', error instanceof Error ? error.message : 'Bir hata oluştu, lütfen tekrar deneyin.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-[calc(100vh-4rem)] flex items-center justify-center px-4 py-12">
      <div className="w-full max-w-md">
        <h1 className="text-2xl font-bold text-ink-900 text-center mb-2">Aramıza Katılın</h1>
        <p className="text-sm text-ink-500 text-center mb-8">Aracınızın kontrolü artık sizin elinizde</p>
        
        <div className="card p-8">
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="label-text">Adınız Soyadınız</label>
              <div className="relative mt-1">
                <User className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-ink-400" />
                <input
                  name="AdSoyad"
                  className="input-field pl-10"
                  value={form.AdSoyad}
                  onChange={(e) => setForm({ ...form, AdSoyad: e.target.value })}
                  placeholder="Örn: Ali Yılmaz"
                  required
                />
              </div>
            </div>
            <div>
              <label className="label-text">E-posta Adresiniz</label>
              <div className="relative mt-1">
                <Mail className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-ink-400" />
                <input
                  name="Email"
                  type="email"
                  className="input-field pl-10"
                  value={form.Email}
                  onChange={(e) => setForm({ ...form, Email: e.target.value })}
                  placeholder="ornek@email.com"
                  required
                />
              </div>
            </div>
            <div>
              <label className="label-text">Şifre Belirleyin</label>
              <div className="relative mt-1">
                <Lock className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-ink-400" />
                <input
                  name="Sifre"
                  type="password"
                  minLength={6}
                  className="input-field pl-10"
                  value={form.Sifre}
                  onChange={(e) => setForm({ ...form, Sifre: e.target.value })}
                  placeholder="En az 6 karakter"
                  required
                />
              </div>
            </div>
            
            <button type="submit" disabled={loading} className="btn-primary w-full mt-6">
              {loading ? 'Hesap Oluşturuluyor...' : 'Ücretsiz Kayıt Ol'}
            </button>
          </form>
          <p className="mt-6 text-center text-sm text-ink-500">
            Zaten bir hesabınız var mı?{' '}
            <Link to="/giris" className="font-semibold text-brand-600 hover:text-brand-700">
              Giriş Yapın
            </Link>
          </p>
        </div>
        <div className="mt-6 text-center">
          <Link to="/" className="btn-ghost">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Ana sayfaya dön
          </Link>
        </div>
      </div>
    </div>
  );
}
