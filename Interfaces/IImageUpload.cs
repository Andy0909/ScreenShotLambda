using Amazon.Lambda.Core;

namespace ScreenShotLambda.Interfaces
{
    // �Ϥ��W�Ǥ���
    public interface IImageUpload
    {
        /// <summary>
        /// �W�ǧַӹϤ��� S3
        /// </summary>
        /// <param name="context"></param>
        Task uploadImageToS3(ILambdaContext context);
    }
}