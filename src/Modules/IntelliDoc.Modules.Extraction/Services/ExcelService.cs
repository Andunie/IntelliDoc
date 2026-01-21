using ClosedXML.Excel;
using IntelliDoc.Modules.Extraction.Services;
using System.Text.Json;

namespace IntelliDoc.Modules.Extraction.Services;

public class ExcelService
{
    // 1. TEK DOSYA EXPORT
    public byte[] GenerateExcel(string jsonData)
    {
        JsonElement rootElement = ParseJsonToRoot(jsonData);

        if (rootElement.ValueKind == JsonValueKind.Undefined)
        {
            return GenerateErrorExcel("Veri bulunamadı veya JSON formatı hatalı.");
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Rapor");

        // Sayfayı Doldur
        FillWorksheetWithData(worksheet, rootElement);

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // 2. TOPLU EXPORT (BATCH)
    public byte[] GenerateBatchExcel(List<string> jsonDatas)
    {
        using var workbook = new XLWorkbook();
        int counter = 1;

        foreach (var json in jsonDatas)
        {
            JsonElement rootElement = ParseJsonToRoot(json);

            if (rootElement.ValueKind != JsonValueKind.Undefined)
            {
                // Sayfa Adı Oluştur
                string sheetName = $"Belge {counter}";
                if (rootElement.TryGetProperty("DocumentType", out var type))
                {
                    string safeType = type.ToString();
                    // Excel sayfa adı kısıtlamaları (max 31 karakter, özel karakter yok)
                    sheetName = $"{safeType.Substring(0, Math.Min(15, safeType.Length))} {counter}";
                }

                var worksheet = workbook.Worksheets.Add(sheetName);

                // Sayfayı Doldur (Aynı mantığı kullanıyoruz)
                FillWorksheetWithData(worksheet, rootElement);

                worksheet.Columns().AdjustToContents();
                counter++;
            }
        }

        if (workbook.Worksheets.Count == 0)
        {
            var ws = workbook.Worksheets.Add("Hata");
            ws.Cell(1, 1).Value = "Hiçbir belge dışa aktarılamadı.";
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // --- ORTAK MANTIK (CORE LOGIC) ---

    private void FillWorksheetWithData(IXLWorksheet worksheet, JsonElement root)
    {
        int row = 1;

        // A. Temel Bilgiler (DocumentType, Summary)
        if (root.TryGetProperty("DocumentType", out var docType))
        {
            worksheet.Cell(row, 1).Value = "Belge Türü";
            worksheet.Cell(row, 2).Value = docType.ToString();
            worksheet.Range(row, 1, row, 2).Style.Font.Bold = true;
            worksheet.Range(row, 1, row, 2).Style.Fill.BackgroundColor = XLColor.LightCyan;
            row++;
        }
        if (root.TryGetProperty("Summary", out var summary))
        {
            worksheet.Cell(row, 1).Value = "Özet";
            worksheet.Cell(row, 2).Value = summary.ToString();
            worksheet.Cell(row, 2).Style.Alignment.WrapText = true;
            row += 2; // Boşluk
        }

        // B. "Entities" veya "Fields" İçini Gez (Recursive)
        JsonElement dataNode = default;
        if (root.TryGetProperty("Fields", out var f) && f.ValueKind != JsonValueKind.Null) dataNode = f;
        else if (root.TryGetProperty("Entities", out var e) && e.ValueKind != JsonValueKind.Null) dataNode = e;

        if (dataNode.ValueKind == JsonValueKind.Object)
        {
            worksheet.Cell(row, 1).Value = "DETAYLI BİLGİLER";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            worksheet.Cell(row, 1).Style.Font.FontSize = 14;
            row++;

            row = ParseJsonObjectToExcel(worksheet, dataNode, row, 0);
        }

        // C. Kök Dizindeki Diğer Diziler (Fallback)
        // (WorkExperiences gibi kök dizinde olan ama Tables içinde OLMAYAN diziler için)
        foreach (var property in root.EnumerateObject())
        {
            // KRİTİK DÜZELTME BURADA: "Tables" dizisini burada işleme!
            if (property.Value.ValueKind == JsonValueKind.Array &&
                property.Name != "Fields" &&
                property.Name != "Entities" &&
                property.Name != "Tables") // <--- BU SATIR EKLENDİ
            {
                row += 2;
                row = CreateTableFromList(worksheet, property.Name, property.Value, row);
            }
        }

        // D. Dinamik "Tables" Dizisi (Asıl Tablolar)
        if (root.TryGetProperty("Tables", out var tablesNode) && tablesNode.ValueKind == JsonValueKind.Array)
        {
            foreach (var table in tablesNode.EnumerateArray())
            {
                string tableName = "Tablo";
                if (table.TryGetProperty("Name", out var n)) tableName = n.GetString() ?? "Tablo";

                if (table.TryGetProperty("Rows", out var rows))
                {
                    row += 2;
                    row = CreateTableFromList(worksheet, tableName, rows, row);
                }
            }
        }
    }

    // --- YARDIMCI METOTLAR ---

    private JsonElement ParseJsonToRoot(string jsonData)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var aiResponse = JsonSerializer.Deserialize<AiResponse>(jsonData, options);

            string jsonToParse = "";
            if (aiResponse?.extracted_data != null)
                jsonToParse = JsonSerializer.Serialize(aiResponse.extracted_data);
            else if (!string.IsNullOrEmpty(aiResponse?.text))
                jsonToParse = aiResponse.text.Replace("```json", "", StringComparison.OrdinalIgnoreCase).Replace("```", "").Trim();

            if (!string.IsNullOrEmpty(jsonToParse))
            {
                using var doc = JsonDocument.Parse(jsonToParse);
                return doc.RootElement.Clone();
            }
        }
        catch { }
        return default;
    }

    private int ParseJsonObjectToExcel(IXLWorksheet ws, JsonElement element, int startRow, int indentLevel)
    {
        int currentRow = startRow;

        foreach (var property in element.EnumerateObject())
        {
            // "Tables" dizisini burada da engelle (Recursive içinde çıkarsa diye)
            if (property.Name == "Tables") continue;

            ws.Cell(currentRow, 1).Style.Alignment.Indent = indentLevel;

            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                ws.Cell(currentRow, 1).Value = property.Name;
                ws.Cell(currentRow, 1).Style.Font.Bold = true;
                ws.Cell(currentRow, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                currentRow++;
                currentRow = ParseJsonObjectToExcel(ws, property.Value, currentRow, indentLevel + 1);
            }
            else if (property.Value.ValueKind == JsonValueKind.Array)
            {
                // Dizi ise tablo yap
                currentRow += 1;
                currentRow = CreateTableFromList(ws, property.Name, property.Value, currentRow);
                currentRow += 1;
            }
            else
            {
                ws.Cell(currentRow, 1).Value = property.Name;
                ws.Cell(currentRow, 2).Value = property.Value.ToString();
                currentRow++;
            }
        }
        return currentRow;
    }

    private int CreateTableFromList(IXLWorksheet ws, string tableName, JsonElement arrayElement, int startRow)
    {
        if (arrayElement.GetArrayLength() == 0) return startRow;

        // Tablo Başlığı
        ws.Cell(startRow, 1).Value = tableName;
        ws.Cell(startRow, 1).Style.Font.Bold = true;
        ws.Cell(startRow, 1).Style.Font.FontSize = 12;
        ws.Cell(startRow, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(startRow, 1).Style.Fill.BackgroundColor = XLColor.DarkBlue;
        startRow++;

        var firstItem = arrayElement[0];

        // Sütunları Belirle
        var headers = new List<string>();
        if (firstItem.ValueKind == JsonValueKind.String)
        {
            headers.Add("Değer"); // Basit string listesi için kolon adı
        }
        else if (firstItem.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in firstItem.EnumerateObject()) headers.Add(prop.Name);
        }

        // Başlıkları Yaz
        for (int i = 0; i < headers.Count; i++)
        {
            ws.Cell(startRow, i + 1).Value = headers[i];
            ws.Cell(startRow, i + 1).Style.Font.Bold = true;
            ws.Cell(startRow, i + 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }
        startRow++;

        // Verileri Yaz
        foreach (var item in arrayElement.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                ws.Cell(startRow, 1).Value = item.ToString();
            }
            else
            {
                for (int i = 0; i < headers.Count; i++)
                {
                    if (item.TryGetProperty(headers[i], out var val))
                    {
                        ws.Cell(startRow, i + 1).Value = val.ToString();
                    }
                }
            }
            startRow++;
        }

        return startRow;
    }

    private byte[] GenerateErrorExcel(string msg)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Error");
        ws.Cell(1, 1).Value = msg;
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}