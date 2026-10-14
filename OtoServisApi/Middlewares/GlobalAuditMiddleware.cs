using Microsoft.AspNetCore.Http;
using OtoServis.Data; // DbContext'inin bulunduğu namespace 
using OtoServis.Data.Models;
using System;
using System.Security.Claims;
using System.Threading.Tasks;


namespace OtoServisApi.Middlewares
{
    public class GlobalAuditMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalAuditMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // DbContext'i constructor'da değil, InvokeAsync içinde alıyoruz çünkü DbContext "Scoped" (istek başına) çalışır.
        
        public async Task InvokeAsync(HttpContext context, ServisDbContext dbContext)
        {
            // 1. İsteği API'nin derinliklerine (Controller'a) gönder ve cevabı (Red veya Onay) bekle
            await _next(context);

            // 2. Sadece /api/ ile başlayan gerçek istekleri logla (Swagger arayüz dosyalarını es geçmek için)
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                var statusCode = context.Response.StatusCode;
                var metod = context.Request.Method;
                var yol = context.Request.Path;

                // Sadece GET (Okuma) isteklerini veya Hatalı/Yetkisiz (401, 403, 500 vb.) durumları logla 
                //Başarısız POST/PUT/DELETE (Durum Kodu 400, 401, 403, 500) durum kodları bunlar ve bu eylemler de Global Middleware tarafından yakalanıp loglanır
                // (POST, PUT, DELETE zaten başarılı olursa 1. kalkanımız olan DbContext tarafından detaylıca loglanıyor!)
                if (metod == "GET" || statusCode >= 400)
                {
                    // Kullanıcı veya IP tespiti
                    string aktifKullaniciId = "Ziyaretci IP: " + (context.Connection?.RemoteIpAddress?.ToString() ?? "Bilinmiyor");

                    if (context.User?.Identity?.IsAuthenticated == true)
                    {
                        aktifKullaniciId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                        ?? context.User.FindFirst("id")?.Value
                                        ?? "Bilinmeyen Kullanici";
                    }

                    // Durum koduna göre insancıl bir işlem tipi belirle
                    string islemTipi = statusCode switch
                    {
                        401 => "YETKİSİZ ERİŞİM (401)",
                        403 => "YASAKLI ERİŞİM (403)",
                        >= 500 => "SUNUCU HATASI (500)",
                        _ => "SİSTEM OKUMA (GET)"
                    };

                    // Log nesnesini oluştur
                    var yeniLog = new SistemLog
                    {
                        IslemTipi = islemTipi,
                        TabloAdi = "API_ENDPOINT",
                        Detay = $"{metod} isteği yapıldı. Hedef: {yol} | Sonuç Kodu: {statusCode}",
                        Tarih = DateTime.UtcNow.AddHours(3),
                        KullaniciId = aktifKullaniciId
                    };

                    dbContext.SistemLoglari.Add(yeniLog);
                    await dbContext.SaveChangesAsync();
                }
            }
        }
    }
}
