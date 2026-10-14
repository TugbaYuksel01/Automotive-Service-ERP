import { useEffect, useRef } from 'react';
import { ToastProvider } from '@/components/ToastProvider';
import { useToast } from '@/contexts/ToastContext';
import { signalR } from '@/services/signalr';

function SignalRBridge() {
  const { showAi } = useToast();
  const showAiRef = useRef(showAi);
  showAiRef.current = showAi;

  useEffect(() => {
    const unsub = signalR.onTehis((mesaj) => {
      showAiRef.current({ Icerik: mesaj, Baslik: 'Yapay zeka analizi', raw: mesaj });
    });
    signalR.connect().catch(() => undefined);
    return () => {
      unsub();
    };
  }, []);

  return null;
}

export function AppProviders({ children }: { children: React.ReactNode }) {
  return (
    <ToastProvider>
      <SignalRBridge />
      {children}
    </ToastProvider>
  );
}
