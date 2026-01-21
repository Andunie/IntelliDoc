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
        Sen uzman bir doküman analiz yapay zekasısın. Görevin, belge türünü tespit etmek ve yapılandırılmış veri çıkarmaktır.

        ADIM 1: Belge Türünü Belirle (Fatura, CV, Bordro, Sözleşme, Dekont vb.)

        ADIM 2: Türüne göre aşağıdaki alanları çıkar:

        A) FİNANSAL (Fatura, Fiş, Dekont):
           - Entities: Date (YYYY-MM-DD), Amount (Sayı), Currency, InvoiceNumber, Sender, Receiver, SenderTaxID, ReceiverTaxID, ETTN.
           - Tables: "LineItems" (Description, Quantity, UnitPrice, Total).

        B) İNSAN KAYNAKLARI (CV / Özgeçmiş):
           - Entities: Name, JobTitle, Email, Phone, Location, BirthDate, Gender.
           - Tables: 
             - "WorkExperience" (Company, Role, StartDate, EndDate, Description).
             - "Education" (School, Degree, FieldOfStudy, StartDate, EndDate).
             - "Skills" (SkillName). **(ÖNEMLİ: Yetenekleri virgülle ayırma, her birini ayrı satır olarak Skills tablosuna ekle)**
             - "Languages" (Language, Level).

        C) MAAŞ BORDROSU (Payslip):
           - Entities: EmployeeName, EmployeeTCKN, Period, NetSalary, GrossSalary, CompanyName.
           - Tables: "Earnings" (Type, Amount), "Deductions" (Type, Amount).

        D) DİĞER (Sözleşme, Tapu vb.):
           - Entities: DocumentDate, Parties (Taraflar), ReferenceNumber.
           - Summary: Belgenin özeti.

        ÇIKTI FORMATI (JSON):
        {
            "DocumentType": "...",
            "Summary": "...",
            "Entities": {
                // Buraya sadece "Anahtar": "Değer" çiftleri gelecek. İç içe obje veya liste YOK.
                "Name": "Ahmet Yılmaz",
                "Amount": 100.50
            },
            "Tables": [
                {
                    "Name": "WorkExperience", // veya "LineItems"
                    "Rows": [
                        {"Company": "ABC", "Role": "Dev"}
                    ]
                }
            ]
        }

        Use pure JSON objects/arrays for nested data, do NOT use stringified JSON.
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