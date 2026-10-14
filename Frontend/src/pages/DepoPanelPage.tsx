import { useCallback, useEffect, useState } from 'react';
import { parcaApi } from '@/services/api';
import { useAuth } from '@/contexts/AuthContext';
import { useToast } from '@/contexts/ToastContext';
import type { YedekParca } from '@/types';

const empty: Omit<YedekParca, 'Id'> = {
  ParcaAdi: '',
  BirimFiyat: 0,
  StokMiktari: 0,
  KritikStokSeviyesi: 5,
  UyumluHizmetTipiId: 1,
};

export function DepoPanelPage() {
  const { hasRole } = useAuth();
  const { showSuccess, showError } = useToast();
  const canWrite = hasRole('SuperAdmin', 'Admin', 'DepoSorumlusu');
  const canDelete = hasRole('SuperAdmin', 'Admin');
  const [stoklar, setStoklar] = useState<YedekParca[]>([]);
  const [kritik, setKritik] = useState<YedekParca[] | string>([]);
  const [form, setForm] = useState(empty);
  const [edit, setEdit] = useState<YedekParca | null>(null);

  const load = useCallback(async () => {
    try {
      setStoklar(await parcaApi.tumStoklar());
      const k = await parcaApi.kritikStoklar();
      setKritik(Array.isArray(k) ? k : (k as { Mesaj?: string }).Mesaj || []);
    } catch (error: unknown) {
      showError('Stoklar alınamadı', error instanceof Error ? error.message : '');
    }
  }, [showError]);

  useEffect(() => {
    load();
  }, [load]);

  const run = async (fn: () => Promise<unknown>, ok: string) => {
    try {
      await fn();
      showSuccess(ok);
      setForm(empty);
      setEdit(null);
      load();
    } catch (error: unknown) {
      showError('İşlem başarısız', error instanceof Error ? error.message : '');
    }
  };

  return (
    <div className="space-y-8 max-w-5xl">
      <div>
        <h1 className="text-2xl font-bold">Depo</h1>
      </div>

      <section className="card p-6">
        <h2 className="font-semibold mb-3">Kritik stoklar</h2>
        {typeof kritik === 'string' ? (
          <p className="text-sm text-success-700">{kritik}</p>
        ) : (
          <ul className="text-sm space-y-1">
            {kritik.map((p) => (
              <li key={p.Id} className="text-error-700">
                {p.ParcaAdi} — stok {p.StokMiktari} / kritik {p.KritikStokSeviyesi}
              </li>
            ))}
          </ul>
        )}
      </section>

      {canWrite && (
        <section className="card p-6">
          <h2 className="font-semibold mb-4">{edit ? `Güncelle #${edit.Id}` : 'Yeni parça'}</h2>
          <form
            className="grid sm:grid-cols-2 gap-3"
            onSubmit={(e) => {
              e.preventDefault();
              if (edit) {
                run(() => parcaApi.guncelle(edit.Id, { ...edit, ...form, Id: edit.Id }), 'Güncellendi');
              } else {
                run(() => parcaApi.ekle(form), 'Eklendi');
              }
            }}
          >
            {(['ParcaAdi'] as const).map((name) => (
              <div key={name} className="sm:col-span-2">
                <label className="label-text">{name}</label>
                <input name={name} className="input-field" value={form[name] ?? ''} onChange={(e) => setForm({ ...form, [name]: e.target.value })} required />
              </div>
            ))}
            <div>
              <label className="label-text">BirimFiyat</label>
              <input name="BirimFiyat" type="number" className="input-field" value={form.BirimFiyat} onChange={(e) => setForm({ ...form, BirimFiyat: Number(e.target.value) })} />
            </div>
            <div>
              <label className="label-text">StokMiktari</label>
              <input name="StokMiktari" type="number" className="input-field" value={form.StokMiktari} onChange={(e) => setForm({ ...form, StokMiktari: Number(e.target.value) })} />
            </div>
            <div>
              <label className="label-text">KritikStokSeviyesi</label>
              <input name="KritikStokSeviyesi" type="number" className="input-field" value={form.KritikStokSeviyesi} onChange={(e) => setForm({ ...form, KritikStokSeviyesi: Number(e.target.value) })} />
            </div>
            <div>
              <label className="label-text">UyumluHizmetTipiId</label>
              <input name="UyumluHizmetTipiId" type="number" className="input-field" value={form.UyumluHizmetTipiId} onChange={(e) => setForm({ ...form, UyumluHizmetTipiId: Number(e.target.value) })} />
            </div>
            <div className="sm:col-span-2 flex gap-2">
              <button className="btn-primary" type="submit">
                {edit ? 'Güncelle' : 'Ekle'}
              </button>
              {edit && (
                <button className="btn-secondary" type="button" onClick={() => { setEdit(null); setForm(empty); }}>
                  Vazgeç
                </button>
              )}
            </div>
          </form>
        </section>
      )}

      <section className="card overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="bg-ink-50 text-left">
            <tr>
              <th className="p-3">Id</th>
              <th className="p-3">ParcaAdi</th>
              <th className="p-3">BirimFiyat</th>
              <th className="p-3">StokMiktari</th>
              <th className="p-3">KritikStokSeviyesi</th>
              <th className="p-3" />
            </tr>
          </thead>
          <tbody>
            {stoklar.map((p) => (
              <tr key={p.Id} className="border-t border-ink-100">
                <td className="p-3">{p.Id}</td>
                <td className="p-3">{p.ParcaAdi}</td>
                <td className="p-3">{p.BirimFiyat}</td>
                <td className="p-3">{p.StokMiktari}</td>
                <td className="p-3">{p.KritikStokSeviyesi}</td>
                <td className="p-3 text-right space-x-2">
                  {canWrite && (
                    <button
                      className="btn-ghost"
                      type="button"
                      onClick={() => {
                        setEdit(p);
                        setForm({
                          ParcaAdi: p.ParcaAdi ?? '',
                          BirimFiyat: p.BirimFiyat,
                          StokMiktari: p.StokMiktari,
                          KritikStokSeviyesi: p.KritikStokSeviyesi,
                          UyumluHizmetTipiId: p.UyumluHizmetTipiId,
                        });
                      }}
                    >
                      Düzenle
                    </button>
                  )}
                  {canDelete && (
                    <button className="btn-ghost text-error-600" type="button" onClick={() => run(() => parcaApi.sil(p.Id), 'Silindi')}>
                      Sil
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
    </div>
  );
}
