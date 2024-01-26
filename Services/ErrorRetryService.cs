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
    class ErrorRetryService : IErrorRetry
    {
        // SQS Client
        private readonly IAmazonSQS sqsClient;

        // 異常通知 service
        private readonly IErrorNotify errorNotifyService;

        // 設定 JSON 序列化
        private readonly JsonSerializerOptions options;

        // 建構子
        public ErrorRetryService(IAmazonSQS sqsClient, IErrorNotify errorNotifyService)
        {
            this.sqsClient = sqsClient;

            this.errorNotifyService = errorNotifyService;

            this.options = new JsonSerializerOptions();
        }

        /// <summary>
        /// 有錯誤的 queue 進行重試
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        public async Task retryErrorQueue(SQSEvent.SQSMessage message, ILambdaContext context)
        {
            try
            {
                var queueMessage = JsonConvert.DeserializeObject<QueueMessage>(message.Body);

                if (queueMessage.retryCount >= 3)
                {
                    await this.errorNotifyService.sendErrorMessage($"快照已失敗3次，不再進行重試。", context);
                }
                else
                {
                    try
                    {
                        var retryCount = queueMessage.retryCount + 1;

                        // 要傳送的 JSON 資料
                        var queueData = new
                        {
                            screenShotUrl = queueMessage.screenShotUrl,
                            retryCount = retryCount,
                        };
                        var jsonQueueData = System.Text.Json.JsonSerializer.Serialize(queueData, this.options);

                        // SQS 訊息格式
                        var request = new SendMessageRequest
                        {
                            QueueUrl = "https://sqs.ap-northeast-1.amazonaws.com/781160412246/ScreenShot_Queue",
                            MessageBody = jsonQueueData,
                        };

                        // 傳送訊息至 SQS 進行重試
                        var sendMessageResponse = await this.sqsClient.SendMessageAsync(request);

                        context.Logger.LogInformation($"快照重試第{retryCount}次，傳送訊息至 SQS 成功");

                        // 刪除原本錯誤的訊息
                        await this.deleteErrorMessage(message, context);

                        context.Logger.LogInformation($"快照重試第{retryCount}次，刪除舊訊息成功");
                    }
                    catch (AmazonSQSException e)
                    {
                        await this.errorNotifyService.sendErrorMessage($"傳送訊息至 SQS 錯誤：{e.Message}。", context);
                    }
                }
            }
            catch (Newtonsoft.Json.JsonException e)
            {
                await this.errorNotifyService.sendErrorMessage($"ErrorRetryService queueMessage JsonException: {e.Message}", context);
            }
        }

        /// <summary>
        /// 刪除舊訊息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        private async Task deleteErrorMessage(SQSEvent.SQSMessage message, ILambdaContext context)
        {
            try
            {
                var request = new DeleteMessageRequest
                {
                    QueueUrl = "https://sqs.ap-northeast-1.amazonaws.com/781160412246/ScreenShot_Queue",
                    ReceiptHandle = message.ReceiptHandle,
                };

                var deleteMessageResponse = await this.sqsClient.DeleteMessageAsync(request);
            }
            catch (AmazonSQSException e)
            {
                await this.errorNotifyService.sendErrorMessage($"刪除 SQS 訊息發生錯誤：{e.Message}", context);
            }
        }
    }
}