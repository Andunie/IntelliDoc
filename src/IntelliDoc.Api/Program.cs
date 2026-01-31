using IntelliDoc.Modules.Audit;
using IntelliDoc.Modules.Audit.Endpoints;
using IntelliDoc.Modules.Extraction;
using IntelliDoc.Modules.Extraction.Endpoints;
using IntelliDoc.Modules.Identity;
using IntelliDoc.Modules.Identity.Endpoints;
using IntelliDoc.Modules.Intake;
using IntelliDoc.Modules.Intake.Endpoints;
using IntelliDoc.Modules.Search;
using IntelliDoc.Modules.Search.Endpoints;
using IntelliDoc.Shared.Extensions;
using Microsoft.OpenApi.Models;
using IntelliDoc.Modules.Integration.Endpoints;
using IntelliDoc.Modules.Integration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000") // Frontend adresi
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// 1. ALTYAPIYI YÜKLE
builder.Services.AddSharedInfrastructure(builder.Configuration,
    typeof(IntelliDoc.Modules.Extraction.ExtractionModuleExtensions).Assembly,
    typeof(IntelliDoc.Modules.Audit.AuditModuleExtensions).Assembly,
    typeof(IntelliDoc.Modules.Search.SearchModuleExtensions).Assembly,
    typeof(IntelliDoc.Modules.Intake.IntakeModuleExtensions).Assembly,
    typeof(IntelliDoc.Modules.Integration.IntegrationModuleExtensions).Assembly
);

// 2. MODÜLLERÝ YÜKLE
builder.Services.AddIntakeModule(builder.Configuration);

builder.Services.AddExtractionModule(builder.Configuration);

builder.Services.AddAuditModule(builder.Configuration);

builder.Services.AddSearchModule(builder.Configuration);

builder.Services.AddIdentityModule(builder.Configuration);

builder.Services.AddIntegrationModule(builder.Configuration);

// Email Botunu Arka Plan Servisi Olarak Baþlat
builder.Services.AddHostedService<IntelliDoc.Modules.EmailIngestion.Services.EmailListenerService>();

builder.Services.AddControllers()
    .AddApplicationPart(typeof(DocumentsController).Assembly)
    .AddApplicationPart(typeof(ExtractionController).Assembly)
    .AddApplicationPart(typeof(AuditController).Assembly)
    .AddApplicationPart(typeof(SearchController).Assembly)
    .AddApplicationPart(typeof(AuthController).Assembly)
    .AddApplicationPart(typeof(AnalyticsController).Assembly)
    .AddApplicationPart(typeof(SettingsController).Assembly
);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "IntelliDoc API", Version = "v1" });

    // JWT Ayarý (Kilit Butonu Ýçin)
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT Bearer token **_only_**",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer", // Lowercase olmalý
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = "Bearer",
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, new string[] { } }
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// 4. CONTROLLER'LARI HARÝTALA
app.MapControllers();

// HealthCheck (Senin gördüðün tek endpoint buydu)
app.MapGet("/", () => Results.Ok(new { Status = "Online", System = "IntelliDoc" }));

app.Run();