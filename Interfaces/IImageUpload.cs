using Amazon.Lambda.Core;

namespace ScreenShotLambda.Interfaces
{
    // 圖片上傳介面
    public interface IImageUpload
    {
        /// <summary>
        /// 上傳快照圖片至 S3
        /// </summary>
        /// <param name="context"></param>
        Task uploadImageToS3(ILambdaContext context);
    }
}