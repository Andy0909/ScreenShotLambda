namespace ScreenShotLambda.Interfaces
{
    // 異常通知介面
    public interface IErrorNotify
    {
        /// <summary>
        /// 異常通知
        /// </summary>
        /// <param name="message"></param>
        Task SendErrorMessage(string message);
    }
}
