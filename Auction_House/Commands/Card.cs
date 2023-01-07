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
                    bool isNsfw = true;
                    if (cmd.Channel is IDMChannel) { isNsfw= false;}
                    else if (cmd.Channel is SocketTextChannel)
                    {
                        SocketTextChannel channel = cmd.Channel as SocketTextChannel;
                        isNsfw = channel.IsNsfw;
                    }                 
                    var embedBuiler = createCard(itemData,isNsfw);
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
        public static EmbedBuilder createCard(BsonDocument itemData,bool isChannelNsfw = false)
        {
            SocketUser creator = Program._client.GetUser(ulong.Parse(itemData.GetValue("creator").AsInt64.ToString()));
            string creatorMention = creator.Mention;
            string ownerMention = "Not owned";
            string rarity = "Not yet evaluated";
            int price = itemData.GetValue("price").AsInt32;
            string photourl = itemData.GetValue("nsfw").AsBoolean && !isChannelNsfw ? "https://cdn.discordapp.com/attachments/1047465714086334474/1060948087268450444/fetchimage.png" : itemData.GetValue("photoUrl").AsString;
            var descProfanityFilter = new ProfanityFilter.ProfanityFilter();
            string cardDescOriginal = itemData.GetValue("cardDesc").AsString;
            string cardDesc = descProfanityFilter.ContainsProfanity(cardDescOriginal) && !isChannelNsfw ? descProfanityFilter.CensorString(cardDescOriginal, '█') : cardDescOriginal;
                      
            Color cardColor = Color.DarkGrey;
            List<Color> colorarray = new List<Color>() { Color.DarkerGrey, Color.LighterGrey, Color.Blue, Color.Green, Color.Orange, Color.DarkPurple, Color.Red };
            if (itemData.GetValue("owner").AsInt64.ToString() != "0")
            {
                SocketUser owner = Program._client.GetUser(ulong.Parse(itemData.GetValue("owner").AsInt64.ToString()));
                ownerMention = owner.Mention;
            }
            if (float.Parse(itemData.GetValue("rarity").AsDouble.ToString()) != 0.0)
            {
                rarity = Math.Round(itemData.GetValue("rarity").AsDouble,1).ToString();
                for (int i = 1; i <= 6; i++)
                {
                    if (i == Math.Floor(Decimal.Parse(rarity)))
                    {
                        cardColor = colorarray[i];
                    }
                }
                rarity += "⭐";
            }
            string description = $"{cardDesc}\n\n👤 Creator: {creatorMention}\n\n🤴 Owner:{ownerMention}\n\n💫 Rarity: {rarity}";
            if (price != 0)
            {
                description = description + "\n\n💰 Sold for " + price + "🪙";
            }
            else if (itemData.GetValue("nsfw").AsBoolean)
            {
                description = description + "\n\n🔞 This card contains nsfw image.Therefore, the card image will only appear in nsfw channels.";
            }
            var embedBuiler = new EmbedBuilder()
            .WithTitle(itemData.GetValue("cardName").AsString)
            .WithDescription(description)
            .WithImageUrl(photourl)
            .WithColor(cardColor)
            .WithCurrentTimestamp();

            return embedBuiler;
        }
    }
}
