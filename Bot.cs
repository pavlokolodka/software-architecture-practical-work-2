using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;

namespace Lab_2_Telegram_Bot
{
    public class Bot
    {
        private TelegramBotClient botClient;
        private CancellationTokenSource cts;

        public Bot()
        {
            string botToken = Environment.GetEnvironmentVariable("TELEGRAM_TOKEN");
            botClient = new TelegramBotClient(botToken);
         
        }

        public void Register()
        {
            using CancellationTokenSource cts = new();
            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            Console.ReadLine();
            cts.Cancel();
        }
        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.CallbackQuery)
            {
                Console.WriteLine(update.CallbackQuery?.Data);  
            }
            // Only process Message updates: https://core.telegram.org/bots/api#update
            if (update.Message is not { } message)
                return;
            // Only process text messages
            if (message.Text is not { } messageText)
                return;

            var chatId = message.Chat.Id;
            const string START_COMMAND = "/start";
            const string GENERATE_COMMAND = "/generate";

            switch (message.Text)
            {
                case START_COMMAND:
                    await HandleStartConversation(message, cancellationToken);
                    break;
                case GENERATE_COMMAND:
                    await HandleGenerateMessage(message, cancellationToken);    
                    break;
                default:
                    await HandleDefaulMessage(message, cancellationToken);
                    break;
            }

            Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");      
        }

        async Task<Message> HandleStartConversation(Message message, CancellationToken cancellationToken)
        {
            string username = message.From.Username;
            // underscore has special meaning in Markdown for italic formatting
            if (username.Contains("_"))
            {
                username = username.Replace("_", "\\_");
            }
            var chatId = message.Chat.Id;
            string greetingMessage = $"Hello, @{username}\\. This is the Telegram version of [Decibear API](https://www.dicebear.com/)";


            return await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: greetingMessage,
                parseMode: ParseMode.MarkdownV2,
                cancellationToken: cancellationToken
                );
        }

        async Task<Message> HandleDefaulMessage(Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var unknownMessage = message.Text;
            string defaultMessage = $"Sorry, but '{unknownMessage}' it's not a defined command. Please use the menu, or just type /generate";

            return await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: defaultMessage,
                cancellationToken: cancellationToken
                );
        }

        async Task<Message> HandleGenerateMessage(Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            string replyMessage = $"Cool, let's generate a new avatar. Please select a style";

            InlineKeyboardMarkup inlineKeyboard = new(new[]
            {
               new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Adventurer", callbackData: "adventurer"),
                    InlineKeyboardButton.WithCallbackData(text: "Adventurer Neutral", callbackData: "adventurer_neutral"),
                    InlineKeyboardButton.WithCallbackData(text: "Avataaars", callbackData: "avataaars"),
                    InlineKeyboardButton.WithCallbackData(text: "Avataaars Neutral", callbackData: "avataaars_neutral"),
                    InlineKeyboardButton.WithCallbackData(text: "Big Ears", callbackData: "big_ears"),
                    InlineKeyboardButton.WithCallbackData(text: "Big Ears Neutral", callbackData: "big_ears_neutral"),
                    InlineKeyboardButton.WithCallbackData(text: "Big Smile", callbackData: "big_smile")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Bottts", callbackData: "bottts"),
                    InlineKeyboardButton.WithCallbackData(text: "Bottts Neutral", callbackData: "bottts_neutral"),
                    InlineKeyboardButton.WithCallbackData(text: "Croodles", callbackData: "croodles"),
                    InlineKeyboardButton.WithCallbackData(text: "Croodles Neutral", callbackData: "croodles_neutral"),
                    InlineKeyboardButton.WithCallbackData(text: "Fun Emoji", callbackData: "fun_emoji"),
                    InlineKeyboardButton.WithCallbackData(text: "Icons", callbackData: "icons"),
                    InlineKeyboardButton.WithCallbackData(text: "Identicon", callbackData: "identicon")
                },

                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Initials", callbackData: "initials"),
                    InlineKeyboardButton.WithCallbackData(text: "Lorelei", callbackData: "lorelei"),
                    InlineKeyboardButton.WithCallbackData(text: "Lorelei Neutral", callbackData: "lorelei_neutral"),
                    InlineKeyboardButton.WithCallbackData(text: "Micah", callbackData: "micah"),
                    InlineKeyboardButton.WithCallbackData(text: "Miniavs", callbackData: "miniavs"),
                    InlineKeyboardButton.WithCallbackData(text: "Notionists", callbackData: "notionists"),
                    InlineKeyboardButton.WithCallbackData(text: "Notionists Neutral", callbackData: "notionists_neutral")
                },

                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Open Peeps", callbackData: "open_peeps"),
                    InlineKeyboardButton.WithCallbackData(text: "Personas", callbackData: "personas"),
                    InlineKeyboardButton.WithCallbackData(text: "Pixel Art", callbackData: "pixel_art"),
                    InlineKeyboardButton.WithCallbackData(text: "Pixel Art Neutral", callbackData: "pixel_art_neutral"),
                    InlineKeyboardButton.WithCallbackData(text: "Rings", callbackData: "rings"),
                    InlineKeyboardButton.WithCallbackData(text: "Shapes", callbackData: "shapes"),
                    InlineKeyboardButton.WithCallbackData(text: "Thumbs", callbackData: "thumbs")
                }
            });

            return await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: replyMessage,
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken
                );
        }


        Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }
}
