using Xunit;
using Moq;
using OtoServisApi.Services;     // RandevuService buradadır
using OtoServisApi.Repositories; // IRandevuRepository buradadır
using OtoServisApi.DTOs;         // RandevuOlusturDto buradadır
using OtoServisApi.Models;       // Randevu modeli buradadır

namespace OtoServisApi.Tests
{
    public class RandevuServiceTests
    {
        [Fact]
        public async Task RandevuOlusturAsync_GecerliDtoIleCagrildiginda_TrueDonmeliVeRepoAddCalismali()
        {
            // 1. HAZIRLIK (Arrange)
            // IRandevuRepository arayüzünün sahte (mock) bir kopyasını üretiyoruz
            var mockRepo = new Mock<IRandevuRepository>();

            // Test için örnek bir DTO girdisi hazırlıyoruz
            var testDto = new RandevuOlusturDto
            {
                
                 //(Yıl, Ay, Gün, Saat, Dakika, Saniye)
                Tarih = new DateTime(2026, 11, 1, 14, 0, 0),
                MusteriNotu = "Test notudur",
                MusteriId = 1,
                HizmetTipiId = 2
            };

            // Servisimizi, gerçek veritabanı yerine bu SAHTE repo ile başlatıyoruz
            var service = new RandevuService(mockRepo.Object);

            // 2. ÇALIŞTIRMA (Act)
            var sonuc = await service.RandevuOlusturAsync(testDto);

            // 3. DOĞRULAMA (Assert)
            // A) Metot başarıyla true dönmüş mü?
            Assert.True(sonuc);

            // B) KRİTİK KISIM (Mocking Doğrulaması): 
            // Servis gerçekten de arka planda repository'nin AddAsync metodunu tetiklemiş mi?
            // "It.IsAny<Randevu>()" -> Herhangi bir Randevu nesnesi ile AddAsync çağrıldı mı diye kontrol eder.
            mockRepo.Verify(repo => repo.AddAsync(It.IsAny<Randevu>()), Times.Once);
        }
    }
}