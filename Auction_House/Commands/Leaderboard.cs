using Discord;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Auction_Dbot.Auction_House.Commands
{
    public class Leaderboard
    {
        public static async Task Execute(SocketSlashCommand cmd, IMongoCollection<BsonDocument> userCollection)
        {
            try
            {
                await cmd.DeferAsync();
                var allUserFilter = Builders<BsonDocument>.Filter.Eq("isUser", true);
                List<BsonDocument> users = await userCollection.Find(allUserFilter).Sort(Builders<BsonDocument>.Sort.Descending("cash")).ToListAsync();
                (ComponentBuilder buttons, string desc) = createLeaderBoardEmbed(users, cmd.User, userCollection, 0);
                var embed = new EmbedBuilder()
                    .WithAuthor($"{cmd.User.Username}#{cmd.User.DiscriminatorValue}")
                    .WithTitle("Global Leaderboard")
                    .WithDescription(desc)
                    .WithColor(Color.DarkTeal)
                    .WithCurrentTimestamp();
                await cmd.FollowupAsync(embed: embed.Build(), components: buttons.Build());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\n" + e.StackTrace);
                throw;
            }
            
        }
        public static (ComponentBuilder cmp, string desc) createLeaderBoardEmbed(List<BsonDocument> users,SocketUser interactor, IMongoCollection<BsonDocument> userCollection, int startAT)
        {
            try
            {
                string desc = "             ┌── •✧• ──┐\n\n";
                var buttons = new ComponentBuilder();
                int interactorRank = -1;
                for (int i = 0; i < users.Count; i++)
                {
                    if (ulong.Parse(users[i].GetValue("userid").AsInt64.ToString()) == interactor.Id)
                    {
                        interactorRank = i + 1;
                    }
                }
                if (users.Count <= 10) // if card list has less than 10 cards we wont be adding next button
                {
                    for (int i = 0; i < users.Count; i++)
                    {
                        SocketUser user = Program._client.GetUser(ulong.Parse(users[i].GetValue("userid").AsInt64.ToString()));
                        if (user == null)
                        {
                            desc = desc + (i + 1) + ". " + users[i].GetValue("name").AsString + "\n ➥ " + users[i].GetValue("cash").AsInt32 + "🪙\n";
                            buttons.WithButton((i + 1).ToString(), "leaderboardButton_" + interactor.Id + "_" + users[i].GetValue("userid").AsInt64);
                            continue;
                        }
                        desc = desc + (i + 1) + ". " + user.Username + "#" + user.DiscriminatorValue + "\n ➥ " + users[i].GetValue("cash").AsInt32 + "🪙\n";
                        buttons.WithButton((i + 1).ToString(), "leaderboardButton_" + interactor.Id + "_" + user.Id);
                    }
                }
                else
                {
                    int i;
                    bool skipNext = false;
                    for (i = startAT; i <= startAT + 9; i++)
                    {
                        try
                        {
                            SocketUser user = Program._client.GetUser(ulong.Parse(users[i].GetValue("userid").AsInt64.ToString()));
                            if (user == null)
                            {
                                desc = desc + (i + 1) + ". " + users[i].GetValue("name").AsString + "\n ➥ " + users[i].GetValue("cash").AsInt32 + "🪙\n";
                                buttons.WithButton((i + 1).ToString(), "leaderboardButton_" + interactor.Id + "_" + users[i].GetValue("userid").AsInt64);
                                continue;
                            }
                            desc = desc + (i + 1) + ". " + user.Username + "#" + user.DiscriminatorValue + "\n ➥ " + users[i].GetValue("cash").AsInt32 + "🪙\n";
                            buttons.WithButton((i + 1).ToString(), "leaderboardButton_" + interactor.Id + "_" + user.Id);
                        }
                        catch (Exception e)
                        {
                            if (e.Message == "Index was out of range. Must be non-negative and less than the size of the collection. (Parameter 'index')")
                            {
                                skipNext = true;
                                break;
                            }

                        }
                    }
                    if (!skipNext)
                    {
                        buttons.AddRow(new ActionRowBuilder().WithButton("Next>>", $"leaderBoardNext_{interactor.Id}_{i}"));
                    }
                }
                desc += "\n             └── •✧• ──┘\nYour rank: #" + interactorRank;
                return (buttons, desc);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\n" + e.StackTrace);
                throw;
            }
            
        }
        //leaderboardButton_interactorId_userid
        public static async Task leaderBoardButtonHandler(SocketMessageComponent component, IMongoCollection<BsonDocument> userCollection)
        {
            ulong userid = ulong.Parse(component.Data.CustomId.Split("_")[2]);
            SocketUser user = Program._client.GetUser(userid);
            SocketUser interactor = Program._client.GetUser(ulong.Parse(component.Data.CustomId.Split("_")[1]));
            EmbedBuilder profileEmbed = Profile.profileEmbedBuilder(user, userCollection, userid);
            var buttonTP = new ComponentBuilder()
                        .WithButton("Owned Cards", $"owned_{interactor.Id}_{userid}")
                        .WithButton("Created Cards", $"create_{interactor.Id}_{userid}");
            await component.UpdateAsync(x => { x.Embed = profileEmbed.Build();x.Components = buttonTP.Build(); });
        }
        //leaderBoardNext_interactorID_startat
        public static async Task leaderBoardNextOrPrevButtonHandler(SocketMessageComponent component, IMongoCollection<BsonDocument> userCollection)
        {
            int startAt = int.Parse(component.Data.CustomId.Split("_")[2]);
            var allUserFilter = Builders<BsonDocument>.Filter.Eq("isUser", true);
            List<BsonDocument> users = await userCollection.Find(allUserFilter).Sort(Builders<BsonDocument>.Sort.Descending("cash")).ToListAsync();
            (ComponentBuilder buttons, string desc) = createLeaderBoardEmbed(users, component.User, userCollection, startAt);
            if (startAt != 0)
            {
                buttons.AddRow(new ActionRowBuilder().WithButton("<<Previous", $"leaderBoardPrev_{component.User.Id}_" + (startAt - 10)));
            }
            var embed = new EmbedBuilder()
                .WithAuthor(component.User.Username + "#" + component.User.Discriminator)
                .WithTitle("Global Leaderboard")
                .WithDescription(desc)
                .WithColor(Color.DarkTeal)
                .WithCurrentTimestamp();
            await component.UpdateAsync(x => { x.Embed = embed.Build(); x.Components = buttons.Build(); });
        }
    }
}
