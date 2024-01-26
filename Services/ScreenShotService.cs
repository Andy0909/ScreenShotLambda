using Amazon.Lambda.Core;
using ScreenShotLambda.Interfaces;
using HeadlessChromium.Puppeteer.Lambda.Dotnet;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace ScreenShotLambda.Services
{
    class ScreenShotService : IScreenShot
    {
        // ���� 404
        private readonly int notFoundCode = 404;

        // ���`�q�� service
        private readonly IErrorNotify errorNotifyService;

        // �غc�l
        public ScreenShotService(IErrorNotify errorNotifyService)
        {
            this.errorNotifyService = errorNotifyService;
        }

        /// <summary>
        /// �i�����ַ�
        /// </summary>
        /// <param name="screenShotUrl"></param>
        /// <param name="context"></param>
        public async Task screenShot(string screenShotUrl, ILambdaContext context)
        {
           try
           {
                var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

                var browserLauncher = new HeadlessChromiumPuppeteerLauncher(loggerFactory);

                await using (var browser = await browserLauncher.LaunchAsync())

                await using (var page = await browser.NewPageAsync())
                {
                    var response = await page.GoToAsync(screenShotUrl);

                    if ((int)response.Status == this.notFoundCode)
                    {
                        await this.errorNotifyService.sendErrorMessage($"�������~�A�L�k�i��ַӡC", context);
                    }
                    else
                    {
                        // �]�w�ַӵe���e��
                        await page.SetViewportAsync(new ViewPortOptions
                        {
                            Width = 1280
                        });

                        // �N fullPage �ﶵ�]�m�� true�A�H������ӭ������I��
                        var screenshotOptions = new ScreenshotOptions
                        {
                            FullPage = true
                        };

                        // �N�ַӼȦs�ɩ�b tmp ��Ƨ����U
                        await page.ScreenshotAsync($"/tmp/screenShot.jpg", screenshotOptions);

                        context.Logger.LogInformation($"����ַӦ��\�C");
                    }
                }
           }
           catch (Exception e)
           {
                await this.errorNotifyService.sendErrorMessage($"����ַӥ��� : {e.Message}�C", context);
           }
        }
    }
}