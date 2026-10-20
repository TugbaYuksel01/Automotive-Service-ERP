using MassTransit;
using Microsoft.EntityFrameworkCore;
using OtoServis.Data;
using OtoServis.Data.Repositories;
using OtoServis.Worker.Consumers;
using OtoServis.Worker.Services;

var builder = WebApplication.CreateBuilder(args);

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
    x.AddConsumer<RandevuOlusturulduConsumer>();
    x.AddConsumer<FaturaKesildiConsumer>();
    x.AddConsumer<RandevuKaydirildiConsumer>();
    x.AddConsumer<SifreSifirlamaIstendiConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqConnectionString = builder.Configuration["RabbitMQ:ConnectionString"];

        if (string.IsNullOrWhiteSpace(rabbitMqConnectionString))
        {
            throw new InvalidOperationException("RabbitMQ bağlantı adresi bulunamadı.");
        }

        cfg.Host(new Uri(rabbitMqConnectionString));
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Render'ın otomatik atadığı PORT değişkenini yakalayıp dinlemesini sağlıyoruz (Ücretsiz Web Service kuralı)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

// Render sağlık kontrolü (health check) yaptığında 200 OK dönsün diye basit bir endpoint
app.MapGet("/", () => Results.Ok("OtoServis Worker is running and healthy!"));

app.Run();
