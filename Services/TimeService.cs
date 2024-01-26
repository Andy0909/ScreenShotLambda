namespace ScreenShotLambda.Services
{
    public class TimeService
    {
        private readonly TimeZoneInfo taipeiTimeZone;

        public TimeService()
        {
            // 使用台灣標準時間的時區
            taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");

        }

        public DateTime getCurrentTaipeiTime()
        {
            return TimeZoneInfo.ConvertTime(DateTime.Now, taipeiTimeZone);
        }
    }
}