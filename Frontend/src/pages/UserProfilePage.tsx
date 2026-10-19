import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { User, Mail, Phone, Lock, Shield, Save, Activity } from 'lucide-react';

export default function UserProfilePage() {
  const [formData, setFormData] = useState({
    adSoyad: '',
    email: '',
    telefon: ''
  });
  
  // Şifre değiştirme state'leri (Gelecek için hazır)
  const [passwordData, setPasswordData] = useState({
    mevcutSifre: '',
    yeniSifre: '',
    yeniSifreTekrar: ''
  });

  const [mesaj, setMesaj] = useState<{ tip: 'success' | 'error', metin: string } | null>(null);
  const [yukleniyor, setYukleniyor] = useState(true);

  useEffect(() => {
    const fetchProfil = async () => {
      try {
        const token = sessionStorage.getItem('token');
        const response = await axios.get('http://otoservisapi.somee.com/api/Auth/profil', {
          headers: { Authorization: `Bearer ${token}` }
        });
        setFormData(response.data);
      } catch (error) {
        console.error('Profil bilgileri alınamadı', error);
      } finally {
        setYukleniyor(false);
      }
    };
    fetchProfil();
  }, []);

  const handleProfilGuncelle = async (e: React.FormEvent) => {
    e.preventDefault();
    setMesaj(null);
    try {
      const token = sessionStorage.getItem('token');
      const response = await axios.put('http://otoservisapi.somee.com/api/Auth/profil-guncelle', formData, {
        headers: { Authorization: `Bearer ${token}` }
      });
      setMesaj({ tip: 'success', metin: response.data.mesaj || 'Profil bilgileriniz başarıyla güncellendi!' });
      
      // Mesajı 3 saniye sonra gizle
      setTimeout(() => setMesaj(null), 3000);
    } catch (error: any) {
      setMesaj({ tip: 'error', metin: error.response?.data?.mesaj || 'Güncelleme sırasında bir hata oluştu.' });
    }
  };

  const handleSifreGuncelle = async (e: React.FormEvent) => {
  e.preventDefault();
  setMesaj(null);
  try {
    const token = sessionStorage.getItem('token');
    const response = await axios.put('http://otoservisapi.somee.com/api/Auth/sifre-guncelle', passwordData, {
      headers: { Authorization: `Bearer ${token}` }
    });
    
    setMesaj({ tip: 'success', metin: response.data.mesaj });
    // Başarılı olunca içindeki şifreleri temizleyelim ki ekranda kalmasın
    setPasswordData({ mevcutSifre: '', yeniSifre: '', yeniSifreTekrar: '' }); 
    
    setTimeout(() => setMesaj(null), 3000);
  } catch (error: any) {
    setMesaj({ tip: 'error', metin: error.response?.data?.mesaj || 'Şifre güncellenirken bir hata oluştu.' });
  }
};

  if (yukleniyor) {
    return (
      <div className="flex justify-center items-center h-[60vh]">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  // --- YARDIMCI DEĞİŞKENLER VE FONKSİYONLAR ---
  // İsim Formatlayıcı (Her kelimenin ilk harfini büyük yapar)
  const formatAdSoyad = (isim: string) => {
    if (!isim) return 'Kullanıcı';
    return isim.split(' ').map(kelime => kelime.charAt(0).toUpperCase() + kelime.slice(1).toLowerCase()).join(' ');
  };

  const gosterilenAd = formatAdSoyad(formData.adSoyad);
  const basHarf = formData.adSoyad ? formData.adSoyad.charAt(0).toUpperCase() : 'U';

  // --- ANA EKRAN (RENDER) ---
  return (
    <div className="max-w-5xl mx-auto mt-8 mb-20 px-4 sm:px-6 lg:px-8">
      
      {/* Üst Başlık ve Avatar Bölümü */}
      <div className="flex items-center gap-6 mb-8 bg-white p-6 rounded-2xl shadow-sm border border-gray-100">
        {/* Avatar Kutusu */}
        <div className="flex items-center justify-center w-20 h-20 rounded-full bg-gradient-to-br from-blue-600 to-indigo-700 text-white text-4xl font-extrabold shadow-lg">
          {basHarf}
        </div>
        
        {/* Kullanıcı Detayları */}
        <div className="flex flex-col gap-1">
          {/* İsim Soyisim (Şık ve belirgin font) */}
          <h1 className="text-3xl font-extrabold text-gray-900 tracking-tight">
            {gosterilenAd}
          </h1>
          
          {/* İletişim Bilgileri */}
          <div className="flex flex-col sm:flex-row sm:items-center gap-3 sm:gap-6 mt-1">
            {formData.email && (
              <p className="text-gray-500 flex items-center gap-2 text-sm font-medium">
                <Mail className="w-4 h-4 text-blue-500" /> 
                {formData.email}
              </p>
            )}
            
            {formData.telefon && (
              <p className="text-gray-500 flex items-center gap-2 text-sm font-medium">
                <Phone className="w-4 h-4 text-indigo-500" /> 
                {formData.telefon}
              </p>
            )}
          </div>
        </div>
      </div>

      {/* Ortak Bildirim Alanı */}
      {mesaj && (
        <div className={`p-4 mb-6 rounded-xl flex items-center gap-3 animate-fade-in ${mesaj.tip === 'success' ? 'bg-green-50 text-green-800 border border-green-200' : 'bg-red-50 text-red-800 border border-red-200'}`}>
          <Activity className="w-5 h-5" />
          <span className="font-medium">{mesaj.metin}</span>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        
        {/* SOL KOLON: Kişisel Bilgiler (2 Birim Genişlik) */}
        <div className="lg:col-span-2 space-y-8">
          
          <div className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden">
            <div className="border-b border-gray-100 bg-gray-50/50 px-6 py-4 flex items-center gap-2">
              <User className="w-5 h-5 text-blue-600" />
              <h2 className="text-lg font-semibold text-gray-800">Kişisel Bilgiler</h2>
            </div>
            
            <form onSubmit={handleProfilGuncelle} className="p-6 space-y-5">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                
                {/* Ad Soyad Input */}
                <div className="col-span-1 md:col-span-2">
                  <label className="block text-sm font-medium text-gray-700 mb-1.5">Ad Soyad</label>
                  <div className="relative">
                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                      <User className="h-5 w-5 text-gray-400" />
                    </div>
                    <input
                      type="text"
                      required
                      className="pl-10 w-full px-4 py-2.5 bg-gray-50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-all"
                      value={formData.adSoyad}
                      onChange={(e) => setFormData({ ...formData, adSoyad: e.target.value })}
                    />
                  </div>
                </div>

                {/* E-posta Input */}
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1.5">E-posta Adresi</label>
                  <div className="relative">
                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                      <Mail className="h-5 w-5 text-gray-400" />
                    </div>
                    <input
                      type="email"
                      required
                      className="pl-10 w-full px-4 py-2.5 bg-gray-50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-all"
                      value={formData.email}
                      onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                    />
                  </div>
                </div>

                {/* Telefon Input */}
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1.5">Telefon Numarası</label>
                  <div className="relative">
                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                      <Phone className="h-5 w-5 text-gray-400" />
                    </div>
                    <input
                      type="tel"
                      placeholder="0555 555 55 55"
                      className="pl-10 w-full px-4 py-2.5 bg-gray-50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-all"
                      value={formData.telefon || ''}
                      onChange={(e) => setFormData({ ...formData, telefon: e.target.value })}
                    />
                  </div>
                </div>
              </div>

              <div className="flex justify-end pt-2">
                <button
                  type="submit"
                  className="flex items-center gap-2 bg-blue-600 hover:bg-blue-700 text-white font-medium py-2.5 px-6 rounded-xl transition-all shadow-sm hover:shadow-md"
                >
                  <Save className="w-4 h-4" /> Değişiklikleri Kaydet
                </button>
              </div>
            </form>
          </div>
        </div>

        {/* SAĞ KOLON: Güvenlik (1 Birim Genişlik) */}
        <div className="space-y-6">
          
          <div className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden">
            <div className="border-b border-gray-100 bg-gray-50/50 px-6 py-4 flex items-center gap-2">
              <Shield className="w-5 h-5 text-indigo-600" />
              <h2 className="text-lg font-semibold text-gray-800">Güvenlik</h2>
            </div>
            
            <form onSubmit={handleSifreGuncelle} className="p-6 space-y-4">
              <div>
                <label className="block text-xs font-medium text-gray-500 mb-1">Mevcut Şifre</label>
                <div className="relative">
                  <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                    <Lock className="h-4 w-4 text-gray-400" />
                  </div>
                  <input
                    type="password"
                    className="pl-9 w-full px-3 py-2 text-sm bg-gray-50 border border-gray-200 rounded-lg focus:ring-2 focus:ring-indigo-500"
                    placeholder="••••••••"
                    value={passwordData.mevcutSifre}
                    onChange={(e) => setPasswordData({...passwordData, mevcutSifre: e.target.value})}
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-medium text-gray-500 mb-1">Yeni Şifre</label>
                <div className="relative">
                  <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                    <Lock className="h-4 w-4 text-gray-400" />
                  </div>
                  <input
                    type="password"
                    className="pl-9 w-full px-3 py-2 text-sm bg-gray-50 border border-gray-200 rounded-lg focus:ring-2 focus:ring-indigo-500"
                    placeholder="En az 6 karakter"
                    value={passwordData.yeniSifre}
                    onChange={(e) => setPasswordData({...passwordData, yeniSifre: e.target.value})}
                  />
                </div>
              </div>

              <button
                type="submit"
                className="w-full mt-2 bg-white border border-gray-300 text-gray-700 hover:bg-gray-50 font-medium py-2 px-4 rounded-lg transition-colors text-sm"
              >
                Şifreyi Güncelle
              </button>
            </form>
          </div>

        </div>
      </div>
    </div>
  );
}