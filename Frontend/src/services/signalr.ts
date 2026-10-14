import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { API_BASE } from './api';

type MessageHandler = (mesaj: string) => void;
type StatusHandler = (connected: boolean) => void;
type OnayHandler = (randevuId: number) => void; // Yeni: Onay mesajı için tip tanımı

class SignalRService {
  private connection: HubConnection | null = null;
  
  // Dinleyicileri hafızada tutan listeler (Set)
  private messageHandlers = new Set<MessageHandler>();
  private statusHandlers = new Set<StatusHandler>();
  
  // YENİ EKLENEN LİSTELER
  private redHandlers = new Set<MessageHandler>();
  private onayHandlers = new Set<OnayHandler>();
  
  connectionId: string | null = null;

  onTehis(handler: MessageHandler): () => void {
    this.messageHandlers.add(handler);
    return () => this.messageHandlers.delete(handler);
  }

  // YENİ METOTLAR (Hub'a doğrudan değil, Set listesine ekliyor)
  onRandevuReddedildi(handler: MessageHandler): () => void {
    this.redHandlers.add(handler);
    return () => this.redHandlers.delete(handler);
  }

  onRandevuOnaylandi(handler: OnayHandler): () => void {
    this.onayHandlers.add(handler);
    return () => this.onayHandlers.delete(handler);
  }

  onStatus(handler: StatusHandler): () => void {
    this.statusHandlers.add(handler);
    return () => this.statusHandlers.delete(handler);
  }

  async connect(): Promise<void> {
    if (this.connection?.state === 'Connected') return;

    this.connection = new HubConnectionBuilder()
      .withUrl(`${API_BASE}/notificationHub`)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    // 1. Mavi Kutu (AI Teşhisi) Olayını Dinle
    this.connection.on('ReceiveTehis', (mesaj: string) => {
      this.messageHandlers.forEach((h) => h(mesaj));
    });

    // 2. Kırmızı/Sarı Kutu (Kapasite Dolu) Olayını Dinle
    this.connection.on('RandevuReddedildi', (mesaj: string) => {
      this.redHandlers.forEach((h) => h(mesaj));
    });

    // 3. Yeşil Kutu (Onay) Olayını Dinle
    this.connection.on('RandevuOnaylandi', (randevuId: number) => {
      this.onayHandlers.forEach((h) => h(randevuId));
    });

    this.connection.onreconnected(() => {
      this.connectionId = this.connection?.connectionId ?? null;
      this.statusHandlers.forEach((h) => h(true));
    });

    this.connection.onclose(() => {
      this.connectionId = null;
      this.statusHandlers.forEach((h) => h(false));
    });

    await this.connection.start();
    this.connectionId = this.connection.connectionId ?? null;
    this.statusHandlers.forEach((h) => h(true));
  }

  async disconnect(): Promise<void> {
    if (!this.connection) return;
    await this.connection.stop();
    this.connection = null;
    this.connectionId = null;
  }
}

export const signalR = new SignalRService();
