using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.SQS;
using Amazon.S3;
using EcScreenShot.Interfaces;
using EcScreenShot.Services;
using Microsoft.Extensions.DependencyInjection;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ScreenShotLambda
{
    /// <summary>
    /// lambda 執行入口
    /// </summary>
    public class Function
    {
        /// <summary>
        /// 文字檔來源路徑
        /// </summary>
        private readonly string sourceDir = "/var/task/.fonts";

        /// <summary>
        /// 文字檔欲複製的目標路徑
        /// </summary>
        private readonly string destinationDir = "/tmp/.fonts";

        /// <summary>
        /// 相依注入服務集合物件
        /// </summary>
        private readonly ServiceCollection providerServices;

        /// <summary>
        /// 建構子
        /// </summary>
        public Function()
        {
            this.providerServices = new ServiceCollection();
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

            try
            {
                // 建立依賴注入
                ServiceProvider serviceProvider = this.BuildDependencyInjectionServiceProvider(context.Logger);
            
                // 複製字型檔案到 tmp 資料夾
                CopyFontsToTmp();
            
                context.Logger.LogInformation($"複製字型檔案完成");
            
                // 取得 ScreenShotController
                ScreenShotController? screenShotController = serviceProvider.GetService<ScreenShotController>();
            
                if (screenShotController != null)
                {
                    foreach (var message in evnt.Records)
                    {
                        // 執行快照
                        await screenShotController.HandleScreenShot(message);
                    }
                }
                else
                {
                    context.Logger.LogError($"screenShotController 為 null，無法執行訂單快照主程式");
                }
            }
            catch (Exception e)
            {
                context.Logger.LogError($"FunctionHandler 發生錯誤: {e.Message}");
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
        /// 建立相依注入物件
        /// </summary>
        /// <param name="logger">Lambda Log 物件</param>
        /// <returns> 相依注入服務集合建置物件 </returns>
        private ServiceProvider BuildDependencyInjectionServiceProvider(ILambdaLogger logger)
        {
            this.SetDependencyInjectionServiceProvider(logger);
        
            return this.providerServices.BuildServiceProvider();
        }
        
        /// <summary>
        /// 設定相依注入物件
        /// </summary>
        /// <param name="logger">Lambda Log 物件</param>
        private void SetDependencyInjectionServiceProvider(ILambdaLogger logger)
        {
            // 建立依賴注入 - controller
            this.providerServices.AddScoped<ScreenShotController>();
        
            // 建立依賴注入 - services
            this.providerServices.AddScoped<IImageUpload, ImageUploadService>();
            this.providerServices.AddScoped<IScreenShot, ScreenShotService>();
            this.providerServices.AddScoped<IErrorNotify, ErrorNotifyService>();
            this.providerServices.AddScoped<IEcApiCaller, EcApiCallerService>();
            this.providerServices.AddScoped<IErrorRetry, ErrorRetryService>();
            this.providerServices.AddScoped<IQueueDelete, QueueDeleteService>();
            this.providerServices.AddScoped<TimeService>();
            this.providerServices.AddHttpClient();
        
            // 建立依賴注入 - AWS
            this.providerServices.AddScoped<ILambdaLogger>(ServiceProvider => logger);
            this.providerServices.AddScoped<IAmazonS3, AmazonS3Client>();
            this.providerServices.AddScoped<IAmazonSQS, AmazonSQSClient>();
        }
    }
}
