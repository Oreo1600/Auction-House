using Discord;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Driver;
using NsfwSpyNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                    var nsfwImagedetector = new NsfwSpy();
                    var uri = new Uri(message.Attachments.First().Url);
                    var result = nsfwImagedetector.ClassifyImage(uri);
                    bool isImageNsfw = (result.Sexy + result.Hentai + result.Pornography) > 0.992000 ? true : false;
                    if (isImageNsfw)
                    {
                        var nsfwUpdate = Database.createUpdateSet("nsfw", true);
                        await cardCollection.UpdateOneAsync(filter, nsfwUpdate);
                    }
                    var userFilter = Database.getUserFilter((long)message.Author.Id);
                    var inCompleteUpdate = Database.createUpdateSet("cardCreationInProgress", false);
                    await cardCollection.UpdateOneAsync(filter, update);
                    await userCollection.UpdateOneAsync(userFilter, inCompleteUpdate);

                    await message.Author.SendMessageAsync("Successfully added this image to the card media.");
                }
                if (message.Embeds.Count != 0 && Cache.allowedContentTypeExt.Any(s => message.Embeds.First().Url.EndsWith(s)))
                {
                    var filter = Builders<BsonDocument>.Filter.Eq("_id", userData.GetValue("cardInCompleteId"));
                    var update = Database.createUpdateSet("photoUrl", message.Attachments.First().Url);

                    var userFilter = Database.getUserFilter((long)message.Author.Id);
                    var inCompleteUpdate = Database.createUpdateSet("cardCreationInProgress", false);
                    await cardCollection.UpdateOneAsync(filter, update);
                    await userCollection.UpdateOneAsync(userFilter, inCompleteUpdate);

                    await message.Author.SendMessageAsync("Successfully added this image to the card media.");
                }
            }
        }
    }
}
