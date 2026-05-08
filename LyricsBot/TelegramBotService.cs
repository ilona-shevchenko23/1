using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums; // Нова бібліотека для типів оновлень (повідомлення чи кнопка)
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Hosting;

namespace SpotifyLyricsBot
{
    class UserSession
    {
        public int Step { get; set; } = 0;
        public string Language { get; set; } = "";
        public string Artist { get; set; } = "";
        public string Title { get; set; } = "";
        public string LastSearchedArtist { get; set; } = ""; // Зберігаємо виконавця для Топ-5
    }

    public class TelegramBotService : BackgroundService
    {
        private readonly string BotToken = "8761052351:AAGnHNtTsTiZkbeITd5HhhxzFDfhL2S-xBk";
        private readonly HttpClient client = new HttpClient();
        private Dictionary<long, UserSession> sessions = new Dictionary<long, UserSession>();
        private readonly string SessionsFile = "sessions.json";
        private readonly DatabaseService _db;

        private readonly string[] KnownLanguages = {
            "англійська", "українська", "російська", "французька", "німецька", "іспанська",
            "польська", "італійська", "японська", "корейська", "китайська", "турецька"
        };

        public TelegramBotService(DatabaseService db)
        {
            _db = db;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await LoadSessionsAsync();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            var botClient = new TelegramBotClient(BotToken);

            botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, null, stoppingToken);
            Console.WriteLine("Telegram-бот успішно запущений");

            await Task.Delay(-1, stoppingToken);
        }

