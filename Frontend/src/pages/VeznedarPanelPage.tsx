import { useState } from 'react';
import { odemeApi } from '@/services/api';
import { useToast } from '@/contexts/ToastContext';


const ODEME_TURLERI = ['Nakit', 'POS', 'Havale'] as const;

export function VeznedarPanelPage() {
  const { showSuccess, showError } = useToast();
  const [faturaId, setFaturaId] = useState('');
  const [odemeTuru, setOdemeTuru] = useState<string>('Nakit');
  const [randevuId, setRandevuId] = useState('');
  

  const run = async (fn: () => Promise<unknown>, ok: string) => {
    try {
      const res = await fn();
      showSuccess(ok, (res as { data?: { Mesaj?: string } })?.data?.Mesaj);
    } catch (error: unknown) {
      showError('İşlem başarısız', error instanceof Error ? error.message : '');
    }
  };

  return (
    <div className="space-y-8 max-w-3xl">
      <div>
        <h1 className="text-2xl font-bold">Vezne</h1>
        
      </div>

      <section className="card p-6 space-y-4">
        <h2 className="font-semibold">Fiziksel ödeme</h2>
        <div>
          <label className="label-text">Randevu Numarası</label>
          <input className="input-field" value={faturaId} onChange={(e) => setFaturaId(e.target.value)} />
        </div>
        <div>
          <label className="label-text">odemeTuru</label>
          <select className="input-field" value={odemeTuru} onChange={(e) => setOdemeTuru(e.target.value)}>
            {ODEME_TURLERI.map((t) => (
              <option key={t} value={t}>
                {t}
              </option>
            ))}
          </select>
        </div>
        
        <button
          className="btn-primary"
          type="button"
          onClick={() => run(() => odemeApi.fizikselOde(Number(faturaId), odemeTuru), 'Tahsil edildi')}
        >
          Tahsil et
        </button>
      </section>

      <section className="card p-6 space-y-4">
        <h2 className="font-semibold">Araç teslim</h2>
        <p className="text-xs text-ink-500">Usta tamamlamış ve fatura ödenmiş olmalıdır.</p>
        <div>
          <label className="label-text">RandevuId</label>
          <input className="input-field" value={randevuId} onChange={(e) => setRandevuId(e.target.value)} />
        </div>
        <button className="btn-primary" type="button" onClick={() => run(() => odemeApi.teslimEt(Number(randevuId)), 'Teslim onaylandı')}>
          Teslim et
        </button>
      </section>
    </div>
  );
}
