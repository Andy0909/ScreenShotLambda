namespace ScreenShotLambda.Services
{
    /// <summary>
    /// 取得時間服務
    /// </summary>
    public class TimeService
    {
        /// <summary>
        /// 台北時區的 TimeZoneInfo 物件
        /// </summary>
        private readonly TimeZoneInfo taipeiTimeZone;

        /// <summary>
        /// 建構子
        /// </summary>
        public TimeService()
        {
            // 使用台灣標準時間的時區
            taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");

        }

        /// <summary>
        /// 取得當前台北時間
        /// </summary>
        public DateTime GetCurrentTaipeiTime()
        {
            return TimeZoneInfo.ConvertTime(DateTime.Now, taipeiTimeZone);
        }
    }
}
