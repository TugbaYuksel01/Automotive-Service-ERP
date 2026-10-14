import { useEffect, useState } from 'react';
import { adminApi, hizmetApi, istasyonApi, randevuApi, ustaApi } from '@/services/api';
import { useAuth } from '@/contexts/AuthContext';
import { useToast } from '@/contexts/ToastContext';
import { PERSONEL_ROLLER, type HizmetTipi, type PersonelEkleDto } from '@/types';
import { ShieldPlus, UserPlus, Wrench, Building2, UserCheck, Clock } from 'lucide-react';
import { DailyAppointmentsList } from '@/components/DailyAppointmentsList';

export function AdminPanelPage() {
  const { hasRole } = useAuth();
  const { showSuccess, showError } = useToast();
  const isSuper = hasRole('SuperAdmin');

  const [adminForm, setAdminForm] = useState<PersonelEkleDto>({
    AdSoyad: '',
    Email: '',
    Telefon: '',
    Sifre: '',
    Rol: 'Admin',
    uzmanlikAlani:''
  });
  
  const [personelForm, setPersonelForm] = useState<PersonelEkleDto>({
    AdSoyad: '',
    Email: '',
    Telefon: '',
    Sifre: '',
    Rol: 'Usta',
    uzmanlikAlani: ''
  });

  const [hizmet, setHizmet] = useState({
    Ad: '',
    Fiyat: 0,
    UstaSureDakika: 30,
    LiftSureDakika: 20,
    GerekenUzmanlik: '',
  });

  const [istasyon, setIstasyon] = useState({ Ad: '', MusaitMi: true });
  const [usta, setUsta] = useState({ AdSoyad: '', UzmanlikAlani: '' });
  const [hizmetler, setHizmetler] = useState<HizmetTipi[]>([]);
  const [randevuId, setRandevuId] = useState('');
  const [ekSure, setEkSure] = useState({ EkSureDakika: 15, Sebep: '' });

  useEffect(() => {
    hizmetApi.listele().then(setHizmetler).catch(() => undefined);
  }, []);

  const run = async (fn: () => Promise<unknown>, ok: string) => {
    try {
      const res = await fn();
      const mesaj = (res as { data?: { Mesaj?: string } })?.data?.Mesaj;
      showSuccess(ok, mesaj);
    } catch (error: unknown) {
      showError('İşlem başarısız', error instanceof Error ? error.message : '');
    }
  };

  return (
    <div className="mx-auto max-w-5xl px-4 py-8 space-y-10">
      
      {/* BAŞLIK */}
     {/* GÜNLÜK TÜM RANDEVULAR LİSTESİ */}
      <section className="mb-8">
        <h2 className="text-xl font-bold text-ink-900 mb-4 flex items-center gap-2">
          📅 Atölyedeki Tüm Günlük Randevular
        </h2>
        <DailyAppointmentsList type="admin" />
      </section>
      <div>
        <h1 className="text-3xl font-bold text-gray-800">Yönetim ve Operasyon Paneli</h1>
        <p className="text-sm text-gray-500 mt-1">
          Sistem personellerini, servis kataloglarını ve istasyon durumlarını bu alandan yönetebilirsiniz.
        </p>
      </div>

      {/* 1. SÜPER ADMIN: YENİ ADMIN EKLEME */}
      {isSuper && (
        <section className="card p-6 bg-white border border-gray-200 rounded-2xl shadow-sm space-y-6">
          <div className="flex items-center gap-2 border-b pb-4">
            <ShieldPlus className="w-5 h-5 text-indigo-600" />
            <h2 className="font-bold text-lg text-gray-800">Yeni Sistem Yöneticisi (Admin) Ekle</h2>
          </div>
          
          <form
            className="grid sm:grid-cols-2 gap-4"
            onSubmit={(e) => {
              e.preventDefault();
              run(() => adminApi.adminEkle({ ...adminForm, Rol: 'Admin' }), 'Yeni Admin başarıyla eklendi.');
            }}
          >
            <div>
              <label className="block text-xs font-bold text-gray-600 mb-1 uppercase">Ad Soyad</label>
              <input
                type="text"
                className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 outline-none"
                placeholder="Örn: Ahmet Yılmaz"
                value={adminForm.AdSoyad}
                onChange={(e) => setAdminForm({ ...adminForm, AdSoyad: e.target.value })}
                required
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-gray-600 mb-1 uppercase">E-Posta Adresi</label>
              <input
                type="email"
                className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 outline-none"
                placeholder="ornek@otoservis.com"
                value={adminForm.Email}
                onChange={(e) => setAdminForm({ ...adminForm, Email: e.target.value })}
                required
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-gray-600 mb-1 uppercase">Telefon Numarası (İsteğe Bağlı)</label>
              <input
                type="tel"
                className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 outline-none"
                placeholder="0555..."
                value={adminForm.Telefon || ''}
                onChange={(e) => setAdminForm({ ...adminForm, Telefon: e.target.value })}
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-gray-600 mb-1 uppercase">Şifre (En az 6 karakter)</label>
              <input
                type="password"
                className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 outline-none"
                placeholder="******"
                value={adminForm.Sifre}
                onChange={(e) => setAdminForm({ ...adminForm, Sifre: e.target.value })}
                required
                minLength={6}
              />
            </div>

            <button className="btn-primary sm:col-span-2 py-3 rounded-xl font-bold" type="submit">
              Sistem Admini Oluştur
            </button>
          </form>
        </section>
      )}

      {/* 2. PERSONEL EKLE (Usta / Veznedar / Depo) */}
      <section className="card p-6 bg-white border border-gray-200 rounded-2xl shadow-sm space-y-6">
        <div className="flex items-center gap-2 border-b pb-4">
          <UserPlus className="w-5 h-5 text-indigo-600" />
          <h2 className="font-bold text-lg text-gray-800">Personel Ekle (Usta / Vezne / Depo)</h2>
        </div>

        <form
          className="grid sm:grid-cols-2 gap-4"
          onSubmit={(e) => {
            e.preventDefault();
            run(() => adminApi.personelEkle(personelForm), 'Personel başarıyla sisteme eklendi.');
          }}
        >
          <div>
            <label className="block text-xs font-bold text-gray-600 mb-1 uppercase">Ad Soyad</label>
            <input
              type="text"
              className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 outline-none"
              placeholder="Personel Adı Soyadı"
              value={personelForm.AdSoyad}
              onChange={(e) => setPersonelForm({ ...personelForm, AdSoyad: e.target.value })}
              required
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-gray-600 mb-1 uppercase">E-Posta Adresi</label>
            <input
              type="email"
              className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 outline-none"
              placeholder="personel@otoservis.com"
              value={personelForm.Email}
              onChange={(e) => setPersonelForm({ ...personelForm, Email: e.target.value })}
              required
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-gray-600 mb-1 uppercase">Telefon Numarası</label>
            <input
              type="tel"
              className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 outline-none"
              placeholder="0555..."
              value={personelForm.Telefon || ''}
              onChange={(e) => setPersonelForm({ ...personelForm, Telefon: e.target.value })}
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-gray-600 mb-1 uppercase">Şifre</label>
            <input
              type="password"
              className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 outline-none"
              placeholder="******"
              value={personelForm.Sifre}
              onChange={(e) => setPersonelForm({ ...personelForm, Sifre: e.target.value })}
              required
              minLength={6}
            />
          </div>

          <div className="sm:col-span-2">
            <label className="block text-xs font-bold text-gray-600 mb-1 uppercase">Personel Rolü / Departmanı</label>
            <select
              className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 outline-none bg-white font-medium"
              value={personelForm.Rol}
              onChange={(e) => setPersonelForm({ ...personelForm, Rol: e.target.value })}
            >
              {PERSONEL_ROLLER.map((r) => (
                <option key={r} value={r}>
                  {r === 'DepoSorumlusu' ? 'Depo Sorumlusu' : r}
                </option>
              ))}
            </select>
          </div>

          {/* DİNAMİK UZMANLIK ALANI KUTUSU */}
          {personelForm.Rol === 'Usta' && (
            <div className="sm:col-span-2 p-4 bg-indigo-50 border border-indigo-100 rounded-lg animate-fade-in">
              <label className="block text-xs font-bold text-indigo-800 mb-1 uppercase">
                🛠️ Ustanın Uzmanlık Alanı (Zorunlu)
              </label>
              <select
                className="w-full px-3 py-2 text-sm border border-indigo-200 rounded-lg focus:ring-2 focus:ring-indigo-500 outline-none bg-white"
                value={personelForm.uzmanlikAlani}
                onChange={(e) => setPersonelForm({ ...personelForm, uzmanlikAlani: e.target.value })}
                required
              >
                <option value="">Lütfen uzmanlık alanı seçin...</option>
                <option value="Mekanik">Motor & Mekanik</option>
                <option value="Oto Elektrik">Oto Elektrik & Elektronik</option>
                <option value="Kaporta">Kaporta & Boya</option>
                <option value="Lastik">Ön Düzen & Lastik</option>
              </select>
            </div>
          )}

          <button className="btn-primary sm:col-span-2 py-3 rounded-xl font-bold" type="submit">
            Personeli Kaydet
          </button>
        </form>
      </section>

      {/* 3. YENİ HİZMET TİPİ TANIMLAMA */}
      <section className="card p-6 bg-white border border-gray-200 rounded-2xl shadow-sm space-y-6">
        <div className="flex items-center gap-2 border-b pb-4">
          <Wrench className="w-5 h-5 text-indigo-600" />
          <h2 className="font-bold text-lg text-gray-800">Servis Hizmet Türü Tanımla</h2>
        </div>

        <form
          className="grid sm:grid-cols-2 gap-4"
          onSubmit={(e) => {
            e.preventDefault();
            run(() => hizmetApi.ekle(hizmet), 'Hizmet kataloğa eklendi.');
          }}
        >
          <div>
            <label className="block text-xs font-bold text-gray-600 mb-1 uppercase">Hizmet Adı</label>
            <input
              type="text"
              className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 outline-none"
              placeholder="Örn: Ağır Bakım ve Fren Testi"
              value={hizmet.Ad}
              onChange={(e) => setHizmet({ ...hizmet, Ad: e.target.value })}
              required
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-gray-600 mb-1 uppercase">Taban Fiyat (₺)</label>
            <input
              type="number"
              className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 outline-none"
              value={hizmet.Fiyat}
              onChange={(e) => setHizmet({ ...hizmet, Fiyat: Number(e.target.value) })}
              min="0"
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-gray-600 mb-1 uppercase">Usta İşlem Süresi (Dakika)</label>
            <input
              type="number"
              className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 outline-none"
              value={hizmet.UstaSureDakika}
              onChange={(e) => setHizmet({ ...hizmet, UstaSureDakika: Number(e.target.value) })}
              min="5"
            />
          </div>

          <div>
            <label className="block text-xs font-bold text-gray-600 mb-1 uppercase">Lift Meşguliyet Süresi (Dakika)</label>
            <input
              type="number"
              className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 outline-none"
              value={hizmet.LiftSureDakika}
              onChange={(e) => setHizmet({ ...hizmet, LiftSureDakika: Number(e.target.value) })}
              min="5"
            />
          </div>

          <div className="sm:col-span-2">
            <label className="block text-xs font-bold text-gray-600 mb-1 uppercase">Gereken Uzmanlık Alanı (Usta Eşleşmesi İçin)</label>
            <input
              type="text"
              className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 outline-none"
              placeholder="Örn: Motor, Mekanik, Elektrik"
              value={hizmet.GerekenUzmanlik}
              onChange={(e) => setHizmet({ ...hizmet, GerekenUzmanlik: e.target.value })}
            />
          </div>

          <button className="btn-primary sm:col-span-2 py-3 rounded-xl font-bold" type="submit">
            Hizmet Türünü Kaydet
          </button>
        </form>
        <p className="text-xs text-gray-500 font-medium">Sistemde halihazırda kayıtlı {hizmetler.length} farklı hizmet türü bulunmaktadır.</p>
      </section>

      {/* 4. İSTASYON VE USTA EKLEME (YAN YANA) */}
      <div className="grid md:grid-cols-2 gap-6">
        
        {/* İSTASYON EKLE */}
        <section className="card p-6 bg-white border border-gray-200 rounded-2xl shadow-sm space-y-6 flex flex-col justify-between">
          <div>
            <div className="flex items-center gap-2 border-b pb-4 mb-4">
              <Building2 className="w-5 h-5 text-indigo-600" />
              <h2 className="font-bold text-lg text-gray-800">İstasyon (Lift) Ekle</h2>
            </div>

            <form
              id="istasyonForm"
              className="space-y-4"
              onSubmit={(e) => {
                e.preventDefault();
                run(() => istasyonApi.ekle(istasyon), 'Yeni istasyon başarıyla eklendi.');
              }}
            >
              <div>
                <label className="block text-xs font-bold text-gray-600 mb-1 uppercase">İstasyon Adı / Numarası</label>
                <input
                  type="text"
                  className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 outline-none"
                  placeholder="Örn: Lift 1 veya Boya Fırını"
                  value={istasyon.Ad}
                  onChange={(e) => setIstasyon({ ...istasyon, Ad: e.target.value })}
                  required
                />
              </div>

              <label className="flex items-center gap-3 text-sm font-semibold text-gray-700 cursor-pointer pt-2">
                <input
                  type="checkbox"
                  className="w-4 h-4 text-indigo-600 rounded border-gray-300 focus:ring-indigo-500"
                  checked={istasyon.MusaitMi}
                  onChange={(e) => setIstasyon({ ...istasyon, MusaitMi: e.target.checked })}
                />
                Şu an başlangıçta müsait (boş) olsun
              </label>
            </form>
          </div>

          <button form="istasyonForm" className="btn-primary w-full py-3 rounded-xl font-bold mt-6" type="submit">
            İstasyonu Kaydet
          </button>
        </section>

        {/* USTA EKLE */}
        <section className="card p-6 bg-white border border-gray-200 rounded-2xl shadow-sm space-y-6 flex flex-col justify-between">
          <div>
            <div className="flex items-center gap-2 border-b pb-4 mb-4">
              <UserCheck className="w-5 h-5 text-indigo-600" />
              <h2 className="font-bold text-lg text-gray-800">Usta Kadrosu Ekle</h2>
            </div>

            <form
              id="ustaForm"
              className="space-y-4"
              onSubmit={(e) => {
                e.preventDefault();
                run(() => ustaApi.ekle(usta), 'Usta başarıyla kadroya eklendi.');
              }}
            >
              <div>
                <label className="block text-xs font-bold text-gray-600 mb-1 uppercase">Usta Ad Soyad</label>
                <input
                  type="text"
                  className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 outline-none"
                  placeholder="Örn: Mehmet Usta"
                  value={usta.AdSoyad}
                  onChange={(e) => setUsta({ ...usta, AdSoyad: e.target.value })}
                  required
                />
              </div>

              <div>
                <label className="block text-xs font-bold text-gray-600 mb-1 uppercase">Uzmanlık Alanı</label>
                <input
                  type="text"
                  className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 outline-none"
                  placeholder="Örn: Motor, Mekanik, Kaporta"
                  value={usta.UzmanlikAlani}
                  onChange={(e) => setUsta({ ...usta, UzmanlikAlani: e.target.value })}
                  required
                />
              </div>
            </form>
          </div>

          <button form="ustaForm" className="btn-primary w-full py-3 rounded-xl font-bold mt-6" type="submit">
            Ustayı Kaydet
          </button>
        </section>

      </div>

      {/* 5. ACİL DURUM / MANUEL RANDEVU OPERASYONLARI */}
      <section className="card p-6 bg-white border border-gray-200 rounded-2xl shadow-sm space-y-6">
        <div className="flex items-center gap-2 border-b pb-4">
          <Clock className="w-5 h-5 text-amber-600" />
          <h2 className="font-bold text-lg text-gray-800">Acil Durum & Randevu Operasyon Yönetimi</h2>
        </div>
        <p className="text-xs text-gray-500">
          Atölye içerisinde anlık gelişen kriz durumlarında veya test amaçlı olarak doğrudan randevu numarası ile manuel müdahale gerçekleştirebilirsiniz.
        </p>

        <div className="max-w-xs">
          <label className="block text-xs font-bold text-gray-600 mb-1 uppercase">Hedef Randevu Numarası</label>
          <input
            type="number"
            className="w-full px-4 py-3 text-lg font-bold text-center border border-gray-300 rounded-xl focus:ring-2 focus:ring-indigo-500 outline-none placeholder:font-normal placeholder:text-sm"
            placeholder="Örn: 5"
            value={randevuId}
            onChange={(e) => setRandevuId(e.target.value)}
          />
        </div>

        <div className="flex flex-wrap gap-4 pt-2">
          <button 
            className="px-6 py-2.5 bg-indigo-50 text-indigo-700 border border-indigo-200 font-bold rounded-xl hover:bg-indigo-600 hover:text-white transition-all shadow-sm" 
            type="button" 
            onClick={() => run(() => randevuApi.baslat(Number(randevuId)), `Randevu #${randevuId} işleme alındı.`)}
          >
            Manuel Olarak Başlat
          </button>
          
          <button 
            className="px-6 py-2.5 bg-red-50 text-red-700 border border-red-200 font-bold rounded-xl hover:bg-red-600 hover:text-white transition-all shadow-sm" 
            type="button" 
            onClick={() => run(() => randevuApi.iptal(Number(randevuId)), `Randevu #${randevuId} iptal edildi.`)}
          >
            Randevuyu İptal Et
          </button>
        </div>

        <div className="bg-amber-50/50 p-5 rounded-xl border border-amber-100 space-y-4 mt-4">
          <h3 className="text-sm font-bold text-gray-700">Operasyonel Süre Uzatma & Trafik Polisi Tetikleyicisi</h3>
          <div className="grid sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-bold text-gray-600 mb-1 uppercase">Ek Süre (Dakika)</label>
              <select
                className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-amber-500 outline-none"
                value={ekSure.EkSureDakika}
                onChange={(e) => setEkSure({ ...ekSure, EkSureDakika: Number(e.target.value) })}
              >
                <option value={15}>15 Dakika</option>
                <option value={30}>30 Dakika</option>
                <option value={45}>45 Dakika</option>
                <option value={60}>60 Dakika (1 Saat)</option>
              </select>
            </div>
            
            <div>
              <label className="block text-xs font-bold text-gray-600 mb-1 uppercase">Gecikme Sebebi</label>
              <input
                type="text"
                className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-amber-500 outline-none"
                placeholder="Örn: Sıkışmış cıvata / Parça değişimi"
                value={ekSure.Sebep}
                onChange={(e) => setEkSure({ ...ekSure, Sebep: e.target.value })}
              />
            </div>
          </div>

          <button
            className="w-full py-3 bg-amber-500 text-white font-bold rounded-xl hover:bg-amber-600 transition-colors shadow-sm"
            type="button"
            onClick={() => run(() => randevuApi.sureUzat(Number(randevuId), ekSure), `Randevu #${randevuId} için süre uzatıldı ve arka plan planlaması güncellendi.`)}
          >
            Süreyi Uzat & Arkadaki Randevuları Otomatik Kaydır (Kriz Yönetimi)
          </button>
        </div>
      </section>

    </div>
  );
}