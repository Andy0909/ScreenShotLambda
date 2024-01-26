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
        // ��r�ɨӷ����|
        private readonly string sourceDir;

        // ��r�ɱ��ƻs���ؼи��|
        private readonly string destinationDir;

        // �̪ۨ`�J�A�ȶ��X����
        private readonly ServiceCollection providerServices;

        // �غc�l
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
                // �ѪR SQS �T�������q���T
                var queueMessage = JsonConvert.DeserializeObject<QueueMessage>(message.Body);

                // �����q��s���M����s��
                var screenShotUrl = queueMessage.screenShotUrl;

                context.Logger.LogInformation($"SQS �T���ѪR����, screenShotUrl:{screenShotUrl}");

                // �ƻs�r���ɮר� tmp ��Ƨ�
                copyFontsToTmp();

                context.Logger.LogInformation($"�ƻs�r���ɮק���");

                // �إߨ̿�`�J�A�ȴ��Ѫ�
                var serviceProvider = this.providerServices.BuildServiceProvider();

                // ���o screenShotService
                var screenShotService = serviceProvider.GetService<IScreenShot>();

                // �i��ַ�
                await screenShotService.screenShot(screenShotUrl, context);

                // ���o imageUploadService
                var imageUploadService = serviceProvider.GetService<IImageUpload>();

                // �W�ǧַӹϤ��� S3
                await imageUploadService.uploadImageToS3(context);
            }
            catch (JsonException e)
            {
                // �إߨ̿�`�J�A�ȴ��Ѫ�
                var serviceProvider = this.providerServices.BuildServiceProvider();

                // ���o errorNotifyService
                var errorNotifyService = serviceProvider.GetService<IErrorNotify>();

                await errorNotifyService.sendErrorMessage($"QueueMessage JsonException: {e.Message}", context);
            }
            catch (Exception e)
            {
                // �إߨ̿�`�J�A�ȴ��Ѫ�
                var serviceProvider = this.providerServices.BuildServiceProvider();

                // ���o errorNotifyService
                var errorNotifyService = serviceProvider.GetService<IErrorNotify>();

                await errorNotifyService.sendErrorMessage($"�ַӥD�{�����~: {e.Message}�A�N�i�歫��", context);

                // ���o errorRetryService
                var errorRetryService = serviceProvider.GetService<IErrorRetry>();

                // ���o queue �i�歫��
                await errorRetryService.retryErrorQueue(message, context);
            }
        }

        /// <summary>
        /// �N��r�ɽƻs�� /tmp/fonts
        /// </summary>
        private void copyFontsToTmp()
        {
            // �T�O�ؼи�Ƨ��s�b
            if (!Directory.Exists(this.destinationDir))
            {
                Directory.CreateDirectory(this.destinationDir);
            }

            // �ƻs��r��
            File.Copy(this.sourceDir + "/font.ttc", this.destinationDir + "/font.ttc", true);
        }

        /// <summary>
        /// Dependency Injection �]�w
        /// </summary>
        /// <param name="serviceCollection">Service Collection</param>
        private void ConfigureServices(IServiceCollection serviceCollection)
        {
            // �إߨ̿�`�J
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