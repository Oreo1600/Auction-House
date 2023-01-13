using Auction_Dbot.Auction_House;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBotsList.Api;
using System.Timers;

namespace Auction_Dbot
{
    public class Program
    {

        public static Task Main(string[] args) => new Program().MainAsync();
        public static DiscordSocketClient _client;
        public static System.Timers.Timer auctionTimer;
        public static System.Timers.Timer moneyPoolTimer;
        public static System.Timers.Timer gradRateUpgrade;
        public static AuthDiscordBotListApi DblApi;
        public async Task MainAsync()
        {
            try
            {
                var config = new DiscordSocketConfig()
                {
                    // Other config options can be presented here.
                    GatewayIntents = GatewayIntents.All,
                    AlwaysDownloadUsers = true
                };
                _client = new DiscordSocketClient(config);
                _client.Log += Log;

                string token = Environment.GetEnvironmentVariable("botToken");

                //Starting the bot
                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();
                DblApi = new AuthDiscordBotListApi(935183469959602216, Environment.GetEnvironmentVariable("topggToken"));
                Database.Connect();                

                //Starting an auction
                auctionTimer = new System.Timers.Timer(25200000);
                auctionTimer.Elapsed += new ElapsedEventHandler(Handlers.OnTimedEvent);
                auctionTimer.Start();

                //Resetting MoneyPool
                moneyPoolTimer = new System.Timers.Timer(259200000);
                MoneyPool.MoneyPoolReset();
                moneyPoolTimer.Elapsed += new ElapsedEventHandler(MoneyPool.MoneyPoolReset);
                moneyPoolTimer.Start();

                //gradually increasing rates of users
                gradRateUpgrade = new System.Timers.Timer(7200000);
                MoneyPool.IncreaseRate();
                gradRateUpgrade.Elapsed += new ElapsedEventHandler(MoneyPool.IncreaseRate);
                gradRateUpgrade.Start();


                //Handling events
                _client.Ready += Handlers.Client_Ready;
                _client.SlashCommandExecuted += Handlers.SlashCommandHandler;
                _client.MessageReceived += Handlers.MessageRecievedHandler;
                _client.ModalSubmitted += Handlers.ModalSubmittedHandler;
                _client.JoinedGuild += Handlers.JoinedGuildHandler;
                _client.LeftGuild += Handlers.LeftGuildHandlers;
                _client.SelectMenuExecuted += Handlers.SelectMenuExecutedHandler;
                _client.ButtonExecuted += Handlers.Buttonhandler;

                await Task.Delay(-1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + e.StackTrace);
                throw;
            }
            
        }
        private Task Log(LogMessage msg)
        {
            //logs(not logarithm)
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
    public class LoggingService
    {
        public LoggingService(DiscordSocketClient client, CommandService command)
        {
            client.Log += LogAsync;
            command.Log += LogAsync;
        }
        private Task LogAsync(LogMessage message)
        {
            if (message.Exception is CommandException cmdException)
            {
                Console.WriteLine($"[Command/{message.Severity}] {cmdException.Command.Aliases.First()}"
                    + $" failed to execute in {cmdException.Context.Channel}.");
                Console.WriteLine(cmdException);
            }
            else
                Console.WriteLine($"[General/{message.Severity}] {message}");

            return Task.CompletedTask;
        }
    }
}