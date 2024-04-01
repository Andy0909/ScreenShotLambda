using Amazon.S3.Model;
using Amazon.S3;
using ScreenShotLambda.Interfaces;
using Amazon.Lambda.Core;
using System.Net;

namespace ScreenShotLambda.Services
{
    /// <summary>
    /// 圖片上傳S3服務
    /// </summary>
    class ImageUploadService : IImageUpload
    {
        /// <summary>
        /// AWS S3 物件
        /// </summary>
        private readonly IAmazonS3 s3Client;

        /// <summary>
        /// 異常通知物件
        /// </summary>
        private readonly IErrorNotify errorNotifyService;

        /// <summary>
        /// Lambda Log 物件
        /// </summary>
        private readonly ILambdaLogger logger;

        /// <summary>
        /// 建構子
        /// <param name="s3Client">s3 client</param>
        /// <param name="errorNotifyService">異常通知服務</param>
        /// <param name="logger">log 紀錄</param>
        /// </summary>
        public ImageUploadService(IAmazonS3 s3Client, IErrorNotify errorNotifyService, ILambdaLogger logger)
        {
            this.s3Client = s3Client;
            this.errorNotifyService = errorNotifyService;
            this.logger = logger;
        }

        /// <summary>
        /// 上傳快照圖片至 S3
        /// </summary>
        public async Task UploadImageToS3()
        {
            try
            {
                // 設定 S3 所需參數
                PutObjectRequest request = new PutObjectRequest
                {
                    BucketName = Environment.GetEnvironmentVariable("S3_BUCKET_NAME") ?? "",
                    Key = $"screenShot.jpg",
                    ContentType = "image/jpeg",
                    FilePath = $"/tmp/screenShot.jpg",
                    CannedACL = S3CannedACL.PublicRead
                };

                // 上傳 S3
                PutObjectResponse response = await this.s3Client.PutObjectAsync(request);

                // 檢查 response
                if (response.HttpStatusCode != HttpStatusCode.OK)
                {
                    await this.errorNotifyService.SendErrorMessage($"ImageUploadService 呼叫 s3Client 失敗。HTTP 狀態碼: {response.HttpStatusCode}");
                }

                context.Logger.LogInformation($"圖片上傳 S3 成功");
            }
            catch (AmazonS3Exception e)
            {
                await this.errorNotifyService.SendErrorMessage($"圖片上傳 S3 失敗，S3 異常 : {e.Message}");
            }
            catch (Exception e)
            {
                await this.errorNotifyService.SendErrorMessage($"圖片上傳 S3 失敗 : {e.Message}");
            }
        }
    }
}
