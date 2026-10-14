using OtoServis.Data.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OtoServisApi.Services
{
    public class PdfFaturaService : IPdfFaturaService
    {
        public PdfFaturaService()
        {
            // QuestPDF Community lisansıyla çalıştığımızı sisteme bildiriyoruz
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.UseSystemFonts = true; // Sisteme yüklü Arial vb. fontları kullanmasına izin ver
        }

        public async Task<string> FaturaUretVeKaydetAsync(Fatura fatura, Randevu randevu, List<YedekParca> kullanilanParcalar)
        {
            // 1. PDF'in kaydedileceği klasörü oluştur (Proje içinde "Invoices" klasörü)
            string klasorYolu = Path.Combine(Directory.GetCurrentDirectory(), "Invoices");
            if (!Directory.Exists(klasorYolu))
            {
                Directory.CreateDirectory(klasorYolu);
            }

            // Örn Dosya Adı: Fatura_35ABC123_20261008.pdf
            string dosyaAdi = $"Fatura_{randevu.Arac?.Plaka}_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
            string tamDosyaYolu = Path.Combine(klasorYolu, dosyaAdi);

            // 2. QuestPDF ile Fatura Çizimi
            var belge = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Lato"));
                    // ÜST BİLGİ (HEADER)
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("OTO SERVİS PRO").FontSize(24).SemiBold().FontColor(Colors.Blue.Darken2);
                            col.Item().Text("Sanayi Sitesi No: 42, İzmir").FontSize(10);
                            col.Item().Text("info@otoservispro.com").FontSize(10);
                        });

                        row.ConstantItem(100).AlignRight().Column(col =>
                        {
                            col.Item().Text($"Fatura No: #{fatura.Id}").Bold();
                            col.Item().Text($"Tarih: {fatura.KesimTarihi:dd.MM.yyyy}");
                        });
                    });

                    // ORTA GÖVDE (MÜŞTERİ VE ARAÇ BİLGİLERİ)
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        col.Item().PaddingBottom(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Müşteri Bilgileri:").SemiBold().Underline();
                                c.Item().Text(randevu.Kullanici?.AdSoyad ?? "Bilinmeyen Müşteri");
                                c.Item().Text(randevu.Kullanici?.Telefon ?? "");
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Araç Bilgileri:").SemiBold().Underline();
                                c.Item().Text($"Plaka: {randevu.Arac?.Plaka}");
                                c.Item().Text($"Model: {randevu.Arac?.UretimYili} {randevu.Arac?.MarkaModel}");
                                c.Item().Text($"İşlem: {randevu.HizmetTipi?.Ad}");
                            });
                        });

                        // FATURA TABLOSU (KALEMLER)
                        col.Item().Table(table =>
                        {
                            // Sütun yapılandırması
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30); // Sıra
                                columns.RelativeColumn();   // Ürün/Hizmet Adı
                                columns.ConstantColumn(80); // Miktar
                                columns.ConstantColumn(80); // Fiyat
                                columns.ConstantColumn(80); // Toplam
                            });

                            // Tablo Başlıkları
                            table.Header(header =>
                            {
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("#");
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("Açıklama");
                                header.Cell().BorderBottom(1).AlignRight().PaddingBottom(5).Text("Miktar");
                                header.Cell().BorderBottom(1).AlignRight().PaddingBottom(5).Text("Birim Fiyat");
                                header.Cell().BorderBottom(1).AlignRight().PaddingBottom(5).Text("Toplam");
                            });

                            // 1. Satır: İşçilik Ücreti
                            table.Cell().PaddingVertical(5).Text("1");
                            table.Cell().PaddingVertical(5).Text($"{randevu.HizmetTipi?.Ad} (İşçilik)");
                            table.Cell().AlignRight().PaddingVertical(5).Text("1");
                            table.Cell().AlignRight().PaddingVertical(5).Text($"{fatura.IscilikUcreti:C2}");
                            table.Cell().AlignRight().PaddingVertical(5).Text($"{fatura.IscilikUcreti:C2}");

                            // 2. Satırlar: Kullanılan Yedek Parçalar
                            int sira = 2;
                            foreach (var parca in kullanilanParcalar)
                            {
                                // Fatura için köprü tablodan kaç adet kullanıldığını buluyoruz
                                var miktar = randevu.RandevuParcalari.FirstOrDefault(rp => rp.YedekParcaId == parca.Id)?.KullanilanMiktar ?? 1;

                                table.Cell().PaddingVertical(5).Text(sira.ToString());
                                table.Cell().PaddingVertical(5).Text(parca.ParcaAdi);
                                table.Cell().AlignRight().PaddingVertical(5).Text(miktar.ToString());
                                table.Cell().AlignRight().PaddingVertical(5).Text($"{parca.BirimFiyat:C2}");
                                table.Cell().AlignRight().PaddingVertical(5).Text($"{(parca.BirimFiyat * miktar):C2}");
                                sira++;
                            }
                        });

                        // ALT TOPLAM BÖLÜMÜ
                        col.Item().PaddingTop(20).AlignRight().Column(c =>
                        {
                            c.Item().Text($"İşçilik Toplamı: {fatura.IscilikUcreti:C2}");
                            c.Item().Text($"Parça Toplamı: {fatura.ToplamParcaMaliyeti:C2}");
                            c.Item().Text($"GENEL TOPLAM: {fatura.GenelToplam:C2}").FontSize(14).SemiBold().FontColor(Colors.Red.Medium);
                        });

                        // ÖDEME VE BANKA BİLGİLERİ KUTUSU
                        col.Item().PaddingTop(25).Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                        {
                            c.Item().PaddingBottom(5).Text("Ödeme Bilgileri ve Talimatlar").FontSize(12).SemiBold().FontColor(Colors.Blue.Darken3);

                            c.Item().Text("Lütfen ödemenizi yaparken açıklama kısmına Fatura Numaranızı veya Plakanızı yazmayı unutmayınız.").FontSize(9).Italic();
                            c.Item().PaddingTop(5).Text("Banka Adı: Garanti BBVA - Sanayi Şubesi").FontSize(10);
                            c.Item().Text("Alıcı Adı: Oto Servis Pro A.Ş.").FontSize(10).SemiBold();
                            c.Item().Text("TL IBAN: TR88 0006 2000 0001 2345 6789 10").FontSize(10).SemiBold();

                            c.Item().PaddingTop(5).Text("Online ödeme yapmak için sistemimizdeki müşteri panelinize giriş yapıp kredi kartınızı kullanabilirsiniz.").FontSize(9);
                        });
                    });

                    // ALT BİLGİ (FOOTER)
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Sayfa ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });

                    
                });
            });

            // 3. Dosyayı diske kaydet
            belge.GeneratePdf(tamDosyaYolu);

            // Bu metot dosyanın tam yolunu dönecek ki veritabanına ve maile iletebilelim
            return await Task.FromResult(tamDosyaYolu);
        }
    }
}
