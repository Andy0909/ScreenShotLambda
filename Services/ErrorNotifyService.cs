using Amazon.Lambda.Core;
using ScreenShotLambda.Interfaces;
using System.Text;
using System.Text.Json;

namespace ScreenShotLambda.Services
{
    class ErrorNotifyService : IErrorNotify
    {
        // HTTP client
        private readonly HttpClient httpClient;

        // 設定 JSON 序列化
        private readonly JsonSerializerOptions options;

        // 取得台灣時間 service
        private readonly TimeService timeService;

        // 建構子
        public ErrorNotifyService(TimeService timeService)
        {
            this.httpClient = new HttpClient();

            this.options = new JsonSerializerOptions();

            this.timeService = timeService;
        }

        /// <summary>
        /// 異常通知
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        public async Task sendErrorMessage(string message, ILambdaContext context)
        {
            try
            {
                var errorMessage = message + $"，時間：{timeService.getCurrentTaipeiTime()}";

                // 紀錄 log
                context.Logger.LogInformation(errorMessage);

                // 要傳送的 JSON 資料 
                var requestData = new { text = errorMessage };
                var jsonRequestData = JsonSerializer.Serialize(requestData, this.options);

                // 使用 StringContent 將 JSON 資料包裝成 HTTP 內容
                var content = new StringContent(jsonRequestData, Encoding.UTF8, "application/json");

                // 發送 POST 請求
                var response = await this.httpClient.PostAsync("https://senaoaz.webhook.office.com/webhookb2/e2981449-f205-4ddd-a118-32600d838400@7fbd8d8d-fe50-4c00-9d17-e6997c474f9b/IncomingWebhook/d1f3d9e9a4bd44ee8fda9593d37045ff/1d05bc54-6c6b-4336-ad77-3d908535b310", content);
            }
            catch (Exception e)
            {
                context.Logger.LogInformation($"呼叫 TeamsWebhookUrl 發生錯誤：{e.Message}");
            }
        }
    }
}
