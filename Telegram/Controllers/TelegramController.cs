using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Application.Services;
using Domain.Sessions;
using Application.Dtos;

namespace Telegram.Controllers
{
    public class TelegramController
    {
        private readonly TelegramBotClient _botClient;
        private readonly UserService _userService;
        private readonly CatFactService _catFactService; // Ini deklarasi field
        private readonly JokeService _jokeService; 
        private readonly ReportService _reportService;
        private static readonly Dictionary<long, UserCreationSession> UserSessions = new();

        // Perbaiki konstruktor di sini: tambahkan CatFactService sebagai parameter
        public TelegramController(TelegramBotClient botClient, UserService userService, CatFactService catFactService, JokeService jokeService, ReportService reportService)
        {
            _botClient = botClient;
            _userService = userService;
            _catFactService = catFactService; // Sekarang 'catFactService' berasal dari parameter konstruktor
            _jokeService = jokeService; // Inisialisasi JokeService
            _reportService = reportService;
        }

        public async Task HandleUpdateAsync(Update update)
        {
            if (update.Type != UpdateType.Message || update.Message?.Text is not { } messageText)
                return;

            var chatId = update.Message.Chat.Id;

            // 🔄 Proses interaktif jika sedang input user
            if (UserSessions.TryGetValue(chatId, out var session))
            {
                switch (session.Step)
                {
                    case 1:
                        if (string.IsNullOrWhiteSpace(messageText))
                        {
                            await _botClient.SendMessage(chatId, "❌ Nama tidak boleh kosong. Masukkan nama:");
                            return;
                        }

                        session.Name = messageText;
                        session.Step = 2;
                        await _botClient.SendMessage(chatId, "📆 Berapa umur user?");
                        return;

                    case 2:
                        if (!int.TryParse(messageText, out var age))
                        {
                            await _botClient.SendMessage(chatId, "❌ Umur harus berupa angka. Coba lagi:");
                            return;
                        }

                        session.Age = age;
                        session.Step = 3;
                        await _botClient.SendMessage(chatId, "🎯 Apa hobi user?");
                        return;

                    case 3:
                        session.Hobby = messageText;

                        var userDto = new UserDto
                        {
                            Name = session.Name!,
                            Age = session.Age ?? 0,
                            Hobby = session.Hobby!
                        };

                        try
                        {
                            await _userService.CreateUserAsync(userDto);
                            await _botClient.SendMessage(chatId, $"✅ User *{userDto.Name}* berhasil ditambahkan ke database!", parseMode: ParseMode.Markdown);
                            Console.WriteLine($"[LOG] User '{userDto.Name}' berhasil disimpan.");
                        }
                        catch (Exception ex)
                        {
                            await _botClient.SendMessage(chatId, $"❌ Gagal menyimpan user: {ex.Message}");
                            Console.WriteLine($"[ERROR] Gagal menyimpan user: {ex.Message}");
                        }

                        UserSessions.Remove(chatId);
                        return;
                }
            }


            // 🔘 Command untuk memulai tambah user
            if (messageText.StartsWith("/add user"))
            {
                UserSessions[chatId] = new UserCreationSession { Step = 1 };
                await _botClient.SendMessage(chatId, "👤 Siapa nama user?");
                return;
            }

            // 🔍 Info user
            if (messageText.StartsWith("/info user"))
            {
                var parts = messageText.Split(' ');
                if (parts.Length < 3)
                {
                    await _botClient.SendMessage(chatId, "❌ Format salah. Gunakan: /info user <username>");
                    return;
                }

                var username = parts[2].ToLower();
                var userDto = await _userService.GetUserInfoAsync(username);

                if (userDto == null)
                {
                    await _botClient.SendMessage(chatId, $"❌ User \"{username}\" tidak ditemukan.");
                }
                else
                {
                    await _botClient.SendMessage(chatId,
                        $"📄 *Informasi User*\n\n👤 Nama: {userDto.Name}\n🎂 Umur: {userDto.Age} Tahun\n🎯 Hobi: {userDto.Hobby}",
                        parseMode: ParseMode.Markdown);
                }

                return;
            }

            if (messageText.StartsWith("/catfact"))
            {
                await _botClient.SendChatAction(chatId, ChatAction.Typing); // Menunjukkan bot sedang mengetik

                var fact = await _catFactService.GetRandomCatFactAsync();

                if (!string.IsNullOrEmpty(fact))
                {
                    await _botClient.SendMessage(chatId, $"🐱 Fakta Kucing: {fact}");
                }
                else
                {
                    await _botClient.SendMessage(chatId, "❌ Maaf, tidak dapat mengambil fakta kucing saat ini. Coba lagi nanti!");
                }
                return;
            }
            
            if (messageText.StartsWith("/joke"))
            {
                await _botClient.SendChatAction(chatId, ChatAction.Typing); // Menggunakan SendChatAction (tanpa Async)
                var joke = await _jokeService.GetRandomJokeAsync();

                if (!string.IsNullOrEmpty(joke))
                {
                    await _botClient.SendMessage(chatId, $"😂 Joke untukmu:\n\n{joke}"); // Menggunakan SendMessage (tanpa Async)
                }
                else
                {
                    await _botClient.SendMessage(chatId, "❌ Maaf, tidak dapat mengambil joke saat ini. Coba lagi nanti!"); // Menggunakan SendMessage (tanpa Async)
                }
                return;
            }

             if (messageText.StartsWith("/report_file user"))
            {
                await _botClient.SendChatAction(chatId, ChatAction.UploadDocument); // Menunjukkan bot sedang mengunggah dokumen

                try
                {
                    var (fileStream, fileName) = await _reportService.GenerateUserReportAsync();

                    // Kirim file ke Telegram
                    await _botClient.SendDocument(
                        chatId: chatId,
                        document: new InputFileStream(fileStream, fileName), // Gunakan InputFileStream
                        caption: "📊 Berikut adalah laporan data user dalam format Excel."
                    );

                    // Penting: Pastikan stream ditutup setelah digunakan
                    await fileStream.DisposeAsync();
                    Console.WriteLine($"[LOG] Laporan '{fileName}' berhasil dikirim ke chat ID: {chatId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Gagal membuat atau mengirim laporan: {ex.Message}");
                    await _botClient.SendMessage(chatId, "❌ Maaf, gagal membuat atau mengirim laporan saat ini.");
                }
                return;
            }


            // 🚀 Perintah /start
            if (messageText.StartsWith("/start"))
            {
                string welcomeMessage = "👋 Selamat datang!\n\nPerintah:\n" +
                                        "`/info user <username>` → Lihat info user\n" +
                                        "`/add user` → Tambah user baru\n" +
                                        "`/catfact` → Dapatkan fakta kucing acak\n" +
                                        "`/joke` → Dapatkan joke acak\n" + // <-- Tambahkan baris ini
                                        "`/report_file user` → Dapatkan laporan user dalam Excel";
                await _botClient.SendMessage(chatId, welcomeMessage, parseMode: ParseMode.Markdown); // Menggunakan SendMessage (tanpa Async)
                return;
            }
        }
    }
}