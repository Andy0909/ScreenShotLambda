namespace ScreenShotLambda.DTOs
{
    /// <summary>
    /// �ַӰѼƪ���
    /// </summary>
    public class QueueMessage
    {
        /// <summary>
        /// �ַӺ��}
        /// </summary>
        public string screenShotUrl { get; set; }

        /// <summary>
        /// ���զ���
        /// </summary>
        public int retryCount { get; set; }
    }
}
