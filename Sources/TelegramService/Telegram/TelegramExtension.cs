namespace TelegramService.Telegram
{
    public static class TelegramExtension
    {
        /// <summary> Escape telegram speccodes </summary>
        public static string EscapeHtml(this string str)
        {
            str = str.Replace("<", "&lt;");
            str = str.Replace(">", "&gt;");

            return str;
        }
    }
}