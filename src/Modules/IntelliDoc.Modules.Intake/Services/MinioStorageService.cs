using Minio;
using Minio.DataModel.Args;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace IntelliDoc.Modules.Intake.Services;

public class MinioStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName = "documents";

    public MinioStorageService(IConfiguration config)
    {
        // appsettings'den ayarları alıp MinIO Client oluşturuyoruz
        _minioClient = new MinioClient()
            .WithEndpoint("localhost", 9000) // Docker portu
            .WithCredentials("minioadmin", "minioadmin") // Docker user/pass
            .Build();
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        // 1. Bucket var mı kontrol et, yoksa oluştur
        bool found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName));
        if (!found)
        {
            await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
        }

        // 2. Benzersiz bir dosya ismi oluştur (Guid)
        var objectName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";

        // 3. Dosyayı yükle
        var putObjectArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType);

        await _minioClient.PutObjectAsync(putObjectArgs);

        return objectName; // MinIO'daki dosya adını dön
    }

    public async Task<string> GetPresignedUrlAsync(string objectName)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithExpiry(3600); // Link 1 saat (3600 sn) boyunca geçerli olsun

        return await _minioClient.PresignedGetObjectAsync(args);
    }
}