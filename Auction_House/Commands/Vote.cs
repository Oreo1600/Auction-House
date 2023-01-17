using Discord;
using Discord.WebSocket;
using DiscordBotsList.Api;
using DiscordBotsList.Api.Internal;
using DiscordBotsList.Api.Objects;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Auction_Dbot.Auction_House.Commands
{
    public class Vote
    {
        public static async Task Execute(SocketSlashCommand cmd, IMongoCollection<BsonDocument> userCollection)
        {
            await cmd.DeferAsync();
            var userFilter = Database.getUserFilter(long.Parse(cmd.User.Id.ToString()));
            var userData = await userCollection.Find(userFilter).FirstAsync();
            DateTime voteTimer = userData.GetValue("voteTimer").AsDateTime;
            DateTime voteExpire = voteTimer.AddHours(12);
            if (voteExpire.CompareTo(DateTime.UtcNow) < 0)
            {
                HttpClient httpClient = new();
                httpClient.DefaultRequestHeaders.Add("Authorization", Environment.GetEnvironmentVariable("topggToken"));
                string botId = "935183469959602216";
                string uri = $"https://top.gg/api/bots/{botId}/check?userId={cmd.User.Id}";
                dynamic jsonObj;

                try
                {
                    string json = await httpClient.GetStringAsync(uri);
                    jsonObj = JObject.Parse(json);
                }
                catch
                {
                    throw;
                }

                // List<IDblEntity> stats = await Program.DblApi.GetVotersAsync();
                if (jsonObj.voted != 0) // (stats.Any(v => v.Id == cmd.User.Id))
                {
                    var cashUpdate = Builders<BsonDocument>.Update.Inc("cash", 4000);
                    var rateUpdate = Builders<BsonDocument>.Update.Inc("payoutRate", 4.0);
                    var voteTimerupdate = Database.createUpdateSet("voteTimer", DateTime.Now);
                    await userCollection.UpdateOneAsync(userFilter, cashUpdate);
                    await userCollection.UpdateOneAsync(userFilter, rateUpdate);
                    await userCollection.UpdateOneAsync(userFilter, voteTimerupdate);
                    var embedVoter = new EmbedBuilder().WithAuthor(cmd.User.Username + "#" + cmd.User.DiscriminatorValue)
                    .WithTitle("Vote").WithDescription("Thanks for voting Auction House on top.gg\n Here is your cool rewards:\n\n‣ 4000🪙 ✅\n‣ 4% Payout Rate increased ✅").WithColor(Color.Gold);
                    await cmd.FollowupAsync(embed: embedVoter.Build());
                    return;
                }
                var embed = new EmbedBuilder().WithAuthor(cmd.User.Username + "#" + cmd.User.DiscriminatorValue)
                    .WithTitle("Vote").WithDescription("Vote Auction House on top.gg and get these cool rewards:\n\n‣ 4000🪙\n‣ 4% Payout Rate increased.\n\nRemember to send /vote again to claim the rewards.").WithColor(Color.Gold);
                var componentBuilder = new ComponentBuilder().WithButton("Vote on Top.gg", style: ButtonStyle.Link, url: "https://top.gg/bot/935183469959602216");
                await cmd.FollowupAsync(embed:embed.Build(),components:componentBuilder.Build());
            }
            else
            {
                TimestampTag timestamp = new TimestampTag(voteExpire,TimestampTagStyles.Relative);
                var embed = new EmbedBuilder().WithAuthor(cmd.User.Username + "#" + cmd.User.DiscriminatorValue)
                    .WithTitle("Vote").WithDescription($"Thanks for voting Auction House\nPlease send /vote again {timestamp} to claim these rewards:\n\n‣ 4000🪙\n‣ 4% Payout Rate increased.").WithColor(Color.Gold);
                await cmd.FollowupAsync(embed: embed.Build());
            }
        }

    }
}
