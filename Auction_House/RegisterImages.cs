using Discord;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Auction_Dbot.Auction_House
{
    public class RegisterImages
    {
        public static async Task Execute(SocketMessage message, IMongoCollection<BsonDocument> cardCollection, IMongoCollection<BsonDocument> userCollection)
        {
            BsonDocument userData = Database.getUserData((long)message.Author.Id, userCollection).Result;
            // Checking the photo url and storing it in database
            if (message.Channel is IDMChannel && userData.GetValue("cardCreationInProgress").AsBoolean)
            {
                if (message.Attachments.Count != 0 && Cache.allowedContentType.Contains(message.Attachments.First().ContentType))
                {
                    var filter = Builders<BsonDocument>.Filter.Eq("_id", userData.GetValue("cardInCompleteId"));
                    var update = Database.createUpdateSet("photoUrl", message.Attachments.First().Url);                       
                    
                    await cardCollection.UpdateOneAsync(filter, update);
                   
                    SelectMenuBuilder menu = new SelectMenuBuilder().WithCustomId($"nsfwMenu_{userData.GetValue("cardInCompleteId")}").AddOption("Yes", "y").AddOption("No", "n");
                    var componentbuilder = new ComponentBuilder().WithSelectMenu(menu);
                    await message.Author.SendMessageAsync("Does this image contains any Not Safe For Work(NSFW) elements?",components:componentbuilder.Build());
                }
                if (message.Embeds.Count != 0 && Cache.allowedContentTypeExt.Any(s => message.Embeds.First().Url.EndsWith(s)))
                {
                    var filter = Builders<BsonDocument>.Filter.Eq("_id", userData.GetValue("cardInCompleteId"));
                    var update = Database.createUpdateSet("photoUrl", message.Attachments.First().Url);

                    var userFilter = Database.getUserFilter((long)message.Author.Id);
                    var inCompleteUpdate = Database.createUpdateSet("cardCreationInProgress", false);
                    await cardCollection.UpdateOneAsync(filter, update);
                    await userCollection.UpdateOneAsync(userFilter, inCompleteUpdate);                   
                }
            }
        }
        public static async Task SelectMenu(SocketMessageComponent component, IMongoCollection<BsonDocument> itemCollection, IMongoCollection<BsonDocument> userCollection)
        {
            var userFilter = Database.getUserFilter(long.Parse(component.User.Id.ToString()));
            var itemFilter = Database.getItemFilter(ObjectId.Parse(component.Data.CustomId.Split("_")[1]));
            if (component.Data.Values.First() == "y")
            {
                var nsfwUpdate = Database.createUpdateSet("nsfw", true);
                await itemCollection.UpdateOneAsync(itemFilter, nsfwUpdate);
            }
            var inCompleteUpdate = Database.createUpdateSet("cardCreationInProgress", false);
            await userCollection.UpdateOneAsync(userFilter, inCompleteUpdate);
            await component.RespondAsync("Successfully added this image to the card media.");
        }
        /*public static async Task classifyImage(string url, FilterDefinition<BsonDocument> filter,IMongoCollection<BsonDocument> cardCollection)
        {
            var nsfwImagedetector = new NsfwSpy();
            var uri = new Uri(url);
            var result = nsfwImagedetector.ClassifyImage(uri);
            float resultFloat = result.Sexy + result.Hentai + result.Pornography;
            bool isImageNsfw = resultFloat > 0.992000 ? true : false;
            Console.WriteLine(resultFloat);
            Console.WriteLine(isImageNsfw);
            if (isImageNsfw)
            {
                var nsfwUpdate = Database.createUpdateSet("nsfw", true);
                await cardCollection.UpdateOneAsync(filter, nsfwUpdate);
            }
        }*/
    }
}
