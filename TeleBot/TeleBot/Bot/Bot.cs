using System.Threading.Tasks;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using System;
using System.Threading;
using System.IO;
using System.Linq;
using System.Diagnostics;
using PluginBase;
using Telegram.Bot.Extensions.Polling;
using System.Collections.Generic;
using Telegram.Bot;

namespace TeleBot
{
    public class Chat : IPluginScope
    {
        public long ChatId { get; set; }

        private readonly TelegramBotClient _botClient;

        private ManualResetEvent _messageWaiter = new ManualResetEvent(false);
        private string _lastMessage;
        private bool _waitingMessage = false;

        private ManualResetEvent _pickOptionWaiter = new ManualResetEvent(false);
        private string _pickedOption;
        private bool _waitingPickIption = false;

        private List<IPlugin> Plugins = new List<IPlugin>();

        public Chat(TelegramBotClient botClient)
        {
            _botClient = botClient;

            Plugins.Add(new SystemPlugin());
            Plugins.Add(new ApplicationsPlugin());

            
        }

        public async void BotOnMessageReceived(ITelegramBotClient botClient, Message message)
        {
            if (_waitingMessage)
            {
                _lastMessage = message.Text;

                _messageWaiter.Set();

                return;
            }

            if (_waitingPickIption)
            {
                _pickedOption = message.Text;
                _waitingPickIption = false;
                _pickOptionWaiter.Set();
                return;
            }

            var command = Plugins.FirstOrDefault(p => p.GetCommand() == message.Text);

            if(command != null)
            {
                try
                {
                    await command?.ExecuteAsync(this);

                }
                catch (Exception)
                {

                    ;
                }
            }

        }

        public async Task<string> RequestMessage(string caption, string cancelText)
        {

            ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(
            new[]
            {
                new KeyboardButton[] { cancelText},
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };

            var message = await _botClient.SendTextMessageAsync(chatId: ChatId,
                                                        text: caption,
                                                        replyMarkup: replyKeyboardMarkup);

            _messageWaiter.Reset();

            _waitingMessage = true;

            var sent = _messageWaiter.WaitOne(10000);

            _waitingMessage = false;

            if (!sent)
            {
                await _botClient.SendTextMessageAsync(chatId: ChatId,
                                                        text: "Cancelado por inatividade",
                                                        replyMarkup: new ReplyKeyboardRemove());

                throw new Exception("Cancelado por inatividade");


            }


            return this._lastMessage;

        }

        public async Task SendMessage(string message)
        {
            await _botClient.SendTextMessageAsync(chatId: ChatId, text: message, replyMarkup: new ReplyKeyboardRemove());
        }

        public async Task<string> RequestPickOption(PickOption request)
        {
            await _botClient.SendChatActionAsync(ChatId, ChatAction.Typing);

            var ops = request.Options.Select(op =>

                        InlineKeyboardButton.WithCallbackData(op.Text, op.Value));


            InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(ops);

            var message = await _botClient.SendTextMessageAsync(chatId: ChatId,
                                                        text: request.Text,
                                                        replyMarkup: inlineKeyboard);

            _pickOptionWaiter.Reset();

            _waitingPickIption = true;

            var sent = _pickOptionWaiter.WaitOne(10 * 1000);

            _waitingPickIption = false;

            var v = Enumerable.Empty<InlineKeyboardButton>();
            await _botClient.EditMessageReplyMarkupAsync(ChatId, message.MessageId, new InlineKeyboardMarkup(v));

            if (!sent)
            {
                await _botClient.SendTextMessageAsync(chatId: ChatId,
                                                        text: "Cancelado por inatividade",
                                                        replyMarkup: new ReplyKeyboardRemove());

                throw new Exception("Cancelado por inatividade");
            }

            return _pickedOption;

        }

        public IEnumerable<IPlugin> GetPlugins()
        {
            return this.Plugins.Where(p => p.GetCommand() != "/applications");
        }

        public async void BotOnCallbackQueryReceived(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            if (_waitingPickIption)
            {
                _pickedOption = callbackQuery.Data ?? string.Empty;
                _waitingPickIption = false;
                _pickOptionWaiter.Set();

            }
        }
    }

    class ApplicationsPlugin : IPlugin
    {
        public ApplicationsPlugin()
        {
            
        }

        public async Task ExecuteAsync(IPluginScope scope)
        {
            var x = scope.GetPlugins();

            PickOption request = new PickOption()
            {
                Text = "Selecione:",
                Options = x.Select(p => new Option() {Text = p.GetName(), Value = p.GetCommand() }).ToList()
            };

            var ret = await scope.RequestPickOption(request);

            var plug = scope.GetPlugins().FirstOrDefault(p => p.GetCommand() == ret);

            if(plug != null)
            {
                try
                {
                    await plug.ExecuteAsync(scope);
                }
                catch (Exception)
                {
                    ;
                }
            }
            else
            {
                await scope.SendMessage("Aplicação " + ret + " não encontrada");
            }

            
        }

        public string GetCommand()
        {
            return "/applications";
        }

        public string GetName()
        {
            return "Aplicações";
        }

        public void Init()
        {
            //..
        }
    }

