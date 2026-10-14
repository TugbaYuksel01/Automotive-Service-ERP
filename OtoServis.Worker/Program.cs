using MassTransit;
using Microsoft.EntityFrameworkCore;
using OtoServis.Data;
using OtoServis.Data.Repositories;
using OtoServis.Worker.Consumers;
using OtoServis.Worker.Services;
using Polly;
using Polly.Extensions.Http;



//dependency injection(servislerin vs bağlandığı kısım) ve worker service'in çalıştırıldığı kısım
var builder = Host.CreateApplicationBuilder(args);
// Veritabanı Bağlantısı
builder.Services.AddDbContext<ServisDbContext>(options =>
 options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// Mail Servisini Kaydet
builder.Services.AddScoped<IMailService, EmailService>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IRandevuRepository, RandevuRepository>();

// MassTransit ve RabbitMQ Dinleyicisini Kaydet
builder.Services.AddMassTransit(x =>
{
    // 1. Randevu dinleyicisi
    x.AddConsumer<RandevuOlusturulduConsumer>();

    // 2. YENİ: Fatura dinleyicisi
    x.AddConsumer<FaturaKesildiConsumer>();

    x.AddConsumer<RandevuKaydirildiConsumer>();


    x.AddConsumer<SifreSifirlamaIstendiConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();
