// See https://aka.ms/new-console-template for more information
using Lab_2_Telegram_Bot;

//DotNetEnv.Env.Load("./.env");
DotNetEnv.Env.TraversePath().Load();

string botToken = Environment.GetEnvironmentVariable("TELEGRAM_TOKEN");
Console.WriteLine("token", botToken);
var bot = new Bot();
bot.Register();
Console.WriteLine("Hello, World!");

