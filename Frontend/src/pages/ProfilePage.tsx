import { useEffect, useState } from 'react';
import { useToast } from '@/contexts/ToastContext';
import { User, Mail, Phone, Lock, ShieldCheck } from 'lucide-react';
// api.ts içine profil endpointini eklediğini varsayıyorum (profilApi.getir() ve profilApi.guncelle())
import { profilApi } from '@/services/api'; 

export function ProfilPage() {
  const { showSuccess, showError } = useToast();
  const [loading, setLoading] = useState(true);
  
  const [profil, setProfil] = useState({
    AdSoyad: '',
    Email: '',
    Telefon: '',
    Rol: '',
    YeniSifre: '' // Sadece güncelleme için
  });

  // Sayfa açıldığında mevcut bilgileri getir
  useEffect(() => {
    profilApi.getir()
      .then((res: any) => {
        setProfil({
          ...res.data,
          YeniSifre: '' // Şifre alanını boş bırakıyoruz
        });
        setLoading(false);
      })
      .catch(() => {
        showError('Hata', 'Profil bilgileri yüklenemedi.');
        setLoading(false);
      });
  }, []);

  const handleKaydet = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await profilApi.guncelle({
        AdSoyad: profil.AdSoyad,
        Email: profil.Email,
        Telefon: profil.Telefon,
        YeniSifre: profil.YeniSifre
      });
      showSuccess('Başarılı', 'Profil bilgileriniz güncellendi.');
      // Güncelleme sonrası şifre kutusunu tekrar temizle
      setProfil(prev => ({ ...prev, YeniSifre: '' }));
    } catch (error: any) {
      showError('Hata', error?.response?.data || 'Güncelleme başarısız oldu.');
    }
  };

  if (loading) return <div className="text-center py-10">Bilgiler yükleniyor...</div>;

  return (
    <div className="mx-auto max-w-2xl px-4 py-8 space-y-8">
      
      {/* ÜST BİLGİ KARTI */}
      <div className="bg-white p-6 rounded-2xl shadow-sm border border-gray-200 flex items-center gap-6">
        <div className="w-20 h-20 bg-indigo-100 rounded-full flex items-center justify-center flex-shrink-0">
          <User className="w-10 h-10 text-indigo-600" />
        </div>
        <div>
          <h1 className="text-2xl font-bold text-gray-800">{profil.AdSoyad}</h1>
          <div className="flex items-center gap-2 text-sm mt-1">
            <ShieldCheck className={`w-4 h-4 ${profil.Rol === 'Usta' ? 'text-amber-500' : 'text-green-500'}`} />
            <span className="font-semibold text-gray-600">Sistem Yetkisi: {profil.Rol}</span>
          </div>
        </div>
      </div>

      {/* GÜNCELLEME FORMU */}
      <form onSubmit={handleKaydet} className="bg-white p-8 rounded-2xl shadow-sm border border-gray-200 space-y-6">
        <h2 className="text-lg font-bold text-gray-700 border-b pb-2">Kişisel Bilgileriniz</h2>

        <div className="space-y-4">
          
          {/* Ad Soyad */}
          <div>
            <label className="block text-sm font-bold text-gray-700 mb-1">Ad Soyad</label>
            <div className="relative">
              <User className="absolute left-3 top-3 text-gray-400 w-5 h-5" />
              <input 
                type="text" 
                className="w-full pl-10 pr-4 py-2.5 border border-gray-300 rounded-xl focus:ring-2 focus:ring-indigo-500 outline-none"
                value={profil.AdSoyad}
                onChange={(e) => setProfil({...profil, AdSoyad: e.target.value})}
                required
              />
            </div>
          </div>

          {/* E-Posta */}
          <div>
            <label className="block text-sm font-bold text-gray-700 mb-1">E-Posta Adresi</label>
            <div className="relative">
              <Mail className="absolute left-3 top-3 text-gray-400 w-5 h-5" />
              <input 
                type="email" 
                className="w-full pl-10 pr-4 py-2.5 border border-gray-300 rounded-xl focus:ring-2 focus:ring-indigo-500 outline-none"
                value={profil.Email}
                onChange={(e) => setProfil({...profil, Email: e.target.value})}
                required
              />
            </div>
          </div>

          {/* Telefon */}
          <div>
            <label className="block text-sm font-bold text-gray-700 mb-1">Telefon Numarası</label>
            <div className="relative">
              <Phone className="absolute left-3 top-3 text-gray-400 w-5 h-5" />
              <input 
                type="tel" 
                className="w-full pl-10 pr-4 py-2.5 border border-gray-300 rounded-xl focus:ring-2 focus:ring-indigo-500 outline-none"
                value={profil.Telefon || ''}
                onChange={(e) => setProfil({...profil, Telefon: e.target.value})}
                placeholder="Örn: 0555 123 4567"
              />
            </div>
          </div>

          <div className="border-t border-gray-100 my-4 pt-4"></div>
          <h2 className="text-lg font-bold text-gray-700 mb-2">Güvenlik</h2>

          {/* Şifre Değiştirme */}
          <div>
            <label className="block text-sm font-bold text-gray-700 mb-1">Yeni Şifre Belirle</label>
            <p className="text-xs text-gray-500 mb-2">Şifrenizi değiştirmek istemiyorsanız bu alanı boş bırakabilirsiniz.</p>
            <div className="relative">
              <Lock className="absolute left-3 top-3 text-gray-400 w-5 h-5" />
              <input 
                type="password" 
                className="w-full pl-10 pr-4 py-2.5 border border-gray-300 rounded-xl focus:ring-2 focus:ring-indigo-500 outline-none"
                placeholder="Yeni şifrenizi giriniz..."
                value={profil.YeniSifre}
                onChange={(e) => setProfil({...profil, YeniSifre: e.target.value})}
              />
            </div>
          </div>

        </div>

        <button 
          type="submit" 
          className="w-full mt-8 py-3.5 bg-indigo-600 text-white font-bold rounded-xl hover:bg-indigo-700 transition-colors shadow-md"
        >
          Bilgilerimi Kaydet ve Güncelle
        </button>
      </form>
    </div>
  );
}