using System;
using System.Net.NetworkInformation;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Lab_2_Telegram_Bot
{
    public class Bot
    {
        private TelegramBotClient botClient;
        private CancellationTokenSource cts;
        private Dictionary<long, UserConfig> userContext;

        public Bot()
        {
            string botToken = Environment.GetEnvironmentVariable("TELEGRAM_TOKEN");
            botClient = new TelegramBotClient(botToken);
         
        }

        public void Register()
        {
            userContext = new Dictionary<long, UserConfig>();
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
                var callbackQueryData = update.CallbackQuery?.Data;
                var callbackQueryMessage = update.CallbackQuery?.Message;

                if (callbackQueryData != null && callbackQueryMessage != null)
                {
                    await HandleGenerateAvatarCallback(callbackQueryMessage, callbackQueryData, cancellationToken);
                }

            }
            // Only process Message updates: https://core.telegram.org/bots/api#update
            if (update.Message is not { } message)
                return;
            // Only process text messages
            if (message.Text is not { } messageText)
                return;

            var chatId = message.Chat.Id;

            Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

            switch (message.Text)
            {
                case Command.START:
                    await HandleStartConversation(message, cancellationToken);
                    break;
                case Command.GENERATE:
                    await HandleGenerateCommand(message, cancellationToken);    
                    break;
                case Command.SET_SIZE:
                    await HandleSetImageSizeCommand(message, cancellationToken);
                    break; 
                case Command.SET_SEED:
                    await HandleSetSeedCommand(message, cancellationToken);
                    break;
                case Command.HELP:
                    await HandleGetHelpCommand(message, cancellationToken);
                    break;
                default:
                    string previousOperation = "";
                    try
                    {
                        previousOperation = GetUserConfigPreviousOperation(chatId);
                    } catch (Exception ex)
                    {
                        await HandleInternalException(chatId, cancellationToken);
                        return;
                    }

                    Console.WriteLine($"Previous {previousOperation}");

                    if (previousOperation.Equals(Command.SET_SIZE))
                    {
                        await HandleSetSizeCommand(message, cancellationToken);
                        return;
                    }

                    if (previousOperation.Equals(Command.SET_SEED))
                    {
                        await HandleSetSeedMessage(message, cancellationToken);
                        return;
                    }

                    await HandleDefaulMessage(message, cancellationToken);
                    break;
            }   
        }

        async Task<Message> HandleStartConversation(Message message, CancellationToken cancellationToken)
        {
            string username = message.From.Username;
            // underscore has the special meaning in Markdown for italic formatting
            if (username.Contains("_"))
            {
                username = username.Replace("_", "\\_");
            }
            var chatId = message.Chat.Id;
            string greetingMessage = $"Hello, @{username}\\. This is the Telegram version of [Decibear API](https://www.dicebear.com/)";

            userContext.Add(chatId, new UserConfig());

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
            string defaultMessage = $"Sorry, but '{unknownMessage}' is not a defined command. Please use the menu, or just type /generate";

            return await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: defaultMessage,
                cancellationToken: cancellationToken
                );
        }

        async Task<Message> HandleSetSizeCommand(Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var size = message.Text;

            if (!int.TryParse(size, out int imageSize))
            {
                string defaultMessage = $"Sorry, but '{size}' is not an integer number. Please specify a valid number";

                return await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: defaultMessage,
                    cancellationToken: cancellationToken
                    );
            }

            if (imageSize < 0)
            {
                string defaultMessage = $"Sorry, but '{imageSize}' is less than 1. Please specify a correct number";

                return await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: defaultMessage,
                    cancellationToken: cancellationToken
                    );
            }
            
            try
            {
                SetUserConfigImageSize(chatId, imageSize);
            }
            catch ( Exception ex ) {
                return await HandleInternalException(chatId, cancellationToken);
            }
                      
            string successMessage = $"Successfully updated the current configuration.";
            
            return await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: successMessage,
                cancellationToken: cancellationToken
                );
        }

        async Task<Message> HandleSetSeedMessage(Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var seed = message.Text ?? "random";

            try
            {
                SetUserConfigImageSeed(chatId, seed);
            }
            catch (Exception ex)
            {
                return await HandleInternalException(chatId, cancellationToken);
            }

            string successMessage = $"Successfully updated the current configuration.";

            return await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: successMessage,
                cancellationToken: cancellationToken
                );
        }

        async Task<Message> HandleSetImageSizeCommand(Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;

            try
            {
                SetUserConfigCurrentOperation(chatId, Command.SET_SIZE);
            }
            catch (Exception ex)
            {
                return await HandleInternalException(chatId, cancellationToken);
            }


            string defaultMessage = $"Let's set the image size for future generated avatars. The default one is 128, the minimum 1.\r\n" +
                $"For example, a size of 100 means 100*100 pixels.";

            return await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: defaultMessage,
                cancellationToken: cancellationToken
                );
        }
        async Task<Message> HandleGenerateAvatarCallback(Message message, string avatarStyle, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            int imageSize = 1;
            string imageSeed = "";

            try
            {
                imageSize = GetUserConfigImageSize(chatId);
                imageSeed = GetUserConfigImageSeed(chatId);
            }
            catch (Exception ex)
            {
                return await HandleInternalException(chatId, cancellationToken);
            }

            if (!AvatartStyle.AvatarStyles.Contains(avatarStyle))
            {
                string defaultMessage = $"Sorry, but '{avatarStyle}' it's not a defined avatar style. Please regenerate your image using /generate command";

                return await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: defaultMessage,
                    cancellationToken: cancellationToken
                    );
            }

            string uri = $"https://api.dicebear.com/8.x/{avatarStyle}/png?size={imageSize}&seed={imageSeed}";
            Console.WriteLine($"URI: {uri}");
           
            return await botClient.SendPhotoAsync(
                    chatId: chatId,
                    photo: InputFile.FromUri(uri),
                    cancellationToken: cancellationToken
            );
        }

        async Task<Message> HandleGetHelpCommand(Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
           
            string replyMessage = $"This is the Telegram version of [DiceBear](https://www.dicebear.com) avatar library\\. Use following commands to interact with bot: \r\n" +
                $"\r\n" +
                $"/generate \\- Generate a new avatar\r\n" +
                $"\r\n" +
                $"/set\\_size \\- Set a new image size\\. The default is `128`, the minimum is `1`\\. For example, if you set the size to 10, it means that the image will be 10x10 pixels\r\n" +
                $"\r\n" +
                $"/set\\_seed \\- Set a new image seed\\. This can be any word that be used for built\\-in [PRNG](https://en.wikipedia.org/wiki/Pseudorandom_number_generator)\\." +
                $"You can play with different seeds in the [playground](https://www.dicebear.com/playground/)";

            return await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: replyMessage,
                    parseMode: ParseMode.MarkdownV2,
                    cancellationToken: cancellationToken
            );
        }
        async Task<Message> HandleSetSeedCommand(Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            try
            {
                SetUserConfigCurrentOperation(chatId, Command.SET_SEED);
            }
            catch (Exception ex)
            {
                return await HandleInternalException(chatId, cancellationToken);
            }

            string replyMessage = $"Let's set a new [seed](https://www.dicebear.com/styles/adventurer/#options-seed) for the future avatar generation\\. Please type your word that'll be used as the seed\\.";
            return await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: replyMessage,
                    parseMode: ParseMode.MarkdownV2,
                    cancellationToken: cancellationToken
            );
        }

            


        async Task<Message> HandleGenerateCommand(Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;

            try
            {
                SetUserConfigCurrentOperation(chatId, Command.GENERATE);
            }
            catch (Exception ex)
            {
                return await HandleInternalException(chatId, cancellationToken);
            }

            string replyMessage = $"Cool, let's generate a new avatar. Please select a style:";

            InlineKeyboardMarkup inlineKeyboard = new(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Adventurer", callbackData: "adventurer"),
                    InlineKeyboardButton.WithCallbackData(text: "Adventurer Neutral", callbackData: "adventurer-neutral"),
                    InlineKeyboardButton.WithCallbackData(text: "Avataaars", callbackData: "avataaars")
                },

                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Avataaars Neutral", callbackData: "avataaars-neutral"),
                    InlineKeyboardButton.WithCallbackData(text: "Big Ears", callbackData: "big-ears"),
                    InlineKeyboardButton.WithCallbackData(text: "Big Ears Neutral", callbackData: "big-ears-neutral")
                },

                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Big Smile", callbackData: "big-smile"),
                    InlineKeyboardButton.WithCallbackData(text: "Bottts", callbackData: "bottts"),
                    InlineKeyboardButton.WithCallbackData(text: "Bottts Neutral", callbackData: "bottts-neutral")
                },

                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Croodles", callbackData: "croodles"),
                    InlineKeyboardButton.WithCallbackData(text: "Croodles Neutral", callbackData: "croodles-neutral"),
                    InlineKeyboardButton.WithCallbackData(text: "Fun Emoji", callbackData: "fun-emoji")
                },

                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Icons", callbackData: "icons"),
                    InlineKeyboardButton.WithCallbackData(text: "Identicon", callbackData: "identicon"),
                    InlineKeyboardButton.WithCallbackData(text: "Initials", callbackData: "initials")
                },

                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Lorelei", callbackData: "lorelei"),
                    InlineKeyboardButton.WithCallbackData(text: "Lorelei Neutral", callbackData: "lorelei-neutral"),
                    InlineKeyboardButton.WithCallbackData(text: "Micah", callbackData: "micah")
                },

                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Miniavs", callbackData: "miniavs"),
                    InlineKeyboardButton.WithCallbackData(text: "Notionists", callbackData: "notionists"),
                    InlineKeyboardButton.WithCallbackData(text: "Notionists Neutral", callbackData: "notionists-neutral")
                },

                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Open Peeps", callbackData: "open-peeps"),
                    InlineKeyboardButton.WithCallbackData(text: "Personas", callbackData: "personas"),
                    InlineKeyboardButton.WithCallbackData(text: "Pixel Art", callbackData: "pixel-art")
                },

                new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Pixel Art Neutral", callbackData: "pixel-art-neutral"),
                    InlineKeyboardButton.WithCallbackData(text: "Rings", callbackData: "rings"),
                    InlineKeyboardButton.WithCallbackData(text: "Shapes", callbackData: "shapes")
                },

                new[]
                {
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

        private async Task<Message> HandleInternalException(long chatId, CancellationToken cancellationToken)
        {
            string defaultMessage = $"Sorry, something went wrong. Please restart the bot.";

            return await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: defaultMessage,
                cancellationToken: cancellationToken
                );
        }

        private UserConfig GetUserConfig(long chatId)
        {
            var isCurrentConfigExists = userContext.TryGetValue(chatId, out UserConfig config);

            if (!isCurrentConfigExists)
            {
                throw new Exception("Cannot access the current user config");
            }

            return config;
        }

        private void SetUserConfigImageSize(long chatId, int size)
        {
            var config = GetUserConfig(chatId);
            config.ImageSize = size;
            userContext[chatId] = config;
        }

        private void SetUserConfigImageSeed(long chatId, string seed)
        {
            var config = GetUserConfig(chatId);
            config.ImageSeed = seed;
            userContext[chatId] = config;
        }

        private void SetUserConfigCurrentOperation(long chatId, string operation)
        {
            var config = GetUserConfig(chatId);
            config.PreviousOperation = operation;
            userContext[chatId] = config;
        }

        private string GetUserConfigPreviousOperation(long chatId)
        {
            var config = GetUserConfig(chatId);
            return config.PreviousOperation;
        }

        private int GetUserConfigImageSize(long chatId)
        {
            var config = GetUserConfig(chatId);
            return config.ImageSize;
        }

        private string GetUserConfigImageSeed(long chatId)
        {
            var config = GetUserConfig(chatId);
            return config.ImageSeed;
        }
    }
    public static class AvatartStyle
    {
        public static string[] AvatarStyles = new[]
        {
        "adventurer",
        "adventurer-neutral",
        "avataaars",
        "avataaars-neutral",
        "big-ears",
        "big-ears-neutral",
        "big-smile",
        "bottts",
        "bottts-neutral",
        "croodles",
        "croodles-neutral",
        "fun-emoji",
        "icons",
        "identicon",
        "initials",
        "lorelei",
        "lorelei-neutral",
        "micah",
        "miniavs",
        "notionists",
        "notionists-neutral",
        "open-peeps",
        "personas",
        "pixel-art",
        "pixel-art-neutral",
        "rings",
        "shapes",
        "thumbs"
    };
    }

}
