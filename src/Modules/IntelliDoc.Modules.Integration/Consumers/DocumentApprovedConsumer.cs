using IntelliDoc.Modules.Integration.Data;
using IntelliDoc.Modules.Integration.Services;
using IntelliDoc.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelliDoc.Modules.Integration.Consumers;

public class DocumentApprovedConsumer : IConsumer<IDocumentApproved>
{
    private readonly IntegrationDbContext _dbContext;
    private readonly WebhookSender _sender;

    public DocumentApprovedConsumer(IntegrationDbContext dbContext, WebhookSender sender)
    {
        _dbContext = dbContext;
        _sender = sender;
    }

    public async Task Consume(ConsumeContext<IDocumentApproved> context)
    {
        var msg = context.Message;

        // 1. Kullanıcının Webhook ayarını bul
        var webhook = await _dbContext.Webhooks
            .FirstOrDefaultAsync(x => x.UserId == msg.UserId && x.IsActive);

        if (webhook != null)
        {
            Console.WriteLine($"[Integration] Webhook Tetikleniyor -> {webhook.EndpointUrl}");

            // 2. Veriyi Gönder
            await _sender.SendAsync(webhook.EndpointUrl, new
            {
                Event = "DocumentApproved",
                DocumentId = msg.DocumentId,
                Data = msg.FinalJsonData // Onaylanmış temiz veri
            });
        }
    }
}