# Oto Servis Randevu ve Yapay Zeka Triyaj Sistemi

Bu proje, standart bir "adınızı ve saatinizi seçin" formundan ziyade, bir oto tamirhanesinin arka plandaki karmaşık operasyonlarını dijitalleştiren tam kapsamlı bir randevu sistemidir. Monorepo yaklaşımıyla geliştirilmiş olup, arka planda C# / ASP.NET Core, ön yüzde ise React kullanmaktadır.

## Sistem Nasıl Çalışıyor?

- **Yapay Zeka Tabanlı Triyaj ve Hata Toleransı:** Müşteri aracın sorununu kendi kelimeleriyle ifade ettiğinde, sistem bu metni aracın geçmiş servis kayıtlarıyla birleştirerek Groq API üzerinden çalışan yapay zekaya iletir. Yapay zeka, şikayeti atölyenin hizmet listesindeki en uygun kategoriyle eşleştirir. AI sunucularında bir kesinti yaşanması ihtimaline karşı Polly kütüphanesiyle üç aşamalı bir yeniden deneme mekanizması kurulmuştur. Tüm denemelere rağmen bağlantı kurulamazsa, sistem müşteriyi mağdur etmemek adına işlemi otomatik olarak "Genel Kontrol" kategorisine atar.
- **Katı Kapasite Yönetimi ve Alternatif Saat Üretimi:** Servisteki her işlem "1 Usta = 1 Araç = 1 İstasyon (Lift)" kuralına göre değerlendirilir ve kapasite aşımları sıfıra indirilir. Sistem sadece o anın boşluğunu değil, hesaplanan işlem süresinin 09:00 - 18:00 mesai saatlerini taşıp taşmadığını da kontrol eder. İstenen saatte atölye doluysa, algoritma öğle arası (12:00-13:00) ve mesai dışı saatleri atlayarak 15 dakikalık periyotlarla tarama yapar ve müşteriye en yakın boş saati kesin olarak hesaplayıp sunar.
- **Otonom Kaynak Planlama Motoru:** Arka planda her 60 saniyede bir çalışan sürekli bir motor bulunur. Bu motor üç ana fazda çalışır: İlk olarak boştaki randevuları uygun uzmanlıktaki ustalar ve boş liftlerle eşleştirir. İkinci fazda, kaza veya çekici gibi "Acil" etiketli araçlar geldiğinde sistem minimum mağduriyet senaryosunu hesaplar. Acil aracın önünü açmak için normal randevuları çakışma durumuna göre sırasıyla farklı bir ustaya devreder, saatini erteler veya ertesi sabah "VIP" statüsüne kaydırır. Bu değişikliklerde müşteri mağduriyeti yaşanmaması için arka planda RabbitMQ üzerinden anında bilgilendirme e-postası (Yeni Saat, Tarih, Erteleme Nedeni) gönderilir. Üçüncü fazda (Trafik Polisi) ise, randevusuna 5 dakika kalan bir işin ustası hala başka bir araçla meşgulse, sistemi kilitlememek için bekleyen aracı anında boştaki başka bir uzmana otonom olarak devreder.
- **Asenkron Operasyonlar ve Performans (RabbitMQ & Redis):** Ana API'nin müşteriye anında yanıt dönebilmesi için ağır işlemler asenkron mimariye taşınmıştır. Fatura kesimi (QuestPDF ile), şifre sıfırlama, randevu onay ve kriz erteleme bildirimleri gibi tüm e-posta operasyonları MassTransit aracılığıyla RabbitMQ kuyruğuna aktarılır ve izole bir Worker servisi tarafından arka planda işlenir. Ayrıca hizmet tipleri ve yedek parça listeleri gibi sık okunan veriler ile token güvenliği için kara liste kontrolleri Redis üzerinde önbelleklenerek veritabanı yorgunluğu engellenmiştir.
- **Misafir ve Kayıtlı Kullanıcı Akışı:** Sistemi daha önce kullanan müşteriler JWT token ile tanınır ve araç bilgileri veritabanından otomatik çekilir. İlk kez gelen misafirler ise doğrudan form üzerinden ilerler. Günün sonunda her iki kullanıcı tipi de aynı ortak kapasite ve rezervasyon algoritmasından güvenle geçer.

## Kullanılan Teknolojiler

**Backend:**
- C# / ASP.NET Core Web API
- Entity Framework Core (Code-First)
- MS SQL Server
- JWT (JSON Web Token) Kimlik Doğrulaması
- RabbitMQ & MassTransit (Mesaj Kuyruğu)
- Redis (Önbellekleme)
- Serilog (Loglama)
- Polly (Hata Toleransı)
- QuestPDF (Fatura Üretimi)

**Frontend:**
- React.js
- TypeScript & Vite
- Axios (Senkron API iletişimi için)

## Kurulum ve Çalıştırma

1. Projeyi bilgisayarınıza klonlayın.
2. Backend (OtoServisApi) tarafında veritabanını oluşturmak için Package Manager Console üzerinden "Update-Database" komutunu çalıştırın.
3. Projede güvenlik amacıyla API şifreleri gizlenmiştir. Kendi yapay zeka API anahtarınızı projenin .NET User Secrets kısmına "AiSettings:ApiKey" olarak eklemeniz gerekmektedir.
4. RabbitMQ ve Redis sunucularınızın ayakta olduğundan emin olun.
5. Backend uygulamasını Visual Studio üzerinden çalıştırın.
6. Ayrı bir Worker servisi olan `OtoServis.Worker` projesini de eşzamanlı olarak çalıştırın.
7. Frontend klasörüne geçiş yapın ve terminalde önce "npm install", ardından "npm run dev" komutlarını çalıştırarak kullanıcı arayüzünü ayağa kaldırın.

---
Geliştirici: Tuğba Yüksel
