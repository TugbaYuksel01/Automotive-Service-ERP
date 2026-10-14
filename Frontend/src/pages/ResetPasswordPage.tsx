import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { authApi } from '@/services/api';
import { useToast } from '@/contexts/ToastContext';

export function ResetPasswordPage() {
  const { showSuccess, showError } = useToast();
  const navigate = useNavigate();
  
  // URL'deki parametreleri (email ve token) yakalıyoruz
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');
  const email = searchParams.get('email');

  const [yeniSifre, setYeniSifre] = useState('');
  const [loading, setLoading] = useState(false);

  // Eğer URL'de token veya email yoksa, formu hiç gösterme
  if (!token || !email) {
    return (
      <div className="mx-auto max-w-md px-4 py-16 text-center">
        <div className="card p-6 space-y-4">
          <h1 className="text-xl font-bold text-red-600">Bağlantı Geçersiz</h1>
          <p className="text-sm text-ink-600">
            Güvenlik nedeniyle işlem yapılamıyor. Lütfen e-postanıza gelen bağlantıya tekrar tıklayın.
          </p>
          <Link to="/sifremi-unuttum" className="btn-primary w-full inline-block">
            Yeni Bağlantı İste
          </Link>
        </div>
      </div>
    );
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const res = await authApi.sifreSifirla({
        Email: email,
        Token: token,
        YeniSifre: yeniSifre
      });
      showSuccess('Şifre güncellendi', (res.data as { Mesaj?: string })?.Mesaj);
      navigate('/giris');
    } catch (error: unknown) {
      showError('Sıfırlama başarısız', error instanceof Error ? error.message : 'Bağlantı süresi dolmuş olabilir.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="mx-auto max-w-md px-4 py-16">
      <h1 className="text-2xl font-bold mb-6 text-center">Yeni Şifre Belirleme</h1>
      
      <form onSubmit={handleSubmit} className="card p-6 space-y-4">
        <div className="text-sm text-center text-ink-600 bg-blue-50 p-2 rounded">
          <strong>{email}</strong> hesabı için yeni şifrenizi giriniz.
        </div>

        <div>
          <label className="label-text">Yeni Şifreniz</label>
          <input
            name="YeniSifre"
            type="password"
            minLength={6}
            className="input-field"
            value={yeniSifre}
            onChange={(e) => setYeniSifre(e.target.value)}
            placeholder="En az 6 karakter"
            required
          />
        </div>
        
        <button type="submit" disabled={loading} className="btn-primary w-full">
          {loading ? 'Kaydediliyor...' : 'Yeni şifreyi kaydet'}
        </button>
      </form>
    </div>
  );
}