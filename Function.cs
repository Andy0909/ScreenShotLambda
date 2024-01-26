using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.SQS;
using Amazon.S3;
using ScreenShotLambda.Interfaces;
using ScreenShotLambda.Services;
using ScreenShotLambda.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ScreenShotLambda
{
    public class Function
    {
        // 文字檔來源路徑
        private readonly string sourceDir;

        // 文字檔欲複製的目標路徑
        private readonly string destinationDir;

        // 相依注入服務集合物件
        private readonly ServiceCollection providerServices;

        // 建構子
        public Function()
        {
            this.sourceDir = "/var/task/.fonts";

            this.destinationDir = "/tmp/.fonts";

            this.providerServices = new ServiceCollection();

            ConfigureServices(this.providerServices);
        }

        /// <summary>
        /// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used 
        /// to respond to SQS messages.
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
        {

            foreach (var message in evnt.Records)
            {
                await ProcessMessageAsync(message, context);
            }
        }

        private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
        {
            try
            {
                // 解析 SQS 訊息中的訂單資訊
                var queueMessage = JsonConvert.DeserializeObject<QueueMessage>(message.Body);

                // 提取訂單編號和賣場編號
                var screenShotUrl = queueMessage.screenShotUrl;

                context.Logger.LogInformation($"SQS 訊息解析完成, screenShotUrl:{screenShotUrl}");

                // 複製字型檔案到 tmp 資料夾
                copyFontsToTmp();

                context.Logger.LogInformation($"複製字型檔案完成");

                // 建立依賴注入服務提供者
                var serviceProvider = this.providerServices.BuildServiceProvider();

                // 取得 screenShotService
                var screenShotService = serviceProvider.GetService<IScreenShot>();

                // 進行快照
                await screenShotService.screenShot(screenShotUrl, context);

                // 取得 imageUploadService
                var imageUploadService = serviceProvider.GetService<IImageUpload>();

                // 上傳快照圖片至 S3
                await imageUploadService.uploadImageToS3(context);
            }
            catch (JsonException e)
            {
                // 建立依賴注入服務提供者
                var serviceProvider = this.providerServices.BuildServiceProvider();

                // 取得 errorNotifyService
                var errorNotifyService = serviceProvider.GetService<IErrorNotify>();

                await errorNotifyService.sendErrorMessage($"QueueMessage JsonException: {e.Message}", context);
            }
            catch (Exception e)
            {
                // 建立依賴注入服務提供者
                var serviceProvider = this.providerServices.BuildServiceProvider();

                // 取得 errorNotifyService
                var errorNotifyService = serviceProvider.GetService<IErrorNotify>();

                await errorNotifyService.sendErrorMessage($"快照主程式錯誤: {e.Message}，將進行重試", context);

                // 取得 errorRetryService
                var errorRetryService = serviceProvider.GetService<IErrorRetry>();

                // 重發 queue 進行重試
                await errorRetryService.retryErrorQueue(message, context);
            }
        }

        /// <summary>
        /// 將文字檔複製到 /tmp/fonts
        /// </summary>
        private void copyFontsToTmp()
        {
            // 確保目標資料夾存在
            if (!Directory.Exists(this.destinationDir))
            {
                Directory.CreateDirectory(this.destinationDir);
            }

            // 複製文字檔
            File.Copy(this.sourceDir + "/font.ttc", this.destinationDir + "/font.ttc", true);
        }

        /// <summary>
        /// Dependency Injection 設定
        /// </summary>
        /// <param name="serviceCollection">Service Collection</param>
        private void ConfigureServices(IServiceCollection serviceCollection)
        {
            // 建立依賴注入
            serviceCollection.AddScoped<IImageUpload, ImageUploadService>();
            serviceCollection.AddScoped<IScreenShot, ScreenShotService>();
            serviceCollection.AddScoped<IErrorNotify, ErrorNotifyService>();
            serviceCollection.AddScoped<IErrorRetry, ErrorRetryService>();
            serviceCollection.AddScoped<TimeService>();
            serviceCollection.AddScoped<IAmazonS3, AmazonS3Client>();
            serviceCollection.AddScoped<IAmazonSQS, AmazonSQSClient>();
        }
    }
}