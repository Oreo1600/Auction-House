using Discord;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Auction_Dbot.Auction_House.Commands
{
    public class Pool
    {
        public static async Task Execute(SocketSlashCommand cmd, IMongoCollection<BsonDocument> userCollection)
        {
            var pinFilter = Database.getPinFilter();

            BsonDocument pool = userCollection.Find(pinFilter).FirstAsync().Result;
            int poolMoney = pool.GetValue("moneyPool").AsInt32;
            var userFilter = Database.getUserFilter((long)cmd.User.Id);
            BsonDocument user = userCollection.Find(userFilter).FirstAsync().Result;
            
            double payoutRate = user.GetValue("payoutRate").AsDouble;
            double withdrawAmount = Math.Ceiling(poolMoney * payoutRate /100);
            
            DateTime resetIn = pool.GetValue("resetTime").AsDateTime;
            TimestampTag resetTimeStamp = new TimestampTag { Time = resetIn, Style = TimestampTagStyles.Relative };
     
            BsonArray recentWithdraws = pool.GetValue("recentWithdraws").AsBsonArray;
            string text = $"Money Pool: {poolMoney}🪙\nYour Payout Rate: {Math.Round(payoutRate, 2)}%\n\nYou can currently withdraw {withdrawAmount}🪙.\n\nMoney Pool resets in {resetTimeStamp}\n\n"+ (recentWithdraws.Count != 0 ? "Recent Withdraws:\n" : "");

            if (recentWithdraws.Count != 0)
            {
                for (int i = recentWithdraws.Count - 1; i >= 0; i--)
                {
                    text = text + recentWithdraws[i] + "\n";
                    if (i <= recentWithdraws.Count - 5)
                    {
                        break;
                    }
                }
                text += "\n\n";
            }
            text += "Click on Withdraw button below to withdraw the specified amount.";
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(cmd.User.Username + "#" + cmd.User.DiscriminatorValue)
                .WithTitle("Money Pool")
                .WithDescription(text)
                .WithColor(Color.Green)
                .WithCurrentTimestamp();
            ComponentBuilder button = new ComponentBuilder().WithButton("Withdraw", $"withdraw_{cmd.User.Id}");
            await cmd.RespondAsync(embed: embed.Build(),components:button.Build());
        }
        // customid = withdraw_userid
        public static async Task Withdraw(SocketMessageComponent component, IMongoCollection<BsonDocument> userCollection)
        {
            var userFilter = Database.getUserFilter((long)component.User.Id);
            var pinFilter = Database.getPinFilter();

            BsonDocument pool = userCollection.Find(pinFilter).FirstAsync().Result;
            BsonDocument userData = userCollection.Find(userFilter).FirstAsync().Result;
            int poolAmount = pool.GetValue("moneyPool").AsInt32;
            double payoutRate = userData.GetValue("payoutRate").AsDouble;

            double payoutCash = Math.Ceiling(poolAmount * payoutRate / 100);

            if (payoutCash == 0)
            {
                await component.Message.ModifyAsync(m => { m.Content = $"You cannot withdraw nothing";m.Components = null; m.Embed = null; });
                return;
            }
            var userCashUpdate = Builders<BsonDocument>.Update.Inc("cash", int.Parse(payoutCash.ToString()));
            var userRateUpdate = Database.createUpdateSet("payoutRate", 0.0);
            var poolMoneyUpdate = Builders<BsonDocument>.Update.Set("moneyPool", poolAmount - int.Parse(payoutCash.ToString()));

            TimestampTag tag = new TimestampTag() { Time = DateTime.Now, Style = TimestampTagStyles.ShortDateTime };
            var withdrawpushUpdate = Builders<BsonDocument>.Update.Push("recentWithdraws", $"{component.User.Mention} withdrew **{payoutCash}** from the moneypool.  | {tag}");
            await userCollection.UpdateOneAsync(pinFilter, poolMoneyUpdate);
            await userCollection.UpdateOneAsync(pinFilter, withdrawpushUpdate);
            await userCollection.UpdateOneAsync(userFilter,userCashUpdate);
            await userCollection.UpdateOneAsync(userFilter,userRateUpdate);

            await component.Message.ModifyAsync(m => { m.Content = $"You have successfully withdrew {payoutCash} from the money pool.\nYour new balance: {userData.GetValue("cash").AsInt32 + payoutCash}";m.Components = null; m.Embed = null; });
        }
    }
}
