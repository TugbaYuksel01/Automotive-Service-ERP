using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace OtoServisApi.Hubs
{
    // Bu sınıf, müşterilerin anlık bildirimleri dinleyeceği radyo istasyonumuzdur.
    public class NotificationHub : Hub
    {
        // Müşteri sayfaya girip WebSocket'e bağlandığında tetiklenir (İsteğe bağlı loglama için)
        public override async Task OnConnectedAsync()
        {
            // İleride bağlanan kişinin ConnectionId'si ile özel işlemler yapabiliriz
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        // Worker servisi işi bitirdiğinde(ai analizi sonucu dış servisten geldiği zaman) dışarıdan bu metodu tetikleyecek
        public async Task TehisSonucunuYayinla(string sonucMesaji, string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId))
            {
                // Eğer ID yoksa herkese gönder (Test için veya genel duyurular için)
                await Clients.All.SendAsync("ReceiveTehis", sonucMesaji);
            }
            else
            {
                // ID varsa, NOKTA ATIŞI sadece o müşterinin tarayıcısına fırlat!
                await Clients.Client(connectionId).SendAsync("ReceiveTehis", sonucMesaji);
            }

        }
    }
}
