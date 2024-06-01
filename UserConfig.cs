using System;
namespace Lab_2_Telegram_Bot
{
    public class UserConfig
    {
        public int ImageSize { get; set; } = 128;
        public string ImageSeed { get; set; } = "Oscar";
        public string PreviousOperation { get; set; } = String.Empty;
    }
}
