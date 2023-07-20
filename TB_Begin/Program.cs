using Domain.Models;
using Newtonsoft.Json;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;

namespace BotClient
{
    internal class Program
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [System.Runtime.InteropServices.DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();
        static async Task Main(string[] args)
        {
            ShowWindow(GetConsoleWindow(), 1);

            Console.WriteLine("Hello, World!");

            var botClient = new TelegramBotClient("My Token Here");

            using CancellationTokenSource cts = new CancellationTokenSource();

            ReceiverOptions receiverOptions = new ReceiverOptions()
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
                );


            HttpClient client = new HttpClient();
            var result = await client.GetAsync("https://localhost:44356/api/Good");
            var test = await result.Content.ReadAsStringAsync();
            Console.WriteLine(test);

            Good[] goods = JsonConvert.DeserializeObject<Good[]>(test);
            foreach (var g in goods)
            {
                Console.WriteLine($"{g.GoodId} {g.Title} {g.Price}");
            }

            var me = await botClient.GetMeAsync();

            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();

            cts.Cancel();
        }
        static async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            HttpClient client = new HttpClient();
            var result = await client.GetAsync("https://localhost:44356/api/Good");
            var test = await result.Content.ReadAsStringAsync();
            var r2 = await client.GetAsync("https://localhost:44356/api/Category");
            var t2 = await r2.Content.ReadAsStringAsync();
            Category[] categories = JsonConvert.DeserializeObject<Category[]>(t2);
            Good[] goodes = JsonConvert.DeserializeObject<Good[]>(test);
            if (categories.Select(c => c.CategotyName).Contains(callbackQuery.Data))
            {
                Category category = JsonConvert.DeserializeObject<Category[]>(t2).Where(q => q.CategotyName == callbackQuery.Data.ToString()).First();
                Good[] goods = JsonConvert.DeserializeObject<Good[]>(test).Where(x => x.CategotyId == category.CategotyId).ToArray();

                InlineKeyboardButton[][] keyboardButtons = new InlineKeyboardButton[goods.Length][];
                for (int i = 0; i < goods.Length; i++)
                {
                    keyboardButtons[i] = new InlineKeyboardButton[]
                    { InlineKeyboardButton.WithCallbackData(goods[i].Title, callbackData: goods[i].Title)};
                }

                var inlineKeyboard = new InlineKeyboardMarkup(keyboardButtons);
                await botClient.SendTextMessageAsync(chatId: callbackQuery.Message.Chat.Id, "Электрички:", replyMarkup: inlineKeyboard);
            }
            if (goodes.Select(c => c.Title).Contains(callbackQuery.Data))
            {
                botClient.SendTextMessageAsync(chatId: callbackQuery.Message.Chat.Id, $"Вы купили {callbackQuery.Data}");
            }
        }
        static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, 
            CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.CallbackQuery)
            {
                await HandleCallbackQuery(botClient, update.CallbackQuery);
                return;
            }
            if (update.Message is not { } message)
                return;
            if (message.Text is not { } messageText)
                return;

            var chatId = message.Chat.Id;

            Console.WriteLine($"Received a '{messageText}' message in chat {chatId}");

            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"You said: \n{messageText}",
            cancellationToken: cancellationToken);

            if (message.Text == "Категории")
            {
                HttpClient client = new HttpClient();
                var result = await client.GetAsync("https://localhost:44356/api/Category");
                var test = await result.Content.ReadAsStringAsync();
                Category[] categories = JsonConvert.DeserializeObject<Category[]>(test);
                InlineKeyboardButton[][] keyboardButtons = new InlineKeyboardButton[categories.Length][];
                for (int i = 0; i < categories.Length; i++)
                {
                    keyboardButtons[i] = new InlineKeyboardButton[] 
                    { InlineKeyboardButton.WithCallbackData(categories[i].CategotyName, callbackData: categories[i].CategotyName)};
                }   

                var inlineKeyboard = new InlineKeyboardMarkup(keyboardButtons);
                await botClient.SendTextMessageAsync(message.Chat.Id, "Нажмите на кнопку:", replyMarkup: inlineKeyboard);
            }

            if (message.Text == "Девушка, когда лабораторные?")
            {
                await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Я всё сдам!",
                cancellationToken: cancellationToken);
            }
            ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
{
    new KeyboardButton[] { "Картинка", "Стикер", "Видео" },
    new KeyboardButton[] { "Категории" },
})
            {
                ResizeKeyboard = true
            };
            if (message.Text == "Привет")
            {
                await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Несмотря на то, что ты скорее всего не Олег, я приветствую тебя!",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
            }
            if (message.Text == "Картинка")
            {
                await botClient.SendPhotoAsync(
    chatId: chatId,
    photo: "https://raw.githubusercontent.com/Accfora/Task8/main/wAS.jpg",
    caption: "<b>Это кот. Ему немного стыдно за тебя, но он этого не покажет</b>",
    parseMode: ParseMode.Html,
    cancellationToken: cancellationToken);
            }
            if (message.Text == "Стикер")
            {
                await botClient.SendStickerAsync(
    chatId: chatId,
    sticker: "https://raw.githubusercontent.com/Accfora/Task8/main/thumb128.webp",
    cancellationToken: cancellationToken);
            }
            if (message.Text == "Видео")
            {
                await botClient.SendVideoAsync(
    chatId: chatId,
    video: "https://raw.githubusercontent.com/Accfora/Task8/main/SelenaKilled.mp4",
    supportsStreaming: true,
    cancellationToken: cancellationToken);
            }
        }
        static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
            CancellationToken cancellationToken)
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