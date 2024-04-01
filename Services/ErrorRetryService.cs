using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.SQS;
using Amazon.SQS.Model;
using ScreenShotLambda.DTOs;
using ScreenShotLambda.Interfaces;
using Newtonsoft.Json;
using System.Text.Json;

namespace ScreenShotLambda.Services
{
    /// <summary>
    /// 異常重試服務
    /// </summary>
    class ErrorRetryService : IErrorRetry
    {
        /// <summary>
        /// AWS SQS 物件
        /// </summary>
        private readonly IAmazonSQS sqsClient;

        /// <summary>
        /// 異常通知物件
        /// </summary>
        private readonly IErrorNotify errorNotifyService;

        /// <summary>
        /// 刪除 queue 物件
        /// </summary>
        private readonly IQueueDelete queueDeleteService;
        
        /// <summary>
        /// Lambda Log 物件
        /// </summary>
        private readonly ILambdaLogger logger;

        /// <summary>
        /// 建構子
        /// <param name="sqsClient">sqs client</param>
        /// <param name="errorNotifyService">異常通知服務</param>
        /// <param name="queueDeleteService">刪除佇列服務</param>
        /// <param name="logger">log 紀錄</param>
        /// </summary>
        public ErrorRetryService(IAmazonSQS sqsClient, IErrorNotify errorNotifyService, IQueueDelete queueDeleteService, ILambdaLogger logger)
        {
            this.sqsClient = sqsClient;
            this.errorNotifyService = errorNotifyService;
            this.queueDeleteService = queueDeleteService;
            this.logger = logger;
        }
         
        /// <summary>
        /// 有錯誤的 queue 進行重試
        /// </summary>
        /// <param name="message"></param>
        public async Task RetryErrorQueue(SQSEvent.SQSMessage message)
        {
            try
            {
                var queueMessage = JsonSerializer.Deserialize<QueueMessage>(message.Body);
                var screenShotUrl = queueMessage?.screenShotUrl;
                var retryCount = queueMessage?.retryCount;

                if (retryCount >= 3)
                {
                    await this.errorNotifyService.SendErrorMessage($"快照已失敗3次，不再進行重試。");
                }
                else
                {
                    // 要傳送的 JSON 資料
                    var queueData = new
                    {
                        screenShotUrl = screenShotUrl,
                        retryCount = retryCount + 1,
                    };
                
                    string jsonQueueData = JsonSerializer.Serialize(queueData);
                
                    // SQS 訊息格式
                    SendMessageRequest request = new SendMessageRequest
                    {
                        QueueUrl = Environment.GetEnvironmentVariable("SQS_QUEUE_URL") ?? "",
                        MessageBody = jsonQueueData,
                    };
                
                    // 傳送訊息至 SQS 進行重試
                    SendMessageResponse sendMessageResponse = await this.sqsClient.SendMessageAsync(request);
                
                    // 檢查 response
                    if (sendMessageResponse.HttpStatusCode != HttpStatusCode.OK)
                    {
                        await this.errorNotifyService.SendErrorMessage($"ErrorRetryService 呼叫 sqsClient 失敗。HTTP 狀態碼: {sendMessageResponse.HttpStatusCode}");
                    }
                
                    this.logger.LogInformation($"快照重試第{retryCount + 1}次，傳送訊息至 SQS 成功");
                
                    // 刪除舊訊息
                    var receiptHandle = message.ReceiptHandle;
                    await this.queueDeleteService.DeleteMessage(receiptHandle);
                
                    this.logger.LogInformation($"快照重試第{retryCount + 1}次，刪除舊訊息成功");
                }
            }
            catch (JsonException e)
            {
                await this.errorNotifyService.SendErrorMessage($"ErrorRetryService queueMessage JsonException: {e.Message}");
            }
            catch (AmazonSQSException e)
            {
                await this.errorNotifyService.SendErrorMessage($"快照重試失敗， SQS 異常：{e.Message}");
            }
            catch (Exception e)
            {
                await this.errorNotifyService.SendErrorMessage($"快照重試時發生錯誤：{e.Message}");
            }
        }
    }
}
