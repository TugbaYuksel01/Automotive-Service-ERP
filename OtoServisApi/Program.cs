using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

            // 2. Redis Kara Liste Kontrolü (Bahsettiğim options.Events kısmı)
            //options.Events = new JwtBearerEvents
            //{
            //    OnTokenValidated = async context =>
            //    {
            //        var cache = context.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();
            //        var rawToken = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "").Trim();

            //        if (!string.IsNullOrEmpty(rawToken))
            //        {
            //            var isBlacklisted = await cache.GetStringAsync($"blacklist:{rawToken}");
            //            if (!string.IsNullOrEmpty(isBlacklisted))
            //            {
            //                // Token Redis'te kara listedeyse isteği hemen kapıda reddet
            //                context.Fail("Bu oturum sonlandırılmıştır. Lütfen tekrar giriş yapın.");
            //            }
            //        }
            //    }
            //};
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

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            // AllowAnyOrigin YERİNE bunu kullanıyoruz. Hem her şeye izin verir, hem çökmez.
            policy.SetIsOriginAllowed(origin => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // SignalR için mecburi
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


    // =======================================================
    // 2. UYGULAMANIN İNŞASI VE ARA YAZILIM (MIDDLEWARE) HATTI
    // =======================================================
    var app = builder.Build();
      

        // Geliştirme ortamı ayarları
        if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    app.UseStaticFiles();

    app.UseRouting();

    app.UseCors("AllowFrontend");

    // GÜVENLİK 
    app.UseAuthentication();
    app.UseMiddleware<OtoServisApi.Middlewares.GlobalAuditMiddleware>();
    app.UseAuthorization();
    
    app.MapControllers();

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