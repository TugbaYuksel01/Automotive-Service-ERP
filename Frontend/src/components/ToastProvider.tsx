import { useCallback, useState } from 'react';
import {
  CheckCircle2,
  XCircle,
  Info,
  AlertTriangle,
  Sparkles,
  X,
  TrendingUp,
  Clock,
} from 'lucide-react';
import { ToastContext, type Toast, type ToastType } from '@/contexts/ToastContext';
import type { AiAnalysisMessage } from '@/types';

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const dismiss = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const show = useCallback((toast: Omit<Toast, 'id'>): string => {
    const id = `${Date.now()}-${Math.random().toString(36).slice(2)}`;
    const duration = toast.duration ?? (toast.type === 'ai' ? 12000 : 5000);
    const newToast: Toast = { ...toast, id, duration };
    setToasts((prev) => [...prev, newToast]);
    if (duration > 0) {
      setTimeout(() => dismiss(id), duration);
    }
    return id;
  }, [dismiss]);

  const showSuccess = useCallback((title: string, message?: string) =>
    show({ type: 'success', title, message }), [show]);

  const showError = useCallback((title: string, message?: string) =>
    show({ type: 'error', title, message }), [show]);

  const showInfo = useCallback((title: string, message?: string) =>
    show({ type: 'info', title, message }), [show]);

  const showWarning = useCallback((title: string, message?: string) =>
    show({ type: 'warning', title, message }), [show]);

  const showAi = useCallback((data: AiAnalysisMessage) => {
    const title = data.Baslik || 'Yapay Zeka Analiz Sonucu';
    const message = data.Icerik || data.raw || '';
    show({
      type: 'ai',
      title,
      message,
      aiData: data,
      duration: 15000,
    });
  }, [show]);

  return (
    <ToastContext.Provider
      value={{ toasts, show, dismiss, showSuccess, showError, showInfo, showWarning, showAi }}
    >
      {children}
      <ToastViewport toasts={toasts} onDismiss={dismiss} />
    </ToastContext.Provider>
  );
}

function ToastViewport({
  toasts,
  onDismiss,
}: {
  toasts: Toast[];
  onDismiss: (id: string) => void;
}) {
  return (
    <div className="fixed bottom-4 right-4 z-[9999] flex max-w-sm flex-col gap-3 pointer-events-none">
      {toasts.map((toast) => (
        <ToastCard key={toast.id} toast={toast} onDismiss={() => onDismiss(toast.id)} />
      ))}
    </div>
  );
}

function ToastCard({ toast, onDismiss }: { toast: Toast; onDismiss: () => void }) {
  const [exiting, setExiting] = useState(false);

  const handleClose = () => {
    setExiting(true);
    setTimeout(onDismiss, 300);
  };

  if (toast.type === 'ai') {
    return <AiToastCard toast={toast} onDismiss={handleClose} exiting={exiting} />;
  }

  const config: Record<ToastType, { icon: typeof Info; bg: string; border: string; iconColor: string }> = {
    success: { icon: CheckCircle2, bg: 'bg-white', border: 'border-l-4 border-l-success-500', iconColor: 'text-success-500' },
    error: { icon: XCircle, bg: 'bg-white', border: 'border-l-4 border-l-error-500', iconColor: 'text-error-500' },
    info: { icon: Info, bg: 'bg-white', border: 'border-l-4 border-l-brand-500', iconColor: 'text-brand-500' },
    warning: { icon: AlertTriangle, bg: 'bg-white', border: 'border-l-4 border-l-warning-500', iconColor: 'text-warning-500' },
    ai: { icon: Sparkles, bg: 'bg-white', border: 'border-l-4 border-l-accent-500', iconColor: 'text-accent-500' },
  };

  const { icon: Icon, bg, border, iconColor } = config[toast.type];

  return (
    <div
      className={`pointer-events-auto ${bg} ${border} rounded-xl shadow-xl shadow-ink-900/10 p-4 w-80 animate-slide-in-right ${
        exiting ? 'animate-slide-out-right' : ''
      }`}
    >
      <div className="flex items-start gap-3">
        <Icon className={`h-5 w-5 flex-shrink-0 ${iconColor} mt-0.5`} />
        <div className="flex-1 min-w-0">
          <p className="text-sm font-semibold text-ink-900">{toast.title}</p>
          {toast.message && (
            <p className="mt-0.5 text-sm text-ink-600 break-words">{toast.message}</p>
          )}
        </div>
        <button
          onClick={handleClose}
          className="flex-shrink-0 text-ink-400 hover:text-ink-700 transition-colors"
        >
          <X className="h-4 w-4" />
        </button>
      </div>
    </div>
  );
}

