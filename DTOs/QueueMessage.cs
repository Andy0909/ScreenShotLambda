namespace ScreenShotLambda.DTOs
{
    /// <summary>
    /// 快照參數物件
    /// </summary>
    public class QueueMessage
    {
        /// <summary>
        /// 快照網址
        /// </summary>
        public string screenShotUrl { get; set; }

        /// <summary>
        /// 重試次數
        /// </summary>
        public int retryCount { get; set; }
    }
}