        // --- РОБОТА З СЕСІЯМИ ---
        private async Task LoadSessionsAsync()
        {
            if (File.Exists(SessionsFile))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(SessionsFile);
                    sessions = JsonSerializer.Deserialize<Dictionary<long, UserSession>>(json) ?? new Dictionary<long, UserSession>();
                }
                catch { sessions = new Dictionary<long, UserSession>(); }
            }
        }

        private async Task SaveSessionsAsync()
        {
            try
            {
                string json = JsonSerializer.Serialize(sessions);
                await File.WriteAllTextAsync(SessionsFile, json);
            }
            catch { }
        }

        // --- КЛАВІАТУРИ ---
        private ReplyKeyboardMarkup GetMainMenu()
        {
            return new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { "Шукати текст пісні" } })
            {
                ResizeKeyboard = true
            };
        }

        // Створюємо Inline-кнопку Топ-5
        private InlineKeyboardMarkup GetTop5Button()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new [] { InlineKeyboardButton.WithCallbackData("🔝 Топ 5 пісень цього виконавця", "top5") }
            });
        }

        // --- ГОЛОВНИЙ ОБРОБНИК ---
        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            long chatId = 0;

            // Визначаємо, звідки прийшов запит: текст чи натискання Inline-кнопки
            if (update.Type == UpdateType.Message && update.Message?.Text != null)
                chatId = update.Message.Chat.Id;
            else if (update.Type == UpdateType.CallbackQuery)
                chatId = update.CallbackQuery.Message.Chat.Id;
            else return;

            if (!sessions.ContainsKey(chatId)) sessions[chatId] = new UserSession();
            var session = sessions[chatId];

            // 1. ОБРОБКА НАТИСКАННЯ INLINE-КНОПОК
            if (update.Type == UpdateType.CallbackQuery)
            {
                string callbackData = update.CallbackQuery.Data;
                await bot.AnswerCallbackQuery(update.CallbackQuery.Id); // Зупиняємо "крутилку" на кнопці

                if (callbackData == "top5")
                {
                    if (string.IsNullOrEmpty(session.LastSearchedArtist)) return;

                    await bot.SendMessage(chatId, $"Шукаю топ пісень для **{session.LastSearchedArtist}**...");
                    var topSongs = await GetTop5SongsAsync(session.LastSearchedArtist);

                    if (topSongs.Count == 0)
                    {
                        await bot.SendMessage(chatId, "На жаль, не вдалося знайти інші пісні.");
                    }
                    else
                    {
                        var buttons = new List<InlineKeyboardButton[]>();
                        foreach (var song in topSongs)
                        {
                            // Обрізаємо назву, якщо вона дуже довга (ліміт Telegram)
                            string safeSong = song.Length > 40 ? song.Substring(0, 40) : song;
                            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("🎵 " + song, "song:" + safeSong) });
                        }
                        await bot.SendMessage(chatId, $"Ось популярні пісні виконавця {session.LastSearchedArtist}:", replyMarkup: new InlineKeyboardMarkup(buttons));
                    }
                }
                else if (callbackData.StartsWith("song:"))
                {
                    // Якщо натиснули на конкретну пісню з Топ-5
                    string songTitle = callbackData.Substring(5);
                    session.Artist = session.LastSearchedArtist;
                    session.Title = songTitle;
                    await bot.SendMessage(chatId, $"Шукаю текст: {session.Artist} - {session.Title}...");
                    await SearchAndSendLyricsAsync(bot, chatId, session);
                }
                await SaveSessionsAsync();
                return;
            }

            // 2. ОБРОБКА ЗВИЧАЙНИХ ПОВІДОМЛЕНЬ
            string text = update.Message.Text.Trim();

            if (text == "/start")
            {
                session.Step = 0;
                await SaveSessionsAsync();
                await bot.SendMessage(chatId, "Привіт! Натисни кнопку нижче, щоб розпочати пошук.", replyMarkup: GetMainMenu());
                return;
            }

            if (text == "Шукати текст пісні")
            {
                session.Step = 1;
                session.Language = ""; session.Artist = ""; session.Title = "";
                await SaveSessionsAsync();
                await bot.SendMessage(chatId, "Якої мови пісня?", replyMarkup: GetMainMenu());
                return;
            }

            if (session.Step == 0)
            {
                await bot.SendMessage(chatId, "Будь ласка, натисніть кнопку «Шукати текст пісні», щоб почати.", replyMarkup: GetMainMenu());
            }
            else if (session.Step == 1)
            {
                string corrected = CorrectLanguageSpelling(text);
                if (corrected == null)
                {
                    await bot.SendMessage(chatId, "Я не знаю такої мови. Напиши ще раз:", replyMarkup: GetMainMenu());
                    return;
                }
                session.Language = corrected; session.Step = 2;
                await bot.SendMessage(chatId, $"Мова: {corrected}. Хто виконавець?", replyMarkup: GetMainMenu());
            }
            else if (session.Step == 2)
            {
                session.Artist = text; session.Step = 3;
                await bot.SendMessage(chatId, $"Виконавець: {text}. Яка назва пісні?", replyMarkup: GetMainMenu());
            }
            else if (session.Step == 3)
            {
                session.Title = text;
                await bot.SendMessage(chatId, "Аналізую запит...", replyMarkup: GetMainMenu());
                await SearchAndSendLyricsAsync(bot, chatId, session); // Викликаємо наш новий спільний метод
            }

            await SaveSessionsAsync();
        }

        // --- МЕТОД ПОШУКУ ТА ВІДПРАВКИ ТЕКСТУ ---
        private async Task SearchAndSendLyricsAsync(ITelegramBotClient bot, long chatId, UserSession session)
        {
            var cachedSong = await _db.GetSongAsync(session.Artist, session.Title);

            if (cachedSong != null)
            {
                session.LastSearchedArtist = cachedSong.Value.Artist; // Запам'ятовуємо точне ім'я
                await bot.SendMessage(chatId,
                    $"⚡ **Знайдено в локальній базі!**\n\n" +
                    $"**{cachedSong.Value.Artist} - {cachedSong.Value.Title}**\n" +
                    $"💿 Альбом: **{cachedSong.Value.Album}**\n" +
                    $"Визначена мова: {cachedSong.Value.Language}\n\n" +
                    $"{cachedSong.Value.Lyrics}",
                    replyMarkup: GetTop5Button()); // Чіпляємо кнопку Топ-5

                session.Step = 0;
                await bot.SendMessage(chatId, "Радий був вам допомогти 😊", replyMarkup: GetMainMenu());
            }
            else
            {
                var result = await SuperSmartSearch(session.Artist, session.Title);

                if (result.Lyrics != null)
                {
                    session.LastSearchedArtist = result.Artist; // Запам'ятовуємо точне ім'я
                    string actualLanguage = DetectActualLanguage(result.Lyrics, session.Language);
                    string albumInfo = string.IsNullOrEmpty(result.Album) ? "Невідомий альбом" : result.Album;

                    await _db.SaveSongAsync(result.Artist, result.Title, result.Album, result.Lyrics, actualLanguage);

                    await bot.SendMessage(chatId,
                        $"🌐 **Завантажено з інтернету**\n\n" +
                        $"**{result.Artist} - {result.Title}**\n" +
                        $"💿 Альбом: **{albumInfo}**\n" +
                        $"Визначена мова: {actualLanguage}\n\n" +
                        $"{result.Lyrics}",
                        replyMarkup: GetTop5Button()); // Чіпляємо кнопку Топ-5

                    session.Step = 0;
                    await bot.SendMessage(chatId, "Радий був вам допомогти 😊", replyMarkup: GetMainMenu());
                }
                else
                {
                    session.Step = 2;
                    await bot.SendMessage(chatId, "Нічого не знайшов. Спробуй написати ВИКОНАВЦЯ ще раз:", replyMarkup: GetMainMenu());
                }
            }
        }

        // --- МЕТОД ДЛЯ ПОШУКУ ТОП-5 ---
        private async Task<List<string>> GetTop5SongsAsync(string artist)
        {
            var topSongs = new List<string>();
            try
            {
                string url = $"https://lrclib.net/api/search?q={Uri.EscapeDataString(artist)}";
                var arr = JsonNode.Parse(await client.GetStringAsync(url)) as JsonArray;
                if (arr != null)
                {
                    foreach (var node in arr)
                    {
                        string foundArtist = node["artistName"]?.ToString() ?? "";
                        string foundTitle = node["trackName"]?.ToString() ?? "";

                        // Перевіряємо, чи це дійсно той самий виконавець (за відстанню Левенштейна)
                        if (ComputeLevenshteinDistance(artist.ToLower(), foundArtist.ToLower()) <= 5)
                        {
                            if (!topSongs.Contains(foundTitle)) // Щоб пісні не дублювалися
                            {
                                topSongs.Add(foundTitle);
                                if (topSongs.Count >= 5) break; // Зупиняємося, коли набрали 5 штук
                            }
                        }
                    }
                }
            }
            catch { }
            return topSongs;
        }

        // Допоміжні методи залишаються без змін
        private async Task<(string Artist, string Title, string Album, string Lyrics)> SuperSmartSearch(string artist, string title)
        {
            try
            {
                string url1 = $"https://lrclib.net/api/search?q={Uri.EscapeDataString(artist + " " + title)}";
                var arr1 = JsonNode.Parse(await client.GetStringAsync(url1)) as JsonArray;
                if (arr1 != null && arr1.Count > 0)
                    return (arr1[0]["artistName"]?.ToString(), arr1[0]["trackName"]?.ToString(), arr1[0]["albumName"]?.ToString(), arr1[0]["plainLyrics"]?.ToString());

                string url2 = $"https://lrclib.net/api/search?q={Uri.EscapeDataString(title)}";
                var arr2 = JsonNode.Parse(await client.GetStringAsync(url2)) as JsonArray;
                if (arr2 != null)
                {
                    foreach (var node in arr2)
                    {
                        string foundArtist = node["artistName"]?.ToString() ?? "";
                        if (ComputeLevenshteinDistance(artist.ToLower(), foundArtist.ToLower()) <= 5)
                            return (foundArtist, node["trackName"]?.ToString(), node["albumName"]?.ToString(), node["plainLyrics"]?.ToString());
                    }
                }
            }
            catch { }
            return (null, null, null, null);
        }

        private string CorrectLanguageSpelling(string input) 
        {
            input = input.ToLower().Replace("i", "і").Replace("a", "а");
            string bestMatch = null;
            int minDistance = int.MaxValue;
            foreach (var lang in KnownLanguages)
            {
                int dist = ComputeLevenshteinDistance(input, lang);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestMatch = lang;
                }
            }
            if (minDistance <= 3) return bestMatch;
            return null;
        }

        private int ComputeLevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(t)) return 99;
            int n = s.Length, m = t.Length;
            int[,] d = new int[n + 1, m + 1];
            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }
            for (int i = 1; i <= n; i++)
                for (int j = 1; j <= m; j++)
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + (t[j - 1] == s[i - 1] ? 0 : 1));
            return d[n, m];
        }

        private string DetectActualLanguage(string text, string claimedLanguage)
        {
            if (string.IsNullOrEmpty(text)) return claimedLanguage;

            int latin = text.Count(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == 'é' || c == 'ñ' || c == 'ö');
            int cyrillic = text.Count(c => (c >= 'а' && c <= 'я') || (c >= 'А' && c <= 'Я') || c == 'і' || c == 'ї' || c == 'є' || c == 'ґ');
            int asian = text.Count(c => (c >= 0x4E00 && c <= 0x9FFF) || (c >= 0x3040 && c <= 0x30FF) || (c >= 0xAC00 && c <= 0xD7AF));

            if (asian > 10)
            {
                string[] asianLangs = { "японська", "корейська", "китайська" };
                if (asianLangs.Contains(claimedLanguage)) return claimedLanguage;
                return "азійська (японська/корейська/китайська)";
            }

            if (latin > cyrillic * 2)
            {
                string[] europeanLangs = { "французька", "німецька", "іспанська", "італійська", "польська", "турецька", "португальська", "шведська", "фінська", "нідерландська", "чеська", "румунська", "угорська", "данська", "норвезька", "словацька", "хорватська" };
                if (europeanLangs.Contains(claimedLanguage)) return claimedLanguage;
                return "англійська";
            }

            if (cyrillic > latin * 2)
            {
                string[] cyrillicLangs = { "російська", "білоруська", "болгарська", "сербська" };
                if (cyrillicLangs.Contains(claimedLanguage)) return claimedLanguage;
                return "українська";
            }

            return claimedLanguage;
        }

        private Task HandleErrorAsync(ITelegramBotClient b, Exception e, CancellationToken c) => Task.CompletedTask;
    }
}