using OtoServis.Data.Models;
using OtoServis.Data.Repositories;
using OtoServisApi.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly; // Polly kütüphanesini ekledik
using Polly.Retry; // AsyncPolicy için eklendi
using OtoServerApi.Services;


namespace OtoServisApi.Services
{
    public class AiTriageService : IAiTriageService
    {
        private readonly IRepository<HizmetTipi> _hizmetRepository;
        private readonly IRepository<Randevu> _randevuRepository;
        private readonly IRepository<Arac> _aracRepository;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AiTriageService> _logger; // Polly logları için eklendi
        private readonly AsyncRetryPolicy _retryPolicy; // Polly Politikamız

        public AiTriageService(
            IRepository<HizmetTipi> hizmetRepository,
            IRepository<Randevu> randevuRepository,
            IRepository<Arac> aracRepository,
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<AiTriageService> logger)
        {
            _hizmetRepository = hizmetRepository;
            _randevuRepository = randevuRepository;
            _aracRepository = aracRepository;
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            // Constructor çalıştığında politikamızı hazırlıyoruz
            _retryPolicy = GetRetryPolicy();
        }

        public async Task<AiAnalizSonucuDto> SikayetiAnalizEtAsync(string sikayetMetni, string plaka)
        {
            var hizmetler = await _hizmetRepository.GetAllAsync();
            var hizmetListesi = string.Join(", ", hizmetler.Select(h => $"[ID: {h.Id}, Ad: {h.Ad}]"));

            string aracBilgisi = "Araç marka/model bilgisi sistemde kayıtlı değil.";
            string aracGecmisiMetni = "Bu aracın sistemimizde geçmiş bir servis kaydı bulunmamaktadır.";

            var arac = await _aracRepository.GetAsync(a => a.Plaka == plaka);

            if (arac != null)
            {
                aracBilgisi = $"Araç: {arac.UretimYili} model {arac.MarkaModel}";

                var tumRandevular = await _randevuRepository.GetAllAsync();
                var gecmisRandevular = tumRandevular
                    .Where(r => r.AracId == arac.Id && r.Durum == RandevuDurumu.Tamamlandi)
                    .ToList();

                if (gecmisRandevular.Any())
                {
                    var gecmisIslemler = gecmisRandevular.Select(r =>
                        $"{r.Tarih.ToShortDateString()} tarihinde '{hizmetler.FirstOrDefault(h => h.Id == r.HizmetTipiId)?.Ad}' işlemi yapılmıştır.");

                    aracGecmisiMetni = string.Join(" | ", gecmisIslemler);
                }
            }

            string apiKey = _configuration["AiSettings:ApiKey"];

            var requestBody = new
            {
                model = "openai/gpt-oss-20b",
                response_format = new { type = "json_object" },
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = $@"Sen uzman bir oto servis şefisin. 
                               Elimizdeki hizmetler şunlardır: {hizmetListesi}.
                           
                               MÜŞTERİ ARACI: {aracBilgisi}
                               ARACIN GEÇMİŞ SERVİS BİLGİSİ:
                               {aracGecmisiMetni}
                           
                               GÖREV: Müşterinin aracının marka/modelini, geçmiş servis kayıtlarını VE YENİ şikayetini birlikte değerlendirerek sorunun kaynağını bul ve listemizden EN UYGUN hizmeti seç. Eğer şikayet geçmişte yapılan bir işlemle veya aracın kronik bir sorunuyla bağlantılıysa bunu mutlaka analiz et.
                           
                               Cevabını SADECE şu JSON yapısında ver:
                               {{
                                   ""id"": (seçtiğin hizmetin ID numarası),
                                   ""aciklama"": ""(Müşteriye gösterilecek, aracın modeline ve geçmişine de atıfta bulunan güven veren kibar 2 cümlelik açıklama)""
                               }}"
                    },
                    new { role = "user", content = sikayetMetni }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            try
            {
                // POLLY KALKANI DEVREDE! İsteği ExecuteAsync içine aldık.
                var response = await _retryPolicy.ExecuteAsync(async () =>
                {
                    var httpResponse = await _httpClient.PostAsync("https://api.groq.com/openai/v1/chat/completions", jsonContent);

                    // Eğer sunucu 500 serisi (Çöktü vb) hata dönerse, manuel olarak Exception fırlatıyoruz ki Polly yakalayıp tekrar denesin
                    if ((int)httpResponse.StatusCode >= 500)
                    {
                        throw new HttpRequestException($"Yapay zeka sunucusu hatası: {httpResponse.StatusCode}");
                    }

                    return httpResponse;
                });

                if (!response.IsSuccessStatusCode)
                {
                    var ilkHizmet = hizmetler.First();
                    return new AiAnalizSonucuDto
                    {
                        OnerilenHizmetId = ilkHizmet.Id,
                        OnerilenHizmetAdi = "Bağlantı Hatası",
                        YapayZekaAciklamasi = $"HTTP {(int)response.StatusCode} hatası alındı. Sorun genel kontrole aktarıldı."
                        
                    };
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var aiResult = JsonDocument.Parse(responseString);
                var jsonCevapString = aiResult.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                var aiCevabi = JsonSerializer.Deserialize<AiCevapModeli>(jsonCevapString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var secilenHizmet = hizmetler.FirstOrDefault(h => h.Id == aiCevabi.Id) ?? hizmetler.First();
                return new AiAnalizSonucuDto
                {
                    OnerilenHizmetId = secilenHizmet.Id,
                    OnerilenHizmetAdi = secilenHizmet.Ad,
                    YapayZekaAciklamasi = aiCevabi.Aciklama,
                };
            }
            catch (Exception ex)
            {
                // Polly 3 kere denedi ve hepsi başarısız olduysa kod buraya düşer. Fallback senaryosunu işletiyoruz!
                _logger.LogError($"Polly tüm denemeleri yaptı ancak AI servisine ulaşılamadı. Hata: {ex.Message}");

                return new AiAnalizSonucuDto
                {
                    OnerilenHizmetId = hizmetler.First().Id, // "Genel Kontrol" gibi varsayılan bir ID
                    OnerilenHizmetAdi = "Genel Kontrol",
                    YapayZekaAciklamasi = "Yapay zeka servisimiz şu an yanıt vermiyor, ancak arıza kaydınız ustalarımız tarafından incelenmek üzere başarıyla alınmıştır."
                    
                };
            }
        }

        // POLLY POLİTİKASI (HTTP ve API İstekleri İçin Optimize Edildi)
        private AsyncRetryPolicy GetRetryPolicy()
        {
            return Policy
                .Handle<HttpRequestException>() // Ağ koptuysa veya Groq 500 hatası verdiyse
                .Or<TimeoutException>()         // Zaman aşımı olduysa
                .Or<TaskCanceledException>()    // HttpClient süreyi aşıp görevi iptal ettiyse
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2, 4, 8 saniye bekler
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning($"AI Servisi Bağlantı Hatası: {exception.Message}. {timeSpan.TotalSeconds} saniye sonra tekrar denenecek. (Deneme {retryCount}/3)");
                    });
        }

        private class AiCevapModeli
        {
            public int Id { get; set; }
            public string Aciklama { get; set; }

        }
    }
}
