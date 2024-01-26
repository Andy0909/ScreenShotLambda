using Amazon.S3.Model;
using Amazon.S3;
using ScreenShotLambda.Interfaces;
using Amazon.Lambda.Core;

namespace ScreenShotLambda.Services
{
    class ImageUploadService : IImageUpload
    {
        // S3 Client
        private readonly IAmazonS3 s3Client;

        // 異常通知 service
        private readonly IErrorNotify errorNotifyService;

        // 建構子
        public ImageUploadService(IAmazonS3 s3Client, IErrorNotify errorNotifyService)
        {
            this.s3Client = s3Client;

            this.errorNotifyService = errorNotifyService;
        }

        /// <summary>
        /// 上傳快照圖片至 S3
        /// </summary>
        /// <param name="context"></param>
        public async Task uploadImageToS3(ILambdaContext context)
        {
            try
            {
                // 設定 S3 所需參數
                PutObjectRequest request = new PutObjectRequest
                {
                    BucketName = "your bucket name",
                    Key = $"screenShot.jpg",
                    ContentType = "image/jpg",
                    FilePath = $"/tmp/screenShot.jpg",
                    CannedACL = S3CannedACL.PublicRead
                };

                // 上傳 S3
                PutObjectResponse response = await this.s3Client.PutObjectAsync(request);

                context.Logger.LogInformation($"圖片上傳 S3 成功。");
            }
            catch (AmazonS3Exception e)
            {
                await this.errorNotifyService.sendErrorMessage($"圖片上傳 S3 失敗 : {e.Message}。", context);
            }
        }
    }
}
