using Amazon.Lambda.Core;
using ScreenShotLambda.Interfaces;
using System.Text;
using System.Text.Json;

namespace ScreenShotLambda.Services
{
    /// <summary>
    /// 異常通知服務
    /// </summary>
    class ErrorNotifyService : IErrorNotify
    {
        /// <summary>
        /// HTTP 物件
        /// </summary>
        private readonly IHttpClientFactory httpClientFactory;

        /// <summary>
        /// TimeService 物件
        /// </summary>
        private readonly TimeService timeService;

        /// <summary>
        /// Lambda Log 物件
        /// </summary>
        private readonly ILambdaLogger logger;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="httpClientFactory">HttpClientFactory</param>
        /// <param name="timeService">取得時間服務</param>
        /// <param name="logger">log 紀錄</param>
        public ErrorNotifyService(IHttpClientFactory httpClientFactory, TimeService timeService, ILambdaLogger logger)
        {
            this.httpClientFactory = httpClientFactory;
            this.timeService = timeService;
            this.logger = logger;
        }

        /// <summary>
        /// 異常通知
        /// </summary>
        /// <param name="message"></param>
        public async Task SendErrorMessage(string message)
        {
            try
            {
                var errorMessage = message + $"，時間：{timeService.GetCurrentTaipeiTime()}";

                // 紀錄 log
                this.logger.LogInformation(errorMessage);

                // 要傳送的 JSON 資料 
                var requestData = new { text = errorMessage };
                var jsonRequestData = JsonSerializer.Serialize(requestData);

                // 建立 HttpClient
                HttpClient httpClient = this.httpClientFactory.CreateClient();
                
                // 使用 StringContent 將 JSON 資料包裝成 HTTP 內容
                HttpContent content = new StringContent(jsonRequestData, Encoding.UTF8, "application/json");
                
                // 發送 POST 請求
                string teamsWebhookUrl = Environment.GetEnvironmentVariable("TEAMS_WEBHOOK_URL") ?? "";
                HttpResponseMessage response = await httpClient.PostAsync(teamsWebhookUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    this.logger.LogError($"呼叫 TeamsWebhookUrl 失敗。HTTP 狀態碼：{response.StatusCode}");
                }
            }
            catch (HttpRequestException e)
            {
                this.logger.LogError($"呼叫 TeamsWebhookUrl 發生網路錯誤：{e.Message}");
            }
            catch (Exception e)
            {
                this.logger.LogError($"呼叫 TeamsWebhookUrl 發生錯誤：{e.Message}");
            }
        }
    }
}
