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


Env.Load();

var botToken = Environment.GetEnvironmentVariable("BOT_TOKEN")
               ?? throw new Exception("BOT_TOKEN tidak ditemukan di .env");

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
