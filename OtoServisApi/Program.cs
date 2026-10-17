using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using OtoServerApi.Services;
using OtoServis.Contracts.Events;
using OtoServis.Data;
using OtoServis.Data.Repositories;
using OtoServisApi.Services;
using Serilog;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

// 1. Uygulama ayağa kalkmadan hemen önce Serilog yapılandırmasını başlatıyoruz
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
   // .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    //MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
    //.MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
    //.MinimumLevel.Override("OtoServis.Worker.KaynakPlanlamaWorker", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.FromLogContext()
    .WriteTo.Console() // Logları eski usül konsola da yazdır
    .WriteTo.Seq("http://localhost:5341") // Logları Docker'daki Seq paneline gönder
    .CreateLogger();

try
{
    Log.Information("Uygulama başlatılıyor...");

    var builder = WebApplication.CreateBuilder(args);

    // =======================================================
    // 1. SERVİS KAYITLARI (DEPENDENCY INJECTION VE AYARLAR)
    // =======================================================

    // JSON Ayarları
    builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
    builder.Services.AddSignalR();

    // Veritabanı Bağlantısı
    builder.Services.AddDbContext<ServisDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // JWT Ayarları ve Kimlik Doğrulama
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var gizliAnahtar = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            // 1. JWT Ayarları (Hatayı çözen kısım)
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                ValidAudience = builder.Configuration["JwtSettings:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"]!))
            };

            //2.Redis Kara Liste Kontrolü(Bahsettiğim options.Events kısmı)
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var cache = context.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();
                    var rawToken = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "").Trim();

                    if (!string.IsNullOrEmpty(rawToken))
                    {
                        var isBlacklisted = await cache.GetStringAsync($"blacklist:{rawToken}");
                        if (!string.IsNullOrEmpty(isBlacklisted))
                        {
                            // Token Redis'te kara listedeyse isteği hemen kapıda reddet
                            context.Fail("Bu oturum sonlandırılmıştır. Lütfen tekrar giriş yapın.");
                        }
                    }
                }
            };
        });
    builder.Services.AddAuthorization();

    // Swagger ve JWT "Authorize" (Kilit) Butonu Ayarları
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Token'ınızı şu formatta girin: Bearer {token_buraya}",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
        });
    });

   

   


    // Proje İçi Servisler ve Dependency Injection
    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    builder.Services.AddScoped<IRandevuRepository, RandevuRepository>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IRandevuService, RandevuService>();
    builder.Services.AddScoped<IKullaniciService, KullaniciService>();
    builder.Services.AddScoped<IPdfFaturaService, PdfFaturaService>();
    builder.Services.AddScoped<IAiTriageService, AiTriageService>();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis");
        options.InstanceName = "OtoServis_";
    });

    builder.Services.AddMassTransit(x =>
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host("localhost", "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });
        });
    });

    // Dış İstekler ve Arka Plan Görevleri
    builder.Services.AddHttpClient();
    builder.Services.AddHostedService<KaynakPlanlamaMotoru>();
    // React frontend'ine API ile konuşma yetkisi veriyoruz (VIP Liste)
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("ReactAppPolicy", policy =>
        {
            // React projenin tam çalıştığı adresi buraya yazmalısın (Sonunda / işareti olmasın)
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()  // Her türlü başlığa izin ver
                  .AllowAnyMethod(); // GET, POST, PUT, DELETE hepsine izin ver
        });
    });

   

    //"SabitZamanliKural" adında, 10 saniyede maksimum 5 isteğe izin veren, sınırı aşanı da sıraya sokmadan anında 429 Too Many Requests hatasıyla reddeden bir politika oluşturuyoruz.
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("SabitZamanliKural", opt =>
        {
            opt.Window = TimeSpan.FromSeconds(10);
            opt.PermitLimit = 40; // <-- 5 yerine 40 yapıyoruz
            opt.QueueLimit = 0;
        });
    });

    // Sistemin "Ben hayattayım ve çalışıyorum" diyebilmesi için sağlık kontrolü servisini (Health Check) belleğe ekliyoruz.
    // Canlıya çıktığımızda UptimeRobot gibi sistemler uygulamamızın çöküp çökmediğini bu servis sayesinde anlayacak.
    builder.Services.AddHealthChecks();

    // =======================================================
    // 2. UYGULAMANIN İNŞASI VE ARA YAZILIM (MIDDLEWARE) HATTI
    // =======================================================
    var app = builder.Build();

    // ===============================================================
    // UYGULAMA ÇÖKME KALKANI (GLOBAL ERROR HANDLER)
    // ===============================================================
    // Eğer uygulamanın derinliklerinde (veritabanı kopması, null referans vb.) ölümcül bir hata (Exception) fırlatılırsa,
    // bu kalkan devreye girer. Kırmızı C# hata kodlarının (Stack Trace) müşterinin ekranına sızmasını kesin olarak engeller.
    app.UseExceptionHandler(appError =>
    {
        appError.Run(async context =>
        {
            // Müşteriye döneceğimiz HTTP durum kodunu her zaman "500 - Sunucu Hatası" olarak sabitliyoruz.
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
            if (contextFeature != null)
            {
                // Hatanın teknik detayları (contextFeature.Error) sunucuda gizli kalır ve GlobalAuditMiddleware tarafından loglanır.
                // Müşteriye ise panik yaratmamak için sadece aşağıdaki kibar ve standart JSON mesajı fırlatılır.
                var errorResponse = new { Mesaj = "Sunucu tarafında beklenmeyen bir hata oluştu. Teknik ekip bilgilendirildi." };
                await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
            }
        });
    });




    // Geliştirme ortamı ayarları
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    // ==========================================
    // HTTP GÜVENLİK BAŞLIKLARI (SECURITY HEADERS)
    // ==========================================
    // Bu middleware (ara yazılım), API'mizin dış dünyaya verdiği her yanıta 
    // otomatik olarak dijital güvenlik kuralları ekler.
    app.Use(async (context, next) =>
    {
        // 1. CLICKJACKING (Tıklama Hırsızlığı) KORUMASI
        // Kötü niyetli bir sitenin, bizim projemizi kendi sayfası içine 
        // görünmez bir çerçeve (<iframe>) olarak gömüp kullanıcıları 
        // yanlışlıkla bizim butonlarımıza tıklatmaya zorlamasını kesinlikle (DENY) yasaklar.
        context.Response.Headers.Add("X-Frame-Options", "DENY");

        // 2. MIME-SNIFFING (Dosya Türü Koklama) KORUMASI
        // Tarayıcıya şu emri verir: "Ben sana bu dosya bir metin veya resim diyorsam odur. 
        // İçeriğine bakıp (sniff) uyanıklık yaparak zararlı bir script gibi çalıştırmaya kalkma!"
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

        // 3. TEMEL XSS (Cross-Site Scripting) KORUMASI
        // Tarayıcının içine gömülü olan yerleşik XSS filtresini açar (1). 
        // Eğer sayfada zararlı bir script sızması tespit edilirse, sayfayı 
        // temizlemeye çalışmak yerine riske girmeyip işlemi tamamen durdurur (mode=block).
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

        // Başlıklar eklendikten sonra, isteğin takılmadan normal 
        // yolculuğuna (diğer middleware'lere veya controller'a) devam etmesini sağlar.
        await next();
    });

    // NOT: Bu kod bloğunu Program.cs içinde 'app.UseHttpsRedirection();' satırının hemen 
    // altına, kontrolörler haritalanmadan (app.MapControllers) önce yapıştırmalısın.
    
    app.UseStaticFiles();

    app.UseRouting();

    // CORS VIP listesini tam bu noktada aktif ediyoruz
    app.UseCors("ReactAppPolicy");

    // GÜVENLİK 
    app.UseAuthentication();
    app.UseMiddleware<OtoServisApi.Middlewares.GlobalAuditMiddleware>();
    app.UseRateLimiter();
    app.UseAuthorization();
    
    app.MapControllers();

    // Sistemin sağlık durumunu dış dünyaya açtığımız uç nokta (endpoint).
    // Tarayıcıdan "/api/health" adresine girildiğinde, sistem sorunsuz çalışıyorsa ekrana sadece "Healthy" (Sağlıklı) yazar.
    app.MapHealthChecks("/api/health");

    //istemcilerin bağlanacağı adres (endpoint)
    app.MapHub<OtoServisApi.Hubs.NotificationHub>("/notificationHub");
       

    app.Run();

   
}
catch (Exception ex)
{
    Log.Fatal(ex, "Uygulama başlatılırken kritik bir hata oluştu.");
}
finally
{
    Log.CloseAndFlush();
}
