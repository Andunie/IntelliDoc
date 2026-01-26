using IntelliDoc.Modules.Identity.Entities;
using IntelliDoc.Modules.Intake.Data;
using IntelliDoc.Modules.Intake.Entities;
using IntelliDoc.Modules.Intake.Services;
using IntelliDoc.Shared.Events;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IntelliDoc.Modules.EmailIngestion.Services;

public class EmailListenerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _config;
    private readonly ILogger<EmailListenerService> _logger;

    // Sabitler
    private const long MAX_FILE_SIZE = 50 * 1024 * 1024; // 50 MB
    private const int MAX_RETRIES = 3;
    private const int CHECK_INTERVAL_MINUTES = 1;

    public EmailListenerService(
        IServiceProvider serviceProvider,
        IConfiguration config,
        ILogger<EmailListenerService> logger)
    {
        _serviceProvider = serviceProvider;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int retryCount = 0;

        _logger.LogInformation("[EmailBot] 🚀 Email dinleyici başlatıldı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckEmailsAsync();
                retryCount = 0; // Başarılı, sayacı sıfırla
            }
            catch (MailKit.Security.AuthenticationException authEx)
            {
                _logger.LogError(authEx, "[EmailBot] 🔐 Kimlik doğrulama hatası. Email ayarlarını kontrol edin.");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogError(ex, "[EmailBot] ❌ Hata ({RetryCount}/{MaxRetries})", retryCount, MAX_RETRIES);

                if (retryCount >= MAX_RETRIES)
                {
                    _logger.LogWarning("[EmailBot] 🛑 Maksimum deneme aşıldı. 10 dakika bekleniyor...");
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                    retryCount = 0;
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(CHECK_INTERVAL_MINUTES), stoppingToken);
        }
    }

    private async Task CheckEmailsAsync()
    {
        using var scope = _serviceProvider.CreateScope();

        // Servisleri al
        var intakeDbContext = scope.ServiceProvider.GetRequiredService<IntakeDbContext>();
        var minioService = scope.ServiceProvider.GetRequiredService<MinioStorageService>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Email ayarları kontrolü
        var settings = _config.GetSection("EmailSettings");
        if (string.IsNullOrEmpty(settings["MailServer"]) ||
            string.IsNullOrEmpty(settings["SenderEmail"]))
        {
            _logger.LogWarning("[EmailBot] ⚠️ Email ayarları eksik! appsettings.json kontrol edin.");
            return;
        }

        // IMAP Bağlantısı
        using var client = new ImapClient();

        // Gmail için SslOnConnect kullanılmalı (port 993)
        await client.ConnectAsync(
            settings["MailServer"],
            int.Parse(settings["MailPort"]),
            MailKit.Security.SecureSocketOptions.SslOnConnect);

        await client.AuthenticateAsync(
            settings["SenderEmail"],
            settings["Password"]);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite);

        var unreadMessages = await inbox.SearchAsync(SearchQuery.NotSeen);

        _logger.LogInformation("[EmailBot] 📬 {Count} okunmamış mail bulundu.", unreadMessages.Count);

        foreach (var uniqueId in unreadMessages)
        {
            var message = await inbox.GetMessageAsync(uniqueId);
            var senderEmail = message.From.Mailboxes.FirstOrDefault()?.Address;

            if (string.IsNullOrEmpty(senderEmail))
            {
                _logger.LogWarning("[EmailBot] ⚠️ Gönderen adresi bulunamadı. Mail atlanıyor.");
                await inbox.AddFlagsAsync(uniqueId, MessageFlags.Seen, true);
                continue;
            }

            _logger.LogInformation("[EmailBot] 📧 Yeni Mail: {Subject} - Gönderen: {Sender}",
                message.Subject, senderEmail);

            // Kullanıcı kontrolü
            var user = await userManager.FindByEmailAsync(senderEmail);
            if (user == null)
            {
                _logger.LogWarning("[EmailBot] ❌ Kullanıcı sistemde bulunamadı: {Email}", senderEmail);

                // Maili "Seen" olarak işaretle
                await inbox.AddFlagsAsync(uniqueId, MessageFlags.Seen, true);

                // İsterseniz buraya "Rejected" klasörüne taşıma ekleyebilirsiniz
                continue;
            }

            // Ekleri işle
            int processedCount = 0;
            foreach (var attachment in message.Attachments)
            {
                if (attachment is MimeKit.MimePart part && part.FileName != null)
                {
                    // Dosya tipi kontrolü
                    var ext = Path.GetExtension(part.FileName).ToLower();
                    if (ext != ".pdf" && ext != ".png" && ext != ".jpg" && ext != ".jpeg")
                    {
                        _logger.LogDebug("[EmailBot] ⏭️ Desteklenmeyen dosya tipi atlandı: {FileName}", part.FileName);
                        continue;
                    }

                    // Dosya boyutu kontrolü
                    if (part.Content.Stream.Length > MAX_FILE_SIZE)
                    {
                        _logger.LogWarning("[EmailBot] ⚠️ Dosya çok büyük ({FileName}): {Size} MB. Maksimum: {MaxSize} MB",
                            part.FileName,
                            part.Content.Stream.Length / 1024 / 1024,
                            MAX_FILE_SIZE / 1024 / 1024);
                        continue;
                    }

                    _logger.LogInformation("[EmailBot] 📎 Ek işleniyor: {FileName} ({Size} KB)",
                        part.FileName,
                        part.Content.Stream.Length / 1024);

                    try
                    {
                        // MinIO'ya yükle
                        using (var stream = new MemoryStream())
                        {
                            await part.Content.DecodeToAsync(stream);
                            stream.Position = 0;

                            var storagePath = await minioService.UploadFileAsync(
                                stream,
                                part.FileName,
                                part.ContentType.MimeType);

                            // Veritabanına kaydet
                            var document = new Document
                            {
                                Id = Guid.NewGuid(),
                                UploadedBy = user.Id,
                                OriginalFileName = part.FileName,
                                ContentType = part.ContentType.MimeType,
                                FileSize = stream.Length,
                                StoragePath = storagePath,
                                Status = DocumentStatus.Uploaded,
                                UploadedAt = DateTime.UtcNow
                            };

                            intakeDbContext.Documents.Add(document);
                            await intakeDbContext.SaveChangesAsync();

                            // Event fırlat
                            await publishEndpoint.Publish<IDocumentUploaded>(new
                            {
                                DocumentId = document.Id,
                                FileName = document.OriginalFileName,
                                FilePath = document.StoragePath,
                                UploadedBy = document.UploadedBy,
                                UploadedAt = document.UploadedAt
                            });

                            _logger.LogInformation("[EmailBot] ✅ Belge sisteme alındı: {DocumentId}", document.Id);
                            processedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[EmailBot] ❌ Dosya işlenirken hata: {FileName}", part.FileName);
                    }
                }
            }

            // Maili "Okundu" olarak işaretle
            await inbox.AddFlagsAsync(uniqueId, MessageFlags.Seen, true);

            if (processedCount > 0)
            {
                _logger.LogInformation("[EmailBot] 🎉 Mail işlendi: {Count} dosya yüklendi.", processedCount);
            }
        }

        await client.DisconnectAsync(true);
    }
}