    public static class BotManager
    {
        private static List<Bot> Bots;

        public static Bot Add(string token)
        {
            if (StorageManager.Data.Tokens.Any(t => t == token))
                throw new Exception($"{token} already added");

            var bot = new Bot(token);

            if (!bot.IsOk())
                throw new Exception($"bot {token} is not working");

            Bots.Add(bot);

            StorageManager.Data.Tokens.Add(token);
            StorageManager.Save();

            bot.Start();

            return bot;
        }

        public static void Initialize()
        {
            Bots = StorageManager.Data.Tokens.Select(t => new Bot(t)).ToList();

            foreach (var bot in Bots)
            {
                bot.Start();
            }
        }

        public static void Finish()
        {


            foreach (var bot in Bots)
            {
                bot.Stop();
            }
        }

    }

    public class Bot
    {
        public string Token { get; set; }

        public List<Chat> Chats = new List<Chat>();

        public TelegramBotClient Client;

        CancellationTokenSource cts = new CancellationTokenSource();

        public Bot(string token)
        {
            Token = token;

            Client = new TelegramBotClient(Token);

        }

        public bool IsOk()
        {
            try
            {
                var m = Client.GetMeAsync().Result;

                return m.IsBot;

            }
            catch (Exception)
            {

                return false;
            }

        }

        public void Start()
        {
            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            ReceiverOptions receiverOptions = new ReceiverOptions() { AllowedUpdates = { } };


            IEnumerable<BotCommand> commands = new List<BotCommand>()
            {
                new BotCommand(){Command = "applications", Description = "Aplicações"},
                new BotCommand(){Command = "config", Description = "Configurações"},
            };

            Client.SetMyCommandsAsync(commands);

            Client.StartReceiving(this.HandleUpdateAsync,
                               this.HandleErrorAsync,
                               receiverOptions,
                               cts.Token);
        }

        public void Stop()
        {
            cts.Cancel();
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var handler = update.Type switch
            {
                // UpdateType.Unknown:
                // UpdateType.ChannelPost:
                // UpdateType.EditedChannelPost:
                // UpdateType.ShippingQuery:
                // UpdateType.PreCheckoutQuery:
                // UpdateType.Poll:
                UpdateType.Message => BotOnMessageReceived(botClient, update.Message!),
                UpdateType.EditedMessage => BotOnMessageReceived(botClient, update.EditedMessage!),
                UpdateType.CallbackQuery => BotOnCallbackQueryReceived(botClient, update.CallbackQuery!),
                UpdateType.InlineQuery => BotOnInlineQueryReceived(botClient, update.InlineQuery!),
                UpdateType.ChosenInlineResult => BotOnChosenInlineResultReceived(botClient, update.ChosenInlineResult!),
                _ => UnknownUpdateHandlerAsync(botClient, update)
            };

            try
            {
                await handler;
            }
            catch (Exception exception)
            {
                await HandleErrorAsync(botClient, exception, cancellationToken);
            }
        }

        private async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
        {

            //Console.WriteLine($"Receive message type: {message.Type}");
            if (message.Type != MessageType.Text)
                return;

            var chat = Chats.FirstOrDefault(f => f.ChatId == message.Chat.Id);

            if (/*message.Text.Equals("/start") &&*/ chat == null)
            {
                chat = new Chat(Client)
                {
                    ChatId = message.Chat.Id
                };

                Chats.Add(chat);

                if (message.Text.Equals("/start"))
                {
                    await botClient.SendTextMessageAsync(chat.ChatId, "Bem vindo ao TeleBot!");
                    return;
                }

            }


            chat.BotOnMessageReceived(botClient, message);

        }

        // Process Inline Keyboard callback data
        private async Task BotOnCallbackQueryReceived(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            //Process.Start("C:\\Windows\\System32\\calc.exe");

            //_pickedOption = callbackQuery.Data ?? string.Empty;

            
            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: $"Received {callbackQuery.Data}");

            var chat = Chats.FirstOrDefault(f => f.ChatId == callbackQuery.Message.Chat.Id);

            chat.BotOnCallbackQueryReceived(botClient, callbackQuery);

            //_pickOptionWaiter.Set();
        }

        private static async Task BotOnInlineQueryReceived(ITelegramBotClient botClient, InlineQuery inlineQuery)
        {
            Console.WriteLine($"Received inline query from: {inlineQuery.From.Id}");

            InlineQueryResult[] results = {
                // displayed result
                new InlineQueryResultArticle(
                    id: "3",
                    title: "TgBots",
                    inputMessageContent: new InputTextMessageContent(
                        "hello"
                    )
                )
            };

            await botClient.AnswerInlineQueryAsync(inlineQueryId: inlineQuery.Id,
                                                   results: results,
                                                   isPersonal: true,
                                                   cacheTime: 0);
        }

        private static Task BotOnChosenInlineResultReceived(ITelegramBotClient botClient, ChosenInlineResult chosenInlineResult)
        {
            Console.WriteLine($"Received inline result: {chosenInlineResult.ResultId}");
            return Task.CompletedTask;
        }

        private static Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
        {
            Console.WriteLine($"Unknown update type: {update.Type}");
            return Task.CompletedTask;
        }
    }

}
