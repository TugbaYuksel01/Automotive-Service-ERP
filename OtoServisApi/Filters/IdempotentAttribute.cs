using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Threading.Tasks;

namespace OtoServisApi.Filters
{
    [AttributeUsage(AttributeTargets.Method)]
    public class IdempotentAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // 1. İstekte "Idempotency-Key" header'ı var mı kontrol et(GUID kontrolü ,doğrudan endpointe gelen isteklerin düşmesini sağlar böylece db patlamaz doğrudan endpointe yapılan saldırılarla)
            if (!context.HttpContext.Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKey))
            {
                context.Result = new BadRequestObjectResult(new { Mesaj = "Idempotency-Key header'ı eksik. İşlem reddedildi." });
                return;
            }

            // 2. Redis Cache servisini araya girip (Dependency Injection) çağır
            //Sistem bu kodu görünce "İyi de ben cache (önbellek) olarak neyi kullanacağım? Memory mi, Redis mi, SQL mi?" diye sorar. İşte bunu Program.cs'te belirtmemiz lazım.
            var cache = context.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();
            string cacheKey = $"Idempotency_{idempotencyKey}";

            // 3. Bu anahtar daha önce Redis'e kaydedilmiş mi bak
            var isProcessed = await cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(isProcessed))
            {
                // Eğer varsa, veritabanına inmeden işlemi burada kes ve 409 Conflict dön
                context.Result = new ConflictObjectResult(new { Mesaj = "Bu işlem daha önce başarıyla gerçekleştirildi. Mükerrer kayıt engellendi." });
                return;
            }

            // 4. Eğer Redis'te yoksa, Action'ın (Randevu Kaydetme) çalışmasına izin ver
            var executedContext = await next();

            // 5. İşlem başarılı olduysa (hata fırlatılmadıysa), anahtarı Redis'e kaydet (Örn: 24 saat tut)
            if (executedContext.Exception == null)
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                };
                await cache.SetStringAsync(cacheKey, "Processed", options);
            }
        }
    }
}