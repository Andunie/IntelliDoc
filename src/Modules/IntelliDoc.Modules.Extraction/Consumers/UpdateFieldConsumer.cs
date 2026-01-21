using IntelliDoc.Modules.Extraction.Data;
using IntelliDoc.Modules.Extraction.Services;
using IntelliDoc.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IntelliDoc.Modules.Extraction.Consumers;

public class UpdateFieldConsumer : IConsumer<IFieldUpdated>
{
    private readonly ExtractionDbContext _dbContext;

    public UpdateFieldConsumer(ExtractionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<IFieldUpdated> context)
    {
        var msg = context.Message;
        var document = await _dbContext.ExtractionResults.FirstOrDefaultAsync(x => x.DocumentId == msg.DocumentId);
        if (document == null) return;

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var aiResponse = JsonSerializer.Deserialize<AiResponse>(document.JsonData, options);

            // --- DEĞİŞİKLİK BAŞLANGICI ---

            // Hedefimiz: extracted_data nesnesini doldurmak
            if (aiResponse != null)
            {
                // 1. Durum: extracted_data zaten doluysa sorun yok.

                // 2. Durum: extracted_data boş ama text doluysa -> text'i parse et
                if (aiResponse.extracted_data == null && !string.IsNullOrEmpty(aiResponse.text))
                {
                    string cleanJson = aiResponse.text.Replace("```json", "").Replace("```", "").Trim();
                    aiResponse.extracted_data = JsonSerializer.Deserialize<ExtractedData>(cleanJson, options);
                }

                // Şimdi Güncelleme Yapabiliriz
                if (aiResponse.extracted_data?.Fields != null)
                {
                    string keyToUpdate = msg.FieldName;
                    if (keyToUpdate.Contains(".")) keyToUpdate = keyToUpdate.Split('.').Last();

                    // Anahtar var mı? (Case-insensitive bulmaya çalışalım)
                    var existingKey = aiResponse.extracted_data.Fields.Keys
                        .FirstOrDefault(k => k.Equals(keyToUpdate, StringComparison.OrdinalIgnoreCase));

                    if (existingKey != null)
                    {
                        // Değeri Güncelle
                        aiResponse.extracted_data.Fields[existingKey] = msg.NewValue;

                        // KRİTİK NOKTA: Güncellenen veriyi tekrar 'text' içine de yazmamız gerekebilir
                        // Çünkü bir sonraki okumada yine text'ten parse edebilir.
                        // En temizi: extracted_data'yı ana veri kaynağı yapıp text'i güncellemek.
                        aiResponse.text = JsonSerializer.Serialize(aiResponse.extracted_data, options);

                        // DB'ye Kaydet
                        document.JsonData = JsonSerializer.Serialize(aiResponse, options);
                        await _dbContext.SaveChangesAsync();

                        Console.WriteLine($"[Extraction] ✅ '{existingKey}' güncellendi: {msg.NewValue}");
                    }
                    else
                    {
                        Console.WriteLine($"[Extraction] ⚠️ '{keyToUpdate}' alanı bulunamadı.");
                    }
                }
            }
            // --- DEĞİŞİKLİK SONU ---
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Extraction] ❌ Hata: {ex.Message}");
        }
    }
}
