import { useEffect, useState } from 'react';
import { signalR } from '@/services/signalr';

export function useSignalR() {
  const [connectionId, setConnectionId] = useState<string | null>(signalR.connectionId);
  const [connected, setConnected] = useState(false);
  
  // Kutuların içeriklerini ve yüklenme durumunu tutan State'ler
  const [aiMesaji, setAiMesaji] = useState<string | null>(null);
  const [hataMesaji, setHataMesaji] = useState<string | null>(null);
  const [basariMesaji, setBasariMesaji] = useState<string | null>(null);
  const [isAnalizBekleniyor, setAnalizBekleniyor] = useState<boolean>(false);

  // Form gönderildiğinde ekranı temizlemek için yardımcı fonksiyon
  const resetMesajlar = () => {
    setAiMesaji(null);
    setHataMesaji(null);
    setBasariMesaji(null);
  };

  useEffect(() => {
    // 1. Mavi Kutu (AI Teşhisi)
    const offMsg = signalR.onTehis((mesaj) => {
      setAnalizBekleniyor(false); 
      setAiMesaji(mesaj);
    });    
    // 2. Kırmızı/Sarı Kutu (Kapasite Dolu - Red)
    const offRed = signalR.onRandevuReddedildi((redMesaji) => {
      setAnalizBekleniyor(false); 
      setHataMesaji(redMesaji);
    });

    // 3. Yeşil Kutu (Kesin Onay)
    const offOnay = signalR.onRandevuOnaylandi((randevuId) => {
      setAnalizBekleniyor(false); 
      setBasariMesaji(`Randevu detaylarınız sisteme başarıyla kaydedildi. (Randevu No: ${randevuId})`);
    });

    const offStatus = signalR.onStatus((ok) => {
      setConnected(ok);
      setConnectionId(signalR.connectionId);
    });

    signalR
      .connect()
      .then(() => {
        setConnectionId(signalR.connectionId);
        setConnected(true);
      })
      .catch(() => setConnected(false));

    return () => {
      offMsg();
      offRed();
      offOnay();
      offStatus();
    };
  }, []);

  return { 
    connectionId, 
    connected,
    aiMesaji, 
    hataMesaji, 
    basariMesaji, 
    isAnalizBekleniyor, 
    
    setAiMesaji, 
    setAnalizBekleniyor, 
    resetMesajlar, 
    setHataMesaji,    
    setBasariMesaji
  };
}