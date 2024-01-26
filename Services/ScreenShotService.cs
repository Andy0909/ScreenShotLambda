using Amazon.Lambda.Core;
using ScreenShotLambda.Interfaces;
using HeadlessChromium.Puppeteer.Lambda.Dotnet;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace ScreenShotLambda.Services
{
    class ScreenShotService : IScreenShot
    {
        // 頁面 404
        private readonly int notFoundCode = 404;

        // 異常通知 service
        private readonly IErrorNotify errorNotifyService;

        // 建構子
        public ScreenShotService(IErrorNotify errorNotifyService)
        {
            this.errorNotifyService = errorNotifyService;
        }

        /// <summary>
        /// 進行賣場快照
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
                        await this.errorNotifyService.sendErrorMessage($"頁面錯誤，無法進行快照。", context);
                    }
                    else
                    {
                        // 設定快照畫面寬度
                        await page.SetViewportAsync(new ViewPortOptions
                        {
                            Width = 1280
                        });

                        // 將 fullPage 選項設置為 true，以捕捉整個頁面的截圖
                        var screenshotOptions = new ScreenshotOptions
                        {
                            FullPage = true
                        };

                        // 將快照暫存檔放在 tmp 資料夾底下
                        await page.ScreenshotAsync($"/tmp/screenShot.jpg", screenshotOptions);

                        context.Logger.LogInformation($"執行快照成功。");
                    }
                }
           }
           catch (Exception e)
           {
                await this.errorNotifyService.sendErrorMessage($"執行快照失敗 : {e.Message}。", context);
           }
        }
    }
}