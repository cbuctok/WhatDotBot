namespace WhatDotBot
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Serialization;
    using Telegram.Bot;
    using Telegram.Bot.Args;
    using Telegram.Bot.Types;
    using Telegram.Bot.Types.Enums;
    using Telegram.Bot.Types.InlineQueryResults;
    using Telegram.Bot.Types.ReplyMarkups;
    using WhatDotBot.Models;

    public static class Program
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(Program));
        private static readonly string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly TelegramBotClient Bot = new TelegramBotClient(Environment.GetEnvironmentVariable("TelegramApiKey"));
        private static readonly string filePath = $"{assemblyFolder}/users.xml";
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(HashSet<Subscriber>));
        private static readonly HashSet<Subscriber> subscribers = new HashSet<Subscriber>();

        public static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure(_log.Logger.Repository, new FileInfo($"{assemblyFolder}/log4net.xml"));
            _log.Info(string.Format("{0} start {0}", new string('*', 10)));
            if (System.IO.File.Exists(filePath))
            {
                _log.Info($"Loading {filePath}");
                HashSet<Subscriber> subs;
                using (var reader = System.IO.File.OpenRead(filePath))
                {
                    subs = serializer.Deserialize(reader) as HashSet<Subscriber> ?? new HashSet<Subscriber>();
                }

                if (subs.Count != 0)
                {
                    foreach (var sub in subs)
                    {
                        if (sub.ChatId == 0) continue;
                        subscribers.Add(sub);
                    }
                }
                _log.Info($"Loaded {subscribers.Count}");
            }

            var me = Bot.GetMeAsync().Result;
            Console.Title = me.Username;

            Bot.OnMessage += BotOnMessageReceived;
            Bot.OnMessageEdited += BotOnMessageReceived;
            Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
            Bot.OnInlineQuery += BotOnInlineQueryReceived;
            Bot.OnInlineResultChosen += BotOnChosenInlineResultReceived;
            Bot.OnReceiveError += BotOnReceiveError;

            Bot.StartReceiving(Array.Empty<UpdateType>());

            if (subscribers.Count != 0)
            {
                foreach (var sub in subscribers)
                    IpCommand(new Message() { Chat = new Chat() { Id = sub.ChatId } }).ConfigureAwait(false);
            }

            _log.Info($"Start listening for @{me.Username}");
            Thread.Sleep(Timeout.Infinite);
        }

        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            var callbackQuery = callbackQueryEventArgs.CallbackQuery;

            await Bot.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                $"Received {callbackQuery.Data}").ConfigureAwait(false);

            await Post(
                callbackQuery.Message.Chat.Id,
                $"Received {callbackQuery.Data}").ConfigureAwait(false);
        }

        private static void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
        {
            _log.Info($"Received inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");
        }

        private static async void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs inlineQueryEventArgs)
        {
            _log.Info($"Received inline query from: {inlineQueryEventArgs.InlineQuery.From.Id}");

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

            try
            {
                _log.Info($"<-{message.Chat.Id}: {message.Text}");
                switch (message.Text.Split(' ').First())
                {
                    case "/inline": await InlineCommand(message).ConfigureAwait(false); break;
                    case "/ip": await IpCommand(message).ConfigureAwait(false); break;
                    case "/keyboard": await KeyboardCommand(message).ConfigureAwait(false); break;
                    case "/photo": await SendPhotoCommand(message).ConfigureAwait(false); break;
                    case "/request": await RequestCommand(message).ConfigureAwait(false); break;
                    case "/temp": await TemperatureCommand(message).ConfigureAwait(false); break;
                    case "/subscribe": await SubscribeCommand(message).ConfigureAwait(false); break;
                    default: await ListCommands(message).ConfigureAwait(false); break;
                }
            }
            catch (Exception e)
            {
                _log.Error("ERROR:", e);
            }
        }

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            _log.InfoFormat("Received error: {0} — {1}",
                receiveErrorEventArgs.ApiRequestException.ErrorCode,
                receiveErrorEventArgs.ApiRequestException.Message);
        }

        private static async Task InlineCommand(Message message)
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

        private static async Task IpCommand(Message message)
        {
            await Post(message.Chat.Id,
                string.Join(
                    Environment.NewLine,
                    NetworkInterface
                        .GetAllNetworkInterfaces()
                        .SelectMany(m => m.GetIPProperties().UnicastAddresses)
                        .Select(s => s.Address.ToString())
                        .OrderBy(b => b))).ConfigureAwait(false);
        }

        private static async Task KeyboardCommand(Message message)
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

        private static async Task ListCommands(Message message)
        {
            const string usage = @"
Usage:
/inline   - send inline keyboard
/keyboard - send custom keyboard
/photo    - send a photo
/request  - request location or contact
/ip       - list ip addresses
/temp     - current temperature";

            await Bot.SendTextMessageAsync(
                message.Chat.Id,
                usage,
                replyMarkup: new ReplyKeyboardRemove()).ConfigureAwait(false);
        }

        private static async Task Post(Message message, string msg)
        {
            await Post(message.Chat.Id, msg).ConfigureAwait(false);
        }

        private static async Task Post(long id, string msg)
        {
            _log.Info($"->{id}: {msg.Replace(Environment.NewLine, " \\n ")}");
            await Bot.SendTextMessageAsync(id, msg).ConfigureAwait(false);
        }

        private static async Task RequestCommand(Message message)
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

        private static async Task SendPhotoCommand(Message message)
        {
            const string file = "Files/tux.png";
            if (!System.IO.File.Exists(file)) return;
            await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto).ConfigureAwait(false);

            using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                await Bot.SendPhotoAsync(
                    message.Chat.Id,
                    fileStream,
                    "Nice Picture").ConfigureAwait(false);
            }
        }

        private static async Task SubscribeCommand(Message message)
        {
            var id = message.Chat.Id;

            if (subscribers.Select(s => s.ChatId).Contains(id))
            {
                await Post(message, "Already subscribed").ConfigureAwait(false);
                return;
            }

            subscribers.Add(new Subscriber()
            {
                ChatId = message.Chat.Id,
                Name = $"{message.Chat.FirstName} {message.Chat.LastName}",
                Sub = Subscription.All
            });

            using (var writer = System.IO.File.CreateText(filePath))
                serializer.Serialize(writer, subscribers);

            await Post(message.Chat.Id, $"{message.Chat.Id} Subscribed").ConfigureAwait(false);
        }

        private static async Task TemperatureCommand(Message message)
        {
            ProcessStartInfo procStartInfo = new ProcessStartInfo("vcgencmd", "measure_temp")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            string result;
            using (Process proc = new Process { StartInfo = procStartInfo })
            {
                proc.Start();
                result = proc.StandardOutput.ReadToEnd();
            }

            await Post(message.Chat.Id, result).ConfigureAwait(false);
        }
    }
}