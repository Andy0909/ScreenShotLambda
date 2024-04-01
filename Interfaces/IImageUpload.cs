namespace ScreenShotLambda.Interfaces
{
    // 圖片上傳介面
    public interface IImageUpload
    {
        /// <summary>
        /// 上傳快照圖片至 S3
        /// </summary>
        Task UploadImageToS3();
    }
}
