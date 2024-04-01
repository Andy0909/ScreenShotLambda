using Amazon.Lambda.Core;
using ScreenShotLambda.Interfaces;
using HeadlessChromium.Puppeteer.Lambda.Dotnet;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace ScreenShotLambda.Services
{
    /// <summary>
    /// 快照服務
    /// </summary>
    class ScreenShotService : IScreenShot
    {
        /// <summary>
        /// HTTP 狀態碼 404
        /// </summary>
        private readonly int notFoundCode = 404;

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
        /// <param name="errorNotifyService">異常通知服務</param>
        /// <param name="logger">log 紀錄</param>
        /// </summary>
        public ScreenShotService(IErrorNotify errorNotifyService, ILambdaLogger logger)
        {
            this.errorNotifyService = errorNotifyService;
            this.logger = logger;
        }

        /// <summary>
        /// 進行賣場快照
        /// </summary>
        /// <param name="screenShotUrl"></param>
        public async Task ScreenShot(string screenShotUrl)
        {
           try
           {
                // HeadlessChromium.Puppeteer.Lambda.Dotnet 官網提供的 code
                var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                var browserLauncher = new HeadlessChromiumPuppeteerLauncher(loggerFactory);
                var browser = await browserLauncher.LaunchAsync();
                var page = await browser.NewPageAsync();
                var response = await page.GoToAsync(screenShotUrl);

                // 若 status 為 404 則發異常通知
                if ((int)response.Status == this.notFoundCode)
                {
                    await this.errorNotifyService.SendErrorMessage($"找不到欲執行快照之頁面");
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
                
                    this.logger.LogInformation($"執行快照成功");
                }
           }
           catch (Exception e)
           {
                await this.errorNotifyService.SendErrorMessage($"執行快照失敗 : {e.Message}");
           }
        }
    }
}
