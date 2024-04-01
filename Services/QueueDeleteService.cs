using Amazon.Lambda.Core;
using Amazon.SQS;
using Amazon.SQS.Model;
using ScreenShotLambda.Interfaces;
using System.Net;

namespace ScreenShotLambda.Services
{
    /// <summary>
    /// 刪除佇列服務
    /// </summary>
    class QueueDeleteService : IQueueDelete
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
        /// Lambda Log 物件
        /// </summary>
        private readonly ILambdaLogger logger;

        /// <summary>
        /// 建構子
        /// <param name="sqsClient">sqs client</param>
        /// <param name="errorNotifyService">異常通知服務</param>
        /// <param name="logger">log 紀錄</param>
        /// </summary>
        public QueueDeleteService(IAmazonSQS sqsClient, IErrorNotify errorNotifyService, ILambdaLogger logger)
        {
            this.sqsClient = sqsClient;
            this.errorNotifyService = errorNotifyService;
            this.logger = logger;
        }

        /// <summary>
        /// 刪除 queue
        /// <param name="receiptHandle"></param>
        /// </summary>
        public async Task DeleteMessage(string receiptHandle)
        {
            try
            {
                // 刪除 queue 所需參數
                DeleteMessageRequest request = new DeleteMessageRequest
                {
                    QueueUrl = Environment.GetEnvironmentVariable("SQS_QUEUE_URL") ?? "",
                    ReceiptHandle = receiptHandle,
                };

                // 執行刪除
                DeleteMessageResponse deleteMessageResponse = await this.sqsClient.DeleteMessageAsync(request);

                // 檢查 response
                if (deleteMessageResponse.HttpStatusCode != HttpStatusCode.OK)
                {
                    await this.errorNotifyService.SendErrorMessage($"QueueDeleteService 呼叫 sqsClient 失敗。HTTP 狀態碼: {deleteMessageResponse.HttpStatusCode}");
                }

                this.logger.LogInformation($"刪除 queue 成功");
            }
            catch (AmazonSQSException e)
            {
                await this.errorNotifyService.SendErrorMessage($"刪除 queue 失敗。SQS 異常：{e.Message}");
            }
            catch (Exception e)
            {
                await this.errorNotifyService.SendErrorMessage($"刪除 queue 失敗：{e.Message}");
            }
        }
    }
}
