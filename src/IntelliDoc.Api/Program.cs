using IntelliDoc.Modules.Audit;
using IntelliDoc.Modules.Audit.Endpoints;
using IntelliDoc.Modules.Extraction;
using IntelliDoc.Modules.Extraction.Endpoints;
using IntelliDoc.Modules.Identity;
using IntelliDoc.Modules.Identity.Endpoints;
using IntelliDoc.Modules.Intake; // Modülü kullanmak için
using IntelliDoc.Modules.Intake.Endpoints; // Controller'ý bulmak için
using IntelliDoc.Modules.Search;
using IntelliDoc.Modules.Search.Endpoints;
using IntelliDoc.Shared.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// 1. ALTYAPIYI YÜKLE
builder.Services.AddSharedInfrastructure(builder.Configuration,
    typeof(IntelliDoc.Modules.Extraction.ExtractionModuleExtensions).Assembly,
    typeof(IntelliDoc.Modules.Audit.AuditModuleExtensions).Assembly,
    typeof(IntelliDoc.Modules.Search.SearchModuleExtensions).Assembly
);

// 2. MODÜLLERÝ YÜKLE
builder.Services.AddIntakeModule(builder.Configuration);

builder.Services.AddExtractionModule(builder.Configuration);

builder.Services.AddAuditModule(builder.Configuration);

builder.Services.AddSearchModule(builder.Configuration);

builder.Services.AddIdentityModule(builder.Configuration);

// 3. CONTROLLER'LARI TANIT (ÝÞTE BURASI EKSÝKTÝ VEYA HATALIYDI)
// API'ye diyoruz ki: "Sadece kendine bakma, Intake modülündeki Controller'larý da al."
builder.Services.AddControllers()
    .AddApplicationPart(typeof(DocumentsController).Assembly)
    .AddApplicationPart(typeof(ExtractionController).Assembly)
    .AddApplicationPart(typeof(AuditController).Assembly)
    .AddApplicationPart(typeof(SearchController).Assembly)
    .AddApplicationPart(typeof(AuthController).Assembly
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

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// 4. CONTROLLER'LARI HARÝTALA
app.MapControllers();

// HealthCheck (Senin gördüðün tek endpoint buydu)
app.MapGet("/", () => Results.Ok(new { Status = "Online", System = "IntelliDoc" }));

app.Run();