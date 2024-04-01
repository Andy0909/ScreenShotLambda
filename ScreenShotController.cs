using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using ScreenShotLambda.DTOs;
using ScreenShotLambda.Interfaces;
using System.Text.Json;

namespace ScreenShotLambda
{
    /// <summary>
    /// 訂單快照 controller
    /// </summary>
    class ScreenShotController
    {
        /// <summary>
        /// 快照物件
        /// </summary>
        private readonly IScreenShot screenShotService;

        /// <summary>
        /// 圖片上傳 s3 物件
        /// </summary>
        private readonly IImageUpload imageUploadService;

        /// <summary>
        /// 刪除 queue 物件
        /// </summary>
        private readonly IQueueDelete queueDeleteService;

        /// <summary>
        /// 異常通知物件
        /// </summary>
        private readonly IErrorNotify errorNotifyService;

        /// <summary>
        /// 異常重試物件
        /// </summary>
        private readonly IErrorRetry errorRetryService;

        /// <summary>
        /// Lambda Log 物件
        /// </summary>
        private readonly ILambdaLogger logger;

        /// <summary>
        /// 建構子
        /// <param name="screenShotService">快照服務</param>
        /// <param name="imageUploadService">圖片上傳S3服務</param>
        /// <param name="queueDeleteService">刪除佇列服務</param>
        /// <param name="errorNotifyService">異常通知服務</param>
        /// <param name="errorRetryService">異常重試服務</param>
        /// <param name="logger">log 紀錄</param>
        /// </summary>
        public ScreenShotController(
            IScreenShot screenShotService,
            IImageUpload imageUploadService,
            IQueueDelete queueDeleteService,
            IErrorNotify errorNotifyService,
            IErrorRetry errorRetryService,
            ILambdaLogger logger)
        {
            this.screenShotService = screenShotService;
            this.imageUploadService = imageUploadService;
            this.queueDeleteService = queueDeleteService;
            this.errorNotifyService = errorNotifyService;
            this.errorRetryService = errorRetryService;
            this.logger = logger;
        }

        /// <summary>
        /// 進行賣場快照
        /// </summary>
        /// <param name="message"></param>
        public async Task HandleScreenShot(SQSEvent.SQSMessage message)
        {
            try
            {
                // 解析 SQS 訊息中的訂單資訊
                var queueMessage = JsonSerializer.Deserialize<QueueMessage>(message.Body);

                // 提取訂單編號和賣場編號
                var orderId = queueMessage?.orderId;
                var martCode = queueMessage?.martCode;

                this.logger.LogInformation($"SQS 訊息解析完成。訂單編號：{orderId}，賣場編號：{martCode}");

                if (!string.IsNullOrEmpty(orderId) && !string.IsNullOrEmpty(martCode))
                {
                    // 進行快照
                    await this.screenShotService.ScreenShot(orderId, martCode);

                    // 上傳快照圖片至 S3
                    await this.imageUploadService.UploadImageToS3(orderId, martCode);

                    // 刪除 queue
                    var receiptHandle = message.ReceiptHandle;
                    await this.queueDeleteService.DeleteMessage(receiptHandle, orderId, martCode);
                }
            }
            catch (JsonException e)
            {
                // 異常通知
                await this.errorNotifyService.SendErrorMessage($"OrderInfo JsonException: {e.Message}");
            }
            catch (Exception e)
            {
                // 異常通知
                await this.errorNotifyService.SendErrorMessage($"訂單快照主程式錯誤: {e.Message}，將進行重試");

                // 重發 queue 進行重試
                await this.errorRetryService.RetryErrorQueue(message);
            }
        }
    }
}
