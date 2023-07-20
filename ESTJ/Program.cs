using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Configuration;
using static System.Net.Mime.MediaTypeNames;
using System.Security.Cryptography;
using Newtonsoft.Json.Bson;
using System.ComponentModel;

namespace ESTJ
{
    internal class Program
    {
        static SqlConnection cnn;
        static async Task Main(string[] args)
        {
            string connectionString = @"Data Source=DESKTOP-OKU7E5D;Initial Catalog=ESTJ;Integrated Security=True";
            cnn = new SqlConnection(connectionString);
            cnn.Open();

            var bot = new TelegramBotClient("Token was here :)");
            using CancellationTokenSource cts = new CancellationTokenSource();
            ReceiverOptions receiverOptions = new ReceiverOptions()
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            bot.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
                );
            var me = await bot.GetMeAsync();

            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();
            cts.Cancel();
        }
        static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update,
    CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type == UpdateType.CallbackQuery)
                {
                    await HandleCallbackQuery(bot, update.CallbackQuery);
                    return;
                }
                if (update.Message is not { } message)
                    return;
                if (message.Text is not { } messageText)
                    return;

                var chatId = message.Chat.Id;

                Console.WriteLine($"Received a '{messageText}' message in chat {chatId}");
                SqlCommand cmd = cnn.CreateCommand();
                cmd.CommandText = @$"select Nickname from Guest where id_chat = {chatId}";
                //SqlDataReader sdtrd = await cmd.ExecuteReaderAsync(cancellationToken);

                var chatNickname = await cmd.ExecuteScalarAsync(cancellationToken);

                if ((chatNickname is null || chatNickname.ToString() == "")
                    && (messageText != "/start" && !messageText.StartsWith("/reg ")))
                {
                    await bot.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"Необходимо авторизироваться /reg для дальнейших действий",
                    cancellationToken: cancellationToken);

                    Console.WriteLine($"{chatId} attempted to avoid registration");

                    return;
                }

                ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
                {
                new KeyboardButton[] { "Быки и коровы" }
            })
                {
                    ResizeKeyboard = true
                };

                ReplyKeyboardMarkup hash = new(new[]
    {
                new KeyboardButton[] { "#" }
            })
                {
                    ResizeKeyboard = true
                };

                //        cmd.CommandText = $@"select id_chat from Guest";
                //        SqlDataReader sdr = await cmd.ExecuteReaderAsync();

                //        while (await sdr.ReadAsync())
                //        {
                //            await bot.SendTextMessageAsync(
                //            chatId: (int)sdr[0],
                //text: $"Обновление настроек",
                //replyMarkup: replyKeyboardMarkup,
                //cancellationToken: cancellationToken);
                //        }
                //        await sdr.CloseAsync();

                if (messageText == "/start")
                {
                    if (chatNickname is not null && chatNickname.ToString() != "")
                    {
                        await bot.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"Нет-нет, {chatNickname.ToString()}, мы уже знакомы)",
                        cancellationToken: cancellationToken);

                        Console.WriteLine($"{chatId} attempted to start");

                        return;
                    }
                    else
                    {
                        cmd.CommandText = @$"select Id_chat from Guest where Id_chat = {chatId}";
                        var id = await cmd.ExecuteScalarAsync(cancellationToken);

                        await bot.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Привет! Тебя приветствует ESTJ-бот! Введи /reg и свой ник-нейм:",
                        cancellationToken: cancellationToken);

                        if (id is null)
                        {
                            cmd.CommandText = $@"insert Guest values ({chatId},null,null,default)";
                            await cmd.ExecuteNonQueryAsync(cancellationToken);
                        }

                        cmd.CommandText = $@"select id_chat from Guest where admin = 1";
                        SqlDataReader dr = await cmd.ExecuteReaderAsync(cancellationToken);
                        while (await dr.ReadAsync(cancellationToken))
                        {
                            await bot.SendTextMessageAsync(
                            chatId: Convert.ToInt32(dr[0]),
                            text: $"{chatId} has begun register process",
                            cancellationToken: cancellationToken);
                        }
                        await dr.CloseAsync();
                        Console.WriteLine($"{chatId} has begun register process");

                        return;
                    }
                }
                else if (messageText.StartsWith("/reg "))
                {
                    if (chatNickname is not null && chatNickname.ToString() != "")
                    {
                        await bot.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"Нет-нет, {chatNickname.ToString()}, ник менять нельзя. Все вопросы к администраторам)",
                        cancellationToken: cancellationToken);

                        Console.WriteLine($"{chatId} attempted to register again");

                        return;
                    }

                    var txt = messageText.Substring(5);

                    if (txt.Length < 5)
                    {
                        await bot.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Ну никнейм меньше 5 символов - это как-то несерьёзно. Давай заново)",
                        cancellationToken: cancellationToken);

                        Console.WriteLine($"{chatId} failed to register as {txt}");

                        return;
                    }

                    cmd.CommandText = @$"select Nickname from Guest where Nickname = '{txt}'";
                    var anyes = await cmd.ExecuteScalarAsync(cancellationToken);

                    if (anyes is null || anyes.ToString() == "")
                    {
                        try
                        {
                            cmd.CommandText = @$"update Guest set Nickname = '{txt}' where Id_chat = {chatId}";
                            await cmd.ExecuteNonQueryAsync(cancellationToken);

                            await bot.SendTextMessageAsync(
                            chatId: chatId,
                            text: $"Поздравляю с успешной регистрацией, {txt}",
                            replyMarkup: replyKeyboardMarkup,
                            cancellationToken: cancellationToken);

                            cmd.CommandText = $@"select id_chat from Guest where admin = 1";
                            SqlDataReader drccc = await cmd.ExecuteReaderAsync(cancellationToken);
                            while (await drccc.ReadAsync(cancellationToken))
                            {
                                await bot.SendTextMessageAsync(
                                chatId: Convert.ToInt32(drccc[0]),
                                text: $"{chatId} registered as {txt}",
                                cancellationToken: cancellationToken);
                            }
                            await drccc.CloseAsync();
                            Console.WriteLine($"{chatId} registered as {txt}");
                        }
                        catch
                        {
                            await bot.SendTextMessageAsync(
                            chatId: chatId,
                            text: $"Честно говоря, не знаю, что пошло нет так. Давай заново :о",
                            cancellationToken: cancellationToken);

                            Console.WriteLine($"{chatId} failed to register as {txt}");
                        }
                        return;
                    }
                    else
                    {
                        await bot.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Этот никнейм уже занят! Давай заново :с",
                        cancellationToken: cancellationToken);

                        Console.WriteLine($"{chatId} failed to register as {txt}");

                        return;
                    }
                }

                if (messageText == "#" && wait.Contains(chatId) | waitExplode.Contains(chatId))
                {
                    await bot.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"Вы покинули зал ожидания",
                    replyMarkup: replyKeyboardMarkup,
                    cancellationToken: cancellationToken);

                    wait.Remove(chatId);
                }

                foreach (ExplodeGame exp in processGames.Where(g => g is ExplodeGame).Select(g => (ExplodeGame)g))
                    if (exp.IsHere(chatId))
                    {
                        string imNick = exp.GetNick(chatId);

                        if (messageText == "#")
                        {
                            var _1 = exp.players.Where(f => f.guestId != chatId).First().guestId;
                            var _2 = exp.players.Where(f => f.guestId != chatId).Last().guestId;

                            await bot.SendTextMessageAsync(
                                    chatId: _1,
                                    text: $"{chatNickname} сдался. Ничья между {exp.GetNick(_1)} и {exp.GetNick(_2)}",
                                    replyMarkup: replyKeyboardMarkup,
                                    cancellationToken: cancellationToken);
                            await bot.SendTextMessageAsync(
                                    chatId: _2,
                                    text: $"{chatNickname} сдался. Ничья между {exp.GetNick(_1)} и {exp.GetNick(_2)}",
                                    replyMarkup: replyKeyboardMarkup,
                                    cancellationToken: cancellationToken);
                            await bot.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: $"Вы сдались",
                                    replyMarkup: replyKeyboardMarkup,
                                    cancellationToken: cancellationToken);

                            processGames.Remove(exp);

                            return;
                        }
                    }

                foreach (BykiKoroviGame bik in processGames.Where(g => g is BykiKoroviGame).Select(g => (BykiKoroviGame)g))
                    if (bik.IsHere(chatId))
                    {
                        BykiKoroviPlayer im = (bik.p1.guestId == chatId) ? bik.p1 : bik.p2;
                        BykiKoroviPlayer opponent = (bik.p1.guestId == chatId) ? bik.p2 : bik.p1;
                        cmd.CommandText = @$"select Nickname from Guest where id_chat = {opponent.guestId}";
                        var opponentNick = await cmd.ExecuteScalarAsync(cancellationToken);

                        if (messageText == "#")
                        {
                            await bot.SendTextMessageAsync(
                                    chatId: opponent.guestId,
                                    text: $"{chatNickname} сдался",
                                    replyMarkup: replyKeyboardMarkup,
                                    cancellationToken: cancellationToken);
                            await bot.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: $"Вы сдались",
                                    replyMarkup: replyKeyboardMarkup,
                                    cancellationToken: cancellationToken);

                            processGames.Remove(bik);

                            return;
                        }

                        if (im.pick.Count == 0)
                        {

                            if (bik.DueFormat(messageText))
                            {
                                im.FillPick(messageText);

                                await bot.SendTextMessageAsync(
                                chatId: chatId,
                                text: $"Элки записаны",
                                cancellationToken: cancellationToken);

                                if (opponent.pick.Count == 0)
                                {
                                    await bot.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: $"Ожидание оппонента",
                                    cancellationToken: cancellationToken);

                                    return;
                                }
                                else
                                {
                                    await bot.SendTextMessageAsync(
                                        chatId: chatId,
                                        text: $"{chatNickname} vs {opponentNick}! И пусть удача всегда будет с вами",
                                        cancellationToken: cancellationToken);
                                    await bot.SendTextMessageAsync(
                                        chatId: opponent.guestId,
                                        text: $"{opponentNick} vs {chatNickname}! И пусть удача всегда будет с вами",
                                        cancellationToken: cancellationToken);
                                    return;
                                }
                            }
                            else
                            {
                                await bot.SendTextMessageAsync(
                                chatId: chatId,
                                text: $"Формат некорректен",
                                cancellationToken: cancellationToken);

                                return;
                            }
                        }

                        if (opponent.lastbyki == -1 && im.lastbyki != -1)
                        {
                            await bot.SendTextMessageAsync(
                                chatId: chatId,
                                text: $"Ожидание хода оппонента, имейте терпение",
                                cancellationToken: cancellationToken);

                            return;
                        }

                        if (bik.DueFormat(messageText))
                        {
                            Tuple<int, int> tuple = opponent.Verify(messageText);

                            await bot.SendTextMessageAsync(
                                chatId: chatId,
                                text: $"{tuple.Item1}б | {tuple.Item2}к",
                                cancellationToken: cancellationToken);

                            im.lastbyki = tuple.Item1;
                            im.lastkorovi = tuple.Item2;

                            if (opponent.lastbyki == -1)
                            {
                                await bot.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: $"Ожидание хода оппонента",
                                    cancellationToken: cancellationToken);
                                await bot.SendTextMessageAsync(
                                    chatId: opponent.guestId,
                                    text: $"Оппонент вас ждёт",
                                    cancellationToken: cancellationToken);

                                return;
                            }
                        }
                        else
                        {
                            await bot.SendTextMessageAsync(
                                chatId: chatId,
                                text: $"Формат некорректен",
                                cancellationToken: cancellationToken);

                            return;
                        }

                        if (im.lastbyki == bik.picked || opponent.lastbyki == bik.picked)
                        {
                            if (im.lastbyki == opponent.lastbyki)
                            {
                                await bot.SendTextMessageAsync(
                                        chatId: chatId,
                                        text: $"Удивительно, но у вас с {opponentNick} ничья!",
                                        replyMarkup: replyKeyboardMarkup,
                                        cancellationToken: cancellationToken);
                                await bot.SendTextMessageAsync(
                                        chatId: opponent.guestId,
                                        text: $"Удивительно, но у вас с {chatNickname} ничья!",
                                        replyMarkup: replyKeyboardMarkup,
                                        cancellationToken: cancellationToken);
                            }
                            else
                            {
                                await bot.SendTextMessageAsync(
                                        chatId: im.lastbyki == bik.picked ? chatId : opponent.guestId,
                                        text: $"Поздравляю, вы одержали победу над " +
                                        $"{(im.lastbyki == bik.picked ? opponentNick : chatNickname)}!",
                                        replyMarkup: replyKeyboardMarkup,
                                        cancellationToken: cancellationToken);
                                await bot.SendTextMessageAsync(
                                        chatId: im.lastbyki == bik.picked ? opponent.guestId : chatId,
                                        text: $"Сожалею, но " +
                                        $"{(im.lastbyki == bik.picked ? chatNickname : opponentNick)} вас одолел.\n" +
                                        $"{(im.lastbyki == bik.picked ? chatNickname : opponentNick)} загадал:\n\n" +
                                        $"{String.Join('\n', im.lastbyki == bik.picked ? im.pick : opponent.pick)}",
                                        replyMarkup: replyKeyboardMarkup,
                                        cancellationToken: cancellationToken);
                            }

                            processGames.Remove(bik);
                        }
                        else
                        {
                            await bot.SendTextMessageAsync(
                                chatId: chatId,
                                text: $"Можете приступать к следующей попытке. У {opponentNick} " +
                                $"было {opponent.lastbyki}б | {opponent.lastkorovi}к",
                                cancellationToken: cancellationToken);
                            await bot.SendTextMessageAsync(
                                chatId: opponent.guestId,
                                text: $"Можете приступать к следующей попытке. У {chatNickname} " +
                                $"было {im.lastbyki}б | {im.lastkorovi}к",
                                cancellationToken: cancellationToken);

                            im.lastbyki = -1;
                            im.lastkorovi = -1;
                            opponent.lastbyki = -1;
                            opponent.lastkorovi = -1;
                        }

                        break;
                    }

                if (messageText == "Explode")
                {
                    if (!waitExplode.Contains(chatId))
                        waitExplode.Add(chatId);

                    if (waitExplode.Count > 2)
                    {
                        SqlConnection cn;
                        string connectionString = @"Data Source=DESKTOP-OKU7E5D;Initial Catalog=Anshostos;Integrated Security=True";
                        cn = new SqlConnection(connectionString);
                        cn.Open();

                        SqlCommand cm = cn.CreateCommand();
                        cm.CommandText = @"select top 20 Название_станции from Станции order by dbo.getRandom(1,100)";

                        SqlDataReader lsrjg = await cm.ExecuteReaderAsync(cancellationToken);
                        List<string> els = new List<string>();
                        while (await lsrjg.ReadAsync(cancellationToken))
                        {
                            els.Add(lsrjg[0].ToString());
                        }

                        List<Player> pls = new List<Player> { new Player(waitExplode[0]), new Player(waitExplode[1]), new Player(waitExplode[2]) };
                        processGames.Add(new ExplodeGame(pls));

                        //InlineKeyboardButton[][] keyboardButtons = new InlineKeyboardButton[categories.Length][];
                        //for (int i = 0; i < categories.Length; i++)
                        //{
                        //    keyboardButtons[i] = new InlineKeyboardButton[]
                        //    { InlineKeyboardButton.WithCallbackData(categories[i].CategotyName, callbackData: categories[i].CategotyName)};
                        //}

                        //var inlineKeyboard = new InlineKeyboardMarkup(keyboardButtons);

                        await bot.SendTextMessageAsync(
                            chatId: chatId,
                            text: $"Игра началась. В игре: {String.Join(',', pls.Select(n => n.guestNick))}",
                            replyMarkup: new ReplyKeyboardRemove(),
                            cancellationToken: cancellationToken);
                        await bot.SendTextMessageAsync(
                            chatId: chatId,
                            text: $"Игра началась. В игре: {String.Join(',', pls.Select(n => n.guestNick))}",
                            replyMarkup: new ReplyKeyboardRemove(),
                            cancellationToken: cancellationToken);
                        await bot.SendTextMessageAsync(
                            chatId: chatId,
                            text: $"Игра началась. В игре: {String.Join(',', pls.Select(n => n.guestNick))}",
                            replyMarkup: new ReplyKeyboardRemove(),
                            cancellationToken: cancellationToken);
                    }
                }

                if (messageText == "Быки и коровы")
                {
                    if (!wait.Contains(chatId))
                        wait.Add(chatId);

                    if (wait.Count > 1)
                    {
                        SqlConnection cn;
                        string connectionString = @"Data Source=DESKTOP-OKU7E5D;Initial Catalog=Anshostos;Integrated Security=True";
                        cn = new SqlConnection(connectionString);
                        cn.Open();

                        //SqlCommand cm = cn.CreateCommand();
                        //cm.CommandText = @"select top 13 Название_станции from Станции order by dbo.getRandom(1,100)";

                        //SqlDataReader lsrjg = await cm.ExecuteReaderAsync(cancellationToken);
                        //List<string> els = new List<string>();
                        //while (await lsrjg.ReadAsync(cancellationToken))
                        //{
                        //    els.Add(lsrjg[0].ToString());
                        //}

                        List<string> els = new List<string> { "50%", "Майонез Олейна", "Паника",
                            "Подошва", "Обыкновенный бегемот", "Двусторонний отит", "Крысиный яд", "Чебуречная", "Мы открылись!", 
                            "Восточный Тимор", "Fuck", "Ахахах", "Блудный сын"};

                        BykiKoroviPlayer bkp0 = new BykiKoroviPlayer(wait[0]);
                        BykiKoroviPlayer bkp1 = new BykiKoroviPlayer(wait[1]);

                        processGames.Add(
                        new BykiKoroviGame(
                            bkp0, bkp1,
                                                //                    new List<string> { "Москворечье", "Курсаковская", "Дубосеково",
                                                //"Печатники", "Стрешнево", "Перерва", "Львовская", "Чехов", "Покровское", "Столбовая",
                                                //                    "Шарапова Охота", "Курьяново", "Граждансая"}
                            els, 5
                            ));

                        bool imatzero = chatId == wait[0];
                        await bot.SendTextMessageAsync(
                                chatId: chatId,
                                text: $"Игра началась. Ваш противник - {(imatzero ? bkp1.guestNick : bkp0.guestNick)}",
                                replyMarkup: new ReplyKeyboardRemove(),
                                cancellationToken: cancellationToken);
                        await bot.SendTextMessageAsync(
                                chatId: imatzero ? wait[1] : wait[0],
                                text: $"Игра началась. Ваш противник - {bkp0.guestNick}",
                                replyMarkup: new ReplyKeyboardRemove(),
                                cancellationToken: cancellationToken);
                        await bot.SendTextMessageAsync(
                       chatId: chatId,
                       text: $"Список электричек:\n\n{String.Join('\n', els)}",
                       cancellationToken: cancellationToken);
                        await bot.SendTextMessageAsync(
                                chatId: imatzero ? wait[1] : wait[0],
                                text: $"Список электричек:\n\n{String.Join('\n', els)}",
                                cancellationToken: cancellationToken);

                        wait.RemoveAt(0); wait.RemoveAt(0);
                    }
                    else
                    {
                        await bot.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"Ожидание оппонента",
                        replyMarkup: hash,
                        cancellationToken: cancellationToken);
                    }
                }
            }
            catch { }
        }
        static List<long> wait = new List<long>();
        static List<IGame> processGames = new List<IGame>();
        static List<long> waitExplode = new List<long>();
        public class ExplodeGame : IGame
        {
            public List<Player> players = new List<Player>();
            public ExplodeGame (List<Player> players)
            {
                this.players = players;
            }
            public bool IsHere(long pl)
            {
                return players.Select(id => id.guestId).Contains(pl);
            }
            public string GetNick(long id)
            {
                return players.Where(i => i.guestId == id).Select(p => p.guestNick).First();
            }
        }
        public class Player
        {
            public long guestId { get; protected set; }
            public string guestNick { get; protected set; }
            public Player(long guestId)
            {
                SqlCommand sql = cnn.CreateCommand();
                sql.CommandText = @$"select Nickname from Guest where Id_chat = {guestId}";

                this.guestId = guestId;
                this.guestNick = sql.ExecuteScalar().ToString();
            }
        }
        public class BykiKoroviPlayer : Player
        {
            public List<string> pick { get; private set; }
            public int lastbyki = -1;
            public int lastkorovi = -1;
            public BykiKoroviPlayer(long guestId) : base (guestId)
            {
                pick = new List<string>();
            }
            public void FillPick(string text)
            {
                pick = text.Split('\n').Select(el => el.Trim()).ToList();
            }
            public Tuple<int,int> Verify(string text)
            {
                var attempt = text.Split('\n').Select(el => el.Trim()).ToList();

                int byki = 0, korovi = 0;
                for (int i = 0; i < pick.Count; i++)
                    if (attempt.IndexOf(pick[i]) == i)
                        byki++;
                    else if (attempt.IndexOf(pick[i]) != -1)
                        korovi++;
                return Tuple.Create(byki, korovi);
            }
        }
        public interface IGame
        {
            public bool IsHere(long d);
        }
        public class BykiKoroviGame : IGame
        {
            public BykiKoroviPlayer p1 { get; }
            public BykiKoroviPlayer p2 { get; }
            public List<string> values { get; }
            public int picked { get; }
            public BykiKoroviGame(BykiKoroviPlayer p1, BykiKoroviPlayer p2, List<string> values, int picked)
            {
                this.p1 = p1;
                this.p2 = p2;
                this.values = values;
                this.picked = picked;
            }
            public bool IsHere(long pl)
            {
                if (pl == p1.guestId || pl == p2.guestId)
                    return true;
                return false;
            }
            public bool DueFormat(string text)
            {
                var att = text.Split('\n').Select(el => el.Trim()).ToList();
                return att.Count == picked 
                    && att.Where(pic => !values.Contains(pic.Trim())).Count() == 0
                    && att.Distinct().Count() == att.Count();
            }

        }
        static async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {

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