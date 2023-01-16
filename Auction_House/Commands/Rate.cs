using Discord;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Auction_Dbot.Auction_House.Commands
{
    public class Rate
    {
        public static async Task Execute(SocketSlashCommand cmd, IMongoCollection<BsonDocument> collection, IMongoCollection<BsonDocument> userCollection)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("rarity", 0);
            var list = await collection.Find(filter).ToListAsync();
            if (list.Count == 0)
            {
                await cmd.RespondAsync("No cards found at the moment. Please try again later.",ephemeral:true);
                return;
            }
            int r = new Random().Next(0, list.Count - 1);
            BsonDocument itemData = list[r];
            bool isNsfw = true;
            if (cmd.Channel is IDMChannel) { isNsfw = false; }
            else if (cmd.Channel is SocketTextChannel)
            {
                SocketTextChannel channel = cmd.Channel as SocketTextChannel;
                isNsfw = channel.IsNsfw;
            }
            var embedBuilder = Card.createCard(itemData,userCollection,isNsfw);
            var menuBuilder = new SelectMenuBuilder()
            .WithPlaceholder("Select an option")
            .WithCustomId($"rateMenu_{itemData.GetValue("cardName")}_{cmd.User.Id}")
            .WithMinValues(1)
            .WithMaxValues(1)
            .AddOption("1 ⭐", "1", "Meh")
            .AddOption("2 ⭐", "2", "Common.")
            .AddOption("3 ⭐", "3", "Rare!")
            .AddOption("4 ⭐", "4", "EpIc!")
            .AddOption("5 ⭐", "5", "Legendary!!")
            .AddOption("6 ⭐", "6", "Exotic!!!");
            var builder = new ComponentBuilder()
            .WithSelectMenu(menuBuilder);

            await cmd.RespondAsync(embed: embedBuilder.Build(), components: builder.Build());
        }

        public static async Task SelectMenu(SocketMessageComponent component, IMongoCollection<BsonDocument> collection, IMongoCollection<BsonDocument> userCollection)
        {
            if (ulong.Parse(component.Data.CustomId.Split("_")[2]) != component.User.Id)
            {
                await component.RespondAsync("You cannot interact with this menu",ephemeral:true);
                return;
            }
            BsonDocument itemData = Database.getItemData(component.Data.CustomId.Split("_")[1], collection).Result;
            BsonArray ratings = itemData.GetValue("reviews").AsBsonArray;

            ratings = ratings.Add(int.Parse(component.Data.Values.First()));
            var pushUpdate = Builders<BsonDocument>.Update.Set("reviews", ratings);
            var filter = Database.getItemFilter(component.Data.CustomId.Split("_")[1]);

            if (ratings.Count > 4)
            {
                double overallRating = 0.0;
             
                for (int i = 0; i < ratings.Count; i++)
                {
                    overallRating = overallRating + ratings[i].AsInt32;
                }
                overallRating = overallRating / ratings.Count;
                var ratingUpdate = Database.createUpdateSet("rarity", overallRating);
                await collection.UpdateOneAsync(filter, ratingUpdate);
            }
            var userfilter = Database.getUserFilter((long)component.User.Id);
            var cashUpdate = Builders<BsonDocument>.Update.Inc("cash", 1000);
            await userCollection.UpdateOneAsync(userfilter, cashUpdate);

            var rateUpdate = Builders<BsonDocument>.Update.Inc("payoutRate", 0.05);
            await userCollection.UpdateOneAsync(userfilter, rateUpdate);

            await collection.UpdateOneAsync(filter, pushUpdate);
            await component.UpdateAsync(x => { x.Content = "You have rated this card: " + component.Data.Values.First() + "⭐\nTotal ratings: "+ ratings.Count + "\nYou have earned 1000🪙.\nYour Payout rate is increased by 0.05%"; x.Components = null; });
        }
    }
}