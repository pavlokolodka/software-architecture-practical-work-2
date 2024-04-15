using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab_2_Telegram_Bot
{
    public class UserConfig
    {
        public int ImageSize { get; set; } = 128;
        public string ImageSeed { get; set; } = "Oscar";
        public string PreviousOperation { get; set; } = String.Empty;
    }
}