function AiToastCard({
  toast,
  onDismiss,
  exiting,
}: {
  toast: Toast;
  onDismiss: () => void;
  exiting: boolean;
}) {
  const data = toast.aiData;
  const formattedCost = null;
  const confidence = null;
  const time = null;

  return (
    <div
      className={`pointer-events-auto w-96 rounded-xl bg-gradient-to-br from-white to-brand-50/50 border border-brand-200/60 shadow-2xl shadow-brand-900/15 p-5 animate-slide-in-right ${
        exiting ? 'animate-slide-out-right' : ''
      }`}
    >
      {/* Header */}
      <div className="flex items-start justify-between mb-3">
        <div className="flex items-center gap-2">
          <div className="relative">
            <div className="absolute inset-0 bg-accent-400 rounded-full animate-pulse-ring" />
            <div className="relative flex items-center justify-center w-8 h-8 rounded-full bg-gradient-to-br from-brand-500 to-brand-700">
              <Sparkles className="h-4 w-4 text-white" />
            </div>
          </div>
          <div>
            <p className="text-xs font-semibold text-brand-600 uppercase tracking-wide">Yapay Zeka Analizi</p>
            {time && <p className="text-xs text-ink-400">{time}</p>}
          </div>
        </div>
        <button
          onClick={onDismiss}
          className="text-ink-400 hover:text-ink-700 transition-colors"
        >
          <X className="h-4 w-4" />
        </button>
      </div>

      {/* Title */}
      <h4 className="text-sm font-bold text-ink-900 mb-1">{toast.title}</h4>
      {toast.message && (
        <p className="text-sm text-ink-600 mb-3 break-words leading-relaxed">{toast.message}</p>
      )}

      {/* AI Data */}
      <div className="space-y-2">
        {formattedCost != null && (
          <div className="flex items-center gap-2 rounded-lg bg-white/80 px-3 py-2 border border-brand-100">
            <TrendingUp className="h-4 w-4 text-success-600" />
            <span className="text-xs text-ink-500">Tahmini Maliyet</span>
            <span className="ml-auto text-sm font-bold text-ink-900">{formattedCost}</span>
          </div>
        )}

        {confidence != null && (
          <div className="flex items-center gap-2 rounded-lg bg-white/80 px-3 py-2 border border-brand-100">
            <div className="flex-1">
              <div className="flex items-center justify-between mb-1">
                <span className="text-xs text-ink-500">Güven Skoru</span>
                <span className="text-xs font-semibold text-brand-600">%{confidence}</span>
              </div>
              <div className="h-1.5 rounded-full bg-ink-100 overflow-hidden">
                <div
                  className="h-full rounded-full bg-gradient-to-r from-brand-500 to-success-500 transition-all duration-700"
                  style={{ width: `${confidence}%` }}
                />
              </div>
            </div>
          </div>
        )}

        {data?.RandevuId != null && (
          <div className="flex items-center gap-1.5 text-xs text-ink-400">
            <Clock className="h-3 w-3" />
            <span>Randevu #{data.RandevuId} için analiz edildi</span>
          </div>
        )}
      </div>
    </div>
  );
}
