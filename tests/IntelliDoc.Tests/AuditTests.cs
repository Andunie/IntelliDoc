using FluentAssertions;
using IntelliDoc.Modules.Audit.Consumers;
using IntelliDoc.Modules.Audit.Data;
using IntelliDoc.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text.Json;
using Xunit;

namespace IntelliDoc.Tests;

public class AuditTests
{
    [Fact]
    public async Task Yüksek_Tutarli_Fatura_Denetimden_Kalmali()
    {
        // 1. ARRANGE
        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var dbContext = new AuditDbContext(options);

        var consumer = new AnalyzeDocumentConsumer(dbContext);

        var belgeId = Guid.NewGuid();
        var jsonPayload = JsonSerializer.Serialize(new
        {
            extracted_data = new
            {
                DocumentType = "Fatura",
                Entities = new { Amount = 15000 }
            }
        });

        var mockMessage = new Mock<IDataExtracted>();
        mockMessage.Setup(x => x.DocumentId).Returns(belgeId);
        mockMessage.Setup(x => x.JsonData).Returns(jsonPayload);

        var mockContext = new Mock<ConsumeContext<IDataExtracted>>();
        mockContext.Setup(x => x.Message).Returns(mockMessage.Object);

        // 2. ACT
        await consumer.Consume(mockContext.Object);

        // 3. ASSERT (Yeni tablo yapısına göre kontrol)
        // AuditRecords tablosuna bir kayıt düşmüş mü?
        var record = await dbContext.AuditRecords.FirstOrDefaultAsync();

        record.Should().NotBeNull("Ana Audit kaydı oluşmalıydı.");
        record!.DocumentId.Should().Be(belgeId);

        // FieldHistories tablosuna 'Amount' alanı eklenmiş mi?
        var amountHistory = await dbContext.FieldHistories
                                           .FirstOrDefaultAsync(x => x.FieldName == "Amount");

        amountHistory.Should().NotBeNull("Tutar bilgisi tarihçeye kaydedilmeliydi.");
        amountHistory!.NewValue.Should().Be("15000");
    }
}