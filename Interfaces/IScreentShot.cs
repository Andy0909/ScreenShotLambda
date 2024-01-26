using Amazon.Lambda.Core;

namespace ScreenShotLambda.Interfaces
{
    // 快照介面
    public interface IScreenShot
    {
        /// <summary>
        /// 進行快照
        /// </summary>
        /// <param name="screenShotUrl"></param>
        /// <param name="context"></param>
        Task screenShot(string screenShotUrl, ILambdaContext context);
    }
}