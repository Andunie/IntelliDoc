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
        Sen dünyanın en gelişmiş ve esnek doküman analiz yapay zekasısın.
        Görevin: Verilen belge görüntüsünü analiz etmek ve belgenin türüne özgü kritik verileri dinamik bir yapıda çıkarmaktır.

        KURALLAR:
        1. "DocumentType": Belgenin türünü sen tespit et (Fatura, CV, Maaş Bordrosu, Tapu, Sözleşme, Kimlik vb.).
        2. "Summary": Belgenin içeriğini anlatan profesyonel, kısa bir özet yaz.
        3. "Fields": Belgedeki önemli verileri "Anahtar": "Değer" çiftleri olarak çıkar.
           - Anahtar (Key) isimlerini İngilizce ve PascalCase kullan (Örn: "TotalAmount", "EmployeeName", "SkillSet", "ParcelNumber").
           - Sabit bir şablonun yok. Belgede ne görüyorsan, o belge türü için ne önemliyse onu al.
           - Tarihleri her zaman "YYYY-MM-DD" formatına çevir.
           - Parasal değerleri sayısal (float) formatta ver (Para birimi sembolünü at).
        4. "Tables": Belgede tablo varsa (Fatura kalemleri, Bordro dökümü vb.), bunları satır satır çıkar.

        ÇIKTI FORMATI (SADECE JSON):
        Cevabın sadece aşağıdaki yapıda bir JSON olmalıdır. Yorum satırı veya Markdown ekleme.

        ---
        ÖRNEK 1 (Fatura Gelirse):
        {
            "DocumentType": "Fatura",
            "Summary": "Turkcell İletişim A.Ş. faturası.",
            "Fields": {
                "InvoiceDate": "2024-01-15",
                "TotalAmount": 500.50,
                "VendorName": "Turkcell",
                "InvoiceNumber": "GIB2024001"
            },
            "Tables": [
                {
                    "Name": "Fatura Kalemleri",
                    "Rows": [ {"Description": "Paket", "Price": 400}, {"Description": "KDV", "Price": 100.50} ]
                }
            ]
        }

        ÖRNEK 2 (CV Gelirse):
        {
            "DocumentType": "CV",
            "Summary": "Yazılım Uzmanı Ahmet Yılmaz'ın özgeçmişi.",
            "Fields": {
                "CandidateName": "Ahmet Yılmaz",
                "Email": "ahmet@mail.com",
                "Phone": "+905551234567",
                "Skills": ["C#", "Python", "Docker"],
                "ExperienceYears": 5
            },
            "Tables": []
        }
        ---

        Şimdi sana gönderdiğim belgeyi bu esnek yapıya göre analiz et.
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