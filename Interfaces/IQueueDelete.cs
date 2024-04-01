namespace ScreenShotLambda.Interfaces
{
    // 刪除 queue 介面
    public interface IQueueDelete
    {
        /// <summary>
        /// 刪除 queue
        /// </summary>
        /// <param name="receiptHandle"></param>
        Task DeleteMessage(string receiptHandle);
    }
}
