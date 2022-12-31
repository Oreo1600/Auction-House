using Discord;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Auction_Dbot.Auction_House.Commands
{
    public class Create
    {
        public static async Task Execute(SocketSlashCommand cmd, IMongoCollection<BsonDocument> userCollection)
        {
            try
            {
                BsonDocument userdata = Database.getUserData((long)cmd.User.Id, userCollection).Result;
                if (userdata.GetValue("cardCreationInProgress").AsBoolean)
                {
                    await cmd.RespondAsync("One of your card is missing an image. Please upload it first!", ephemeral: true);
                    return;
                }

                DateTime canCreateCard = userdata.GetValue("canCreateCardUntill").AsDateTime;

                /* if (canCreateCard.CompareTo(DateTime.Now) > 0)
                 {
                     TimeSpan timeSpan = canCreateCard.Subtract(DateTime.Now);
                     await cmd.RespondAsync("You have already created a card today. Try again in " + timeSpan.Hours + " hours", ephemeral:true);
                     return;
                 }*/

                var mb = new ModalBuilder()
                .WithTitle("New Card")
                .WithCustomId("NewCard")
                .AddTextInput("Card Name (Cannot change later)", "CardName", placeholder: "What would the name be for this creation?", maxLength: 50, required: true)
                .AddTextInput("Card Description", "CardDesc", TextInputStyle.Paragraph,
                "Any special features in mind?", maxLength: 100, required: true);

                await cmd.RespondWithModalAsync(mb.Build());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + e.StackTrace);
            }
            
        }

        public static async Task FormHandle(SocketModal modal)
        {
            try
            {
                await modal.DeferAsync(ephemeral: true);

                var itemCollection = Database.getCollection("Cards");
                var userCollection = Database.getCollection("Users");

                List<SocketMessageComponentData> components = modal.Data.Components.ToList();
                string cardName = components.First(x => x.CustomId == "CardName").Value;

                var itemfilter = Database.getItemFilter(cardName.ToLower());
                var userfilter = Database.getUserFilter((long)modal.User.Id);

                if (itemCollection.Find(itemfilter).CountDocumentsAsync().Result > 0)
                {
                    await modal.FollowupAsync("There is already a card with this name.", ephemeral: true);
                    return;
                }

                string cardDesc = components.First(x => x.CustomId == "CardDesc").Value;

                var isPinnedFilter = Builders<BsonDocument>.Filter.Eq("isPinned", true);
                BsonDocument itemPinned = await itemCollection.Find(isPinnedFilter).FirstAsync();

                var newDoc = new BsonDocument
                {
                    {"cardRank", itemPinned.GetValue("cardRank").AsInt32},
                    {"cardName",cardName.Trim()},
                    {"cardNameLower",cardName.ToLower().Trim()},
                    {"cardDesc",cardDesc},
                    {"photoUrl","" },
                    {"price",0 },
                    {"auctionId",0},
                    {"failedAuctions",0 },
                    {"creator",(long)modal.User.Id },
                    {"owner",(long)0 },
                    {"rarity",(double)0.0 },
                    {"reviews",new BsonArray() }
                };

                await itemCollection.InsertOneAsync(newDoc);

                BsonDocument newItem = Database.getItemData(cardName, itemCollection).Result;

                var cardRankIncreased = Builders<BsonDocument>.Update.Inc("cardRank", 1);
                await itemCollection.UpdateOneAsync(isPinnedFilter, cardRankIncreased);

                var cardpushUpdate = Builders<BsonDocument>.Update.Push<ObjectId>("cardListCreated", newItem.GetValue("_id").AsObjectId);
                await userCollection.UpdateOneAsync(userfilter, cardpushUpdate);

                var cardIncompleteIdUpdate = Database.createUpdateSet("cardInCompleteId", newItem.GetValue("_id").AsObjectId);
                await userCollection.UpdateOneAsync(userfilter, cardIncompleteIdUpdate);

                var newCardUntillUpdate = Database.createUpdateSet("canCreateCardUntill", new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.AddDays(1).Day, 00, 00, 00));
                await userCollection.UpdateOneAsync(userfilter, newCardUntillUpdate);

                var cardInProgressUpdate = Database.createUpdateSet("cardCreationInProgress", true);
                await userCollection.UpdateOneAsync(userfilter, cardInProgressUpdate);

                var cashUpdate = Builders<BsonDocument>.Update.Inc("cash", 500);
                await userCollection.UpdateOneAsync(userfilter, cashUpdate);

                var rateUpdate = Builders<BsonDocument>.Update.Inc("payoutRate", 0.2);
                await userCollection.UpdateOneAsync(userfilter, rateUpdate);

                await modal.User.SendMessageAsync($"Your card's photo is missing!\nCard Name: {cardName} \nCard Description: {cardDesc}\nYou may send an image here after this message. Note: You cannot change your photo.");

                await modal.FollowupAsync("Successfully created card!\nYou have earned 500🪙.\nYour Payout rate is increased by 0.2%", ephemeral:true);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message + e.StackTrace);
            }
        }
    }
}
