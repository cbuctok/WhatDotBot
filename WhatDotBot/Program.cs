namespace WhatDotBot
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Threading.Tasks;
    using Telegram.Bot;
    using Telegram.Bot.Args;
    using Telegram.Bot.Types.Enums;
    using Telegram.Bot.Types.InlineQueryResults;
    using Telegram.Bot.Types.ReplyMarkups;

    public static class Program
    {
        private static readonly TelegramBotClient Bot = new TelegramBotClient(Environment.GetEnvironmentVariable("TelegramApiKey"));

        public static void Main(string[] args)
        {
            var me = Bot.GetMeAsync().Result;
            Console.Title = me.Username;

            Bot.OnMessage += BotOnMessageReceived;
            Bot.OnMessageEdited += BotOnMessageReceived;
            Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
            Bot.OnInlineQuery += BotOnInlineQueryReceived;
            Bot.OnInlineResultChosen += BotOnChosenInlineResultReceived;
            Bot.OnReceiveError += BotOnReceiveError;

            Bot.StartReceiving(Array.Empty<UpdateType>());
            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();
            Bot.StopReceiving();
        }

        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            var callbackQuery = callbackQueryEventArgs.CallbackQuery;

            await Bot.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                $"Received {callbackQuery.Data}").ConfigureAwait(false);

            await Bot.SendTextMessageAsync(
                callbackQuery.Message.Chat.Id,
                $"Received {callbackQuery.Data}").ConfigureAwait(false);
        }

        private static void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
        {
            Console.WriteLine($"Received inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");
        }

        private static async void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs inlineQueryEventArgs)
        {
            Console.WriteLine($"Received inline query from: {inlineQueryEventArgs.InlineQuery.From.Id}");

            InlineQueryResultBase[] results = {
                new InlineQueryResultLocation(
                    id: "1",
                    latitude: 40.7058316f,
                    longitude: -74.2581888f,
                    title: "New York")   // displayed result
                    {
                        InputMessageContent = new InputLocationMessageContent(
                            latitude: 40.7058316f,
                            longitude: -74.2581888f)    // message if result is selected
                    },

                new InlineQueryResultLocation(
                    id: "2",
                    latitude: 13.1449577f,
                    longitude: 52.507629f,
                    title: "Berlin") // displayed result
                    {
                        InputMessageContent = new InputLocationMessageContent(
                            latitude: 13.1449577f,
                            longitude: 52.507629f)   // message if result is selected
                    }
            };

            await Bot.AnswerInlineQueryAsync(
                inlineQueryEventArgs.InlineQuery.Id,
                results,
                isPersonal: true,
                cacheTime: 0).ConfigureAwait(false);
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;

            if (message == null || message.Type != MessageType.Text) return;

            switch (message.Text.Split(' ').First())
            {
                // send inline keyboard
                case "/inline":
                    await InlineCommand(message).ConfigureAwait(false); break;

                // send custom keyboard
                case "/keyboard":
                    await KeyboardCommand(message).ConfigureAwait(false); break;

                // send a photo
                case "/photo":
                    await SendPhotoCommand(message).ConfigureAwait(false); break;

                // request location or contact
                case "/request":
                    await RequestCommand(message).ConfigureAwait(false); break;

                case "/ip":
                    await IpCommand(message).ConfigureAwait(false); break;

                default:
                    await ListCommands(message).ConfigureAwait(false); break;
            }
        }

        private static async Task SendPhotoCommand(Telegram.Bot.Types.Message message)
        {
            const string file = "Files/tux.png";
            await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto).ConfigureAwait(false);

            using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                await Bot.SendPhotoAsync(
                    message.Chat.Id,
                    fileStream,
                    "Nice Picture").ConfigureAwait(false);
            }
        }

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            Console.WriteLine("Received error: {0} — {1}",
                receiveErrorEventArgs.ApiRequestException.ErrorCode,
                receiveErrorEventArgs.ApiRequestException.Message);
        }

        private static async Task InlineCommand(Telegram.Bot.Types.Message message)
        {
            await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing).ConfigureAwait(false);

            await Task.Delay(500).ConfigureAwait(false); // simulate longer running task

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                        new [] // first row
                        {
                            InlineKeyboardButton.WithCallbackData("1.1"),
                            InlineKeyboardButton.WithCallbackData("1.2"),
                        },
                        new [] // second row
                        {
                            InlineKeyboardButton.WithCallbackData("2.1"),
                            InlineKeyboardButton.WithCallbackData("2.2"),
                        }
                    });

            await Bot.SendTextMessageAsync(
                message.Chat.Id,
                "Choose",
                replyMarkup: inlineKeyboard).ConfigureAwait(false);
        }

        private static async Task IpCommand(Telegram.Bot.Types.Message message)
        {
            await Bot
                .SendTextMessageAsync(
                message.Chat.Id,
                string.Join(Environment.NewLine, NetworkInterface
                    .GetAllNetworkInterfaces()
                    .SelectMany(m => m.GetIPProperties().UnicastAddresses)
                    .Select(s => s.Address.ToString())
                    .OrderBy(b => b))
                ).ConfigureAwait(false);
        }

        private static async Task KeyboardCommand(Telegram.Bot.Types.Message message)
        {
            ReplyKeyboardMarkup ReplyKeyboard = new[]
            {
                new[] { "1.1", "1.2" },
                new[] { "2.1", "2.2" }
            };

            await Bot.SendTextMessageAsync(
                message.Chat.Id,
                "Choose",
                replyMarkup: ReplyKeyboard).ConfigureAwait(false);
        }

        private static async Task ListCommands(Telegram.Bot.Types.Message message)
        {
            const string usage = @"
Usage:
/inline   - send inline keyboard
/keyboard - send custom keyboard
/photo    - send a photo
/request  - request location or contact
/ip       - list ip addresses";

            await Bot.SendTextMessageAsync(
                message.Chat.Id,
                usage,
                replyMarkup: new ReplyKeyboardRemove()).ConfigureAwait(false);
        }

        private static async Task RequestCommand(Telegram.Bot.Types.Message message)
        {
            var RequestReplyKeyboard = new ReplyKeyboardMarkup(new[]
            {
                KeyboardButton.WithRequestLocation("Location"),
                KeyboardButton.WithRequestContact("Contact")
            });

            await Bot.SendTextMessageAsync(
                message.Chat.Id,
                "Who or Where are you?",
                replyMarkup: RequestReplyKeyboard).ConfigureAwait(false);
        }
    }
}