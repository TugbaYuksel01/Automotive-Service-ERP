import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Mail } from 'lucide-react';
import { authApi } from '@/services/api';
import { useToast } from '@/contexts/ToastContext';
import type { SifremiUnuttumDto } from '@/types';

export function ForgotPasswordPage() {
  const { showSuccess, showError } = useToast();
  const [form, setForm] = useState<SifremiUnuttumDto>({ Email: '' });
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const res = await authApi.sifremiUnuttum(form);
      showSuccess('Bağlantı gönderildi', (res.data as { Mesaj?: string })?.Mesaj || 'E-postanızı kontrol edin.');
    } catch (error: unknown) {
      showError('İşlem başarısız', error instanceof Error ? error.message : 'Tekrar deneyin.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="mx-auto max-w-md px-4 py-16">
      <h1 className="text-2xl font-bold mb-6 text-center">Şifremi Unuttum</h1>
      
      <form onSubmit={handleSubmit} className="card p-6 space-y-4">
        <div>
          <label className="label-text">Kayıtlı Email Adresiniz</label>
          <div className="relative mt-1">
            <Mail className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-ink-400" />
            <input
              name="Email"
              type="email"
              className="input-field pl-10"
              value={form.Email}
              onChange={(e) => setForm({ Email: e.target.value })}
              placeholder="ornek@email.com"
              required
            />
          </div>
        </div>
        <button type="submit" disabled={loading} className="btn-primary w-full">
          {loading ? 'Gönderiliyor...' : 'Sıfırlama Bağlantısı Gönder'}
        </button>
        
        <Link to="/giris" className="block text-center text-sm text-ink-500 hover:text-brand-600 mt-4">
          Giriş ekranına dön
        </Link>
      </form>
    </div>
  );
}
