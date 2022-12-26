using Discord;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Auction_Dbot.Auction_House.Commands
{
    public class Card
    {
        public static async Task Execute(SocketSlashCommand cmd, IMongoCollection<BsonDocument> collection)
        {
            try
            {
                if (cmd.Data.Options.First().Value != String.Empty)
                {
                    BsonDocument itemData = await collection.Find(Database.getItemFilter(cmd.Data.Options.First().Value.ToString().ToLower())).FirstAsync();
                    var embedBuiler = createCard(itemData);
                    await cmd.RespondAsync(embed: embedBuiler.Build());
                }
            }
            catch (Exception e)
            {
                if (e.Message == "Sequence contains no elements")
                {
                    await cmd.RespondAsync("No cards found by that name");
                }
            }
            
        }
        public static EmbedBuilder createCard(BsonDocument itemData)
        {
            SocketUser creator = Program._client.GetUser(ulong.Parse(itemData.GetValue("creator").AsInt64.ToString()));
            string creatorMention = creator.Mention;
            string ownerMention = "Not owned";
            string rarity = "Not yet evaluated";
            int price = itemData.GetValue("price").AsInt32;
            Color cardColor = Color.DarkGrey;
            List<Color> colorarray = new List<Color>() { Color.DarkerGrey, Color.LighterGrey, Color.Blue, Color.Green, Color.Orange, Color.DarkPurple, Color.Red };
            if (itemData.GetValue("owner").AsInt64.ToString() != "0")
            {
                SocketUser owner = Program._client.GetUser(ulong.Parse(itemData.GetValue("owner").AsInt64.ToString()));
                ownerMention = owner.Mention;
            }
            if (float.Parse(itemData.GetValue("rarity").AsDouble.ToString()) != 0.0)
            {
                rarity = itemData.GetValue("rarity").AsDouble.ToString();
                for (int i = 1; i <= 6; i++)
                {
                    if (i == (Math.Floor(Decimal.Parse(rarity))))
                    {
                        cardColor = colorarray[i];
                    }
                }
                rarity += "⭐";
            }
            string description = $"{itemData.GetValue("cardDesc").AsString}\n\n👤 Creator: {creatorMention}\n\n🤴 Owner:{ownerMention}\n\n💫 Rarity: {rarity}";
            if (price != 0)
            {
                description = description + "\n\n💰 Sold for " + price + "🪙";
            }
            var embedBuiler = new EmbedBuilder()
            .WithTitle(itemData.GetValue("cardName").AsString)
            .WithDescription(description)
            .WithImageUrl(itemData.GetValue("photoUrl").AsString)
            .WithColor(cardColor)
            .WithCurrentTimestamp();

            return embedBuiler;
        }
    }
}
