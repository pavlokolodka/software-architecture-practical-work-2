using Lab_2_Telegram_Bot;

DotNetEnv.Env.TraversePath().Load();
var bot = new Bot();
bot.Register();
Console.WriteLine("Bot started listening...");

