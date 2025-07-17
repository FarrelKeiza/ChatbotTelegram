using Microsoft.Extensions.Hosting;
using DotNetEnv;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Application.Services;
using Infrastructure.Repositories;
using Domain.Interfaces;
using Infrastructure;
using Telegram.Controllers;
using System.Net.Http;

Env.Load();

var botToken = Environment.GetEnvironmentVariable("BOT_TOKEN")
               ?? throw new Exception("BOT_TOKEN tidak ditemukan di .env");

var catFactsApiUrl = Environment.GetEnvironmentVariable("CAT_FACTS_API_URL")
                      ?? throw new Exception("CAT_FACTS_API_URL tidak ditemukan di .env");

var jokeApiUrl = Environment.GetEnvironmentVariable("JOKE_API_URL")
                 ?? throw new Exception("JOKE_API_URL tidak ditemukan di .env");

var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
var dbName = Environment.GetEnvironmentVariable("DB_NAME");
var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPass = Environment.GetEnvironmentVariable("DB_PASS");

var connectionString =
    $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass};Ssl Mode=Require;Trust Server Certificate=true";

var host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Database & Services
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<UserService>();
        services.AddScoped<TelegramController>();

        services.AddHttpClient();

        services.AddScoped<CatFactService>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(); // Ambil HttpClient dari factory
            var catFactsApiUrl = Environment.GetEnvironmentVariable("CAT_FACTS_API_URL")
                                 ?? throw new Exception("CAT_FACTS_API_URL tidak ditemukan di .env");
            return new CatFactService(httpClient, catFactsApiUrl);
        });

        services.AddScoped<JokeService>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            return new JokeService(httpClient, jokeApiUrl);
        });

        services.AddScoped<ReportService>();

        // Bot client
        services.AddSingleton<TelegramBotClient>(_ => new TelegramBotClient(botToken));
    })
    .Build();

using var scope = host.Services.CreateScope();
var controller = scope.ServiceProvider.GetRequiredService<TelegramController>();
var botClient = scope.ServiceProvider.GetRequiredService<TelegramBotClient>();

using var cts = new CancellationTokenSource();

var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = Array.Empty<UpdateType>() // semua jenis update
};

botClient.StartReceiving(
    async (client, update, token) =>
    {
        try
        {
            await controller.HandleUpdateAsync(update);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HandleUpdateError] {ex.Message}");
        }
    },
    (client, exception, token) =>
    {
        Console.WriteLine($"[PollingError] {exception.Message}");
        return Task.CompletedTask;
    },
    receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMe();
Console.WriteLine($"🤖 Bot @{me.Username} sedang berjalan... Tekan Enter untuk berhenti.");
Console.ReadLine();
cts.Cancel();
