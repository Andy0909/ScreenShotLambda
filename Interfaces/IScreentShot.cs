using Amazon.Lambda.Core;

namespace ScreenShotLambda.Interfaces
{
    // �ַӤ���
    public interface IScreenShot
    {
        /// <summary>
        /// �i��ַ�
        /// </summary>
        /// <param name="screenShotUrl"></param>
        /// <param name="context"></param>
        Task screenShot(string screenShotUrl, ILambdaContext context);
    }
}