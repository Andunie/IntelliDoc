from fastapi import FastAPI, File, UploadFile
import google.generativeai as genai
from PIL import Image
from pdf2image import convert_from_bytes
import io
import json
import os

# API Key'inizi buraya yapıştırın
GEMINI_API_KEY = "AIzaSyAzfURFrYmhXpxDsOAyytVexTHeWUQe1-o"

genai.configure(api_key=GEMINI_API_KEY)

# Gemini 2.5 Flash modelini seçiyoruz (Hızlı ve Bedava)
model = genai.GenerativeModel('gemini-2.5-flash')

app = FastAPI()

@app.get("/")
def read_root():
    return {"System": "IntelliDoc AI Service", "Status": "Online (Gemini Flash)"}

@app.post("/extract")
async def extract_text(file: UploadFile = File(...)):
    try:
        contents = await file.read()
        image_parts = []

        # 1. Dosyayı Resme Çevir (Gemini resim ister)
        if file.content_type == "application/pdf":
            # PDF'in sadece ilk sayfasını alalım (Şimdilik)
            # Fatura genelde tek sayfadır veya ilk sayfada özet vardır.
            images = convert_from_bytes(contents)
            if len(images) > 0:
                image_parts.append(images[0]) 
        elif file.content_type.startswith("image/"):
            image = Image.open(io.BytesIO(contents))
            image_parts.append(image)
        else:
            return {"error": "Desteklenmeyen format."}

        # 2. Gemini'ye Emir Ver (Prompt)
        prompt = """
        Sen uzman bir finansal ve idari doküman analiz yapay zekasısın. 
        Görevin: Verilen belge görüntüsünü analiz etmek ve yapılandırılmış veri çıkarmaktır.

        KURALLAR:
        1. Belgenin TÜRÜNÜ (DocumentType) kesinlikle belirle (Örn: Fatura, Maaş Bordrosu, Dekont, Sözleşme, CV, Kimlik).
        2. Tarihleri kesinlikle "YYYY-MM-DD" formatına çevir.
        3. Parasal tutarları "1250.50" gibi sayısal formata çevir (Para birimi sembolünü at).
        4. Tablolu verileri (kalemleri) "LineItems" içine ekle.
        5. Bulamadığın veriler için null değerini kullan, asla "Bilinmiyor" veya "Yok" yazma.
        6. Cevabın SADECE geçerli bir JSON objesi olsun. Markdown (```json) kullanma.

        ---
        ÖRNEK SENARYO (Referans alman için):
        Girdi: Turkcell'den Ahmet Yılmaz'a gelmiş, 15 Ocak 2024 tarihli, GIB2024001 numaralı, toplam 500.50 TL tutarlı fatura. İçinde "Paket Ücreti: 400", "KDV: 100.50" yazıyor.
        
        İstenen Çıktı:
        {
            "DocumentType": "Fatura",
            "Summary": "Turkcell İletişim A.Ş. tarafından Ahmet Yılmaz adına düzenlenmiş telekomünikasyon faturası.",
            "Entities": {
                "Date": "2024-01-15",
                "Amount": 500.50,
                "Sender": "Turkcell İletişim A.Ş.",
                "Receiver": "Ahmet Yılmaz",
                "InvoiceNumber": "GIB2024001"
            },
            "LineItems": [
                {"Description": "Paket Ücreti", "Value": "400.00"},
                {"Description": "KDV", "Value": "100.50"}
            ]
        }
        ---

        Şimdi yukarıdaki kurallara ve örneğe uygun olarak, sana gönderdiğim belgeyi analiz et.
        """

        # 3. İsteği Gönder (Resim + Prompt)
        response = model.generate_content([prompt, image_parts[0]])
        
        # 4. Cevabı Temizle ve JSON Yap
        raw_text = response.text
        # Bazen ```json ile başlar, temizleyelim
        cleaned_text = raw_text.replace("```json", "").replace("```", "").strip()
        
        structured_data = json.loads(cleaned_text)

        return {
            "filename": file.filename,
            "text": raw_text, # Gemini'nin tüm cevabı
            "extracted_data": structured_data,
            "confidence": 100.0,
            "error": None
        }

    except Exception as e:
        return {"error": str(e)}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)