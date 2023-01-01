using Auction_Dbot.Auction_House.Commands;
using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.Timers;

namespace Auction_Dbot.Auction_House
{
    public class Handlers
    {
        public static async Task SlashCommandHandler(SocketSlashCommand command)
        {
            try
            {
                var serverCollection = Database.getCollection("Servers");
                var userCollection = Database.getCollection("Users");
                var itemCollection = Database.getCollection("Cards");

                if (command.Channel is not IDMChannel)
                {
                    await Database.CheckAndAddServer(Program._client.GetGuild(command.GuildId.Value), serverCollection);
                }
                await Database.CheckAndAddUser(command.User, userCollection);

                //receiving commands
                switch (command.Data.Name)
                {
                    case "help": await Help.Execute(command); break;
                    case "create": await Create.Execute(command, userCollection); break;
                    case "card": await Card.Execute(command, itemCollection); break;
                    case "rate": await Rate.Execute(command, itemCollection); break;
                    case "profile": await Profile.Execute(command, userCollection); break;
                    case "pool": await Pool.Execute(command, userCollection); break;
                    case "server_settings": await ChangeSettings.Execute(command, serverCollection); break;
                    case "leaderboard": await Leaderboard.Execute(command, userCollection); break;
                    default: break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + e.StackTrace);
                throw;
            }
            
        }
        public static async Task Client_Ready()
        {           
            try
            {
                //set commands
                
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }
        public static async Task ModalSubmittedHandler(SocketModal modal)
        {
            var auctionCollection = Database.getCollection("Auctions");
            var userCollection = Database.getCollection("Users");
            await Database.CheckAndAddUser(modal.User, userCollection);
            if (modal.Data.CustomId == "NewCard") // modal for card create command
            {
                _ = Task.Run(() => { Create.FormHandle(modal); });
            }
            else if (modal.Data.CustomId.StartsWith("bidModal")) // modal for auction bidding
            {
                _ = Task.Run(() => { Auction.BidHandle(modal,auctionCollection,userCollection); });
            }
        }
        public static async Task JoinedGuildHandler(SocketGuild guild)
        {
            // Adding server to the serverlist in database
            var collection = Database.getCollection("Servers");

            var filter = Builders<BsonDocument>.Filter.Eq("isList", true);
            var updatePush = Builders<BsonDocument>.Update.Push<long>("serverList", (long)guild.Id);
            var updateInc = Builders<BsonDocument>.Update.Inc<int>("serverCount", 1);

            await collection.UpdateOneAsync(filter, updatePush);
            await collection.UpdateOneAsync(filter, updateInc);
        }
        public static async Task LeftGuildHandlers(SocketGuild guild)
        {
            //removing server from the list
            var collection = Database.getCollection("Servers");

            var filter = Builders<BsonDocument>.Filter.Eq("isList", true);
            var updatePush = Builders<BsonDocument>.Update.Pull<long>("serverList", (long)guild.Id);
            var updateDsc = Builders<BsonDocument>.Update.Inc<int>("serverCount", -1);

            await collection.UpdateOneAsync(filter, updatePush);
            await collection.UpdateOneAsync(filter, updateDsc);
        }
        public static async Task MessageRecievedHandler(SocketMessage message)
        {
            var userCollection = Database.getCollection("Users");
            if (message.Content == "!startauction" && message.Author.Id == 511583052401475604) // temp command
            {
                var serverCollection = Database.getCollection("Servers");
                var itemCollection = Database.getCollection("Cards");
                Task.Run(() => { Auction.Execute(serverCollection, itemCollection, userCollection); });
            }
            try
            {
                var cardCollection = Database.getCollection("Cards");
                BsonDocument userData = Database.getUserData((long)message.Author.Id, userCollection).Result;
                // Checking the photo url and storing it in database
                if (message.Channel is IDMChannel && userData.GetValue("cardCreationInProgress").AsBoolean)
                {
                    if (message.Attachments.Count != 0 && Cache.allowedContentType.Contains(message.Attachments.First().ContentType))
                    {
                        var filter = Builders<BsonDocument>.Filter.Eq("_id", userData.GetValue("cardInCompleteId"));
                        var update = Database.createUpdateSet("photoUrl", message.Attachments.First().Url);

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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                /*if (e.Message == "One or more errors occurred. (Sequence contains no elements)")
                {
                    Console.Write("");
                }*/
                /*if (!Cache.userList.Contains(message.Author.Id))
                {
                    Cache.userList.Add(message.Author.Id);
                    if (!Database.IsDocExistsAsync(userCollection, (long)message.Author.Id).Result)
                    {
                        await Database.AddUser(message.Author, userCollection);
                    }
                }
                throw;*/
            }
        }

        public static async Task Buttonhandler(SocketMessageComponent component)
        {
            var userCollection = Database.getCollection("Users");
            var itemCollection = Database.getCollection("Cards");
            var serverCollection = Database.getCollection("Servers");
            
            await Database.CheckAndAddUser(component.User, userCollection);
            try
            {
                //check if the person who sent is command is the one that is interacting
                if (component.Data.CustomId.StartsWith("bidButton"))
                {
                    await Auction.Bid(component);
                    return;
                }
                if (ulong.Parse(component.Data.CustomId.Split("_")[1]) != component.User.Id)
                {
                    await component.RespondAsync("You cannot interact with this menu", ephemeral: true);
                    return;
                }
                string buttonType = component.Data.CustomId.Split("_")[0];
                switch (buttonType)
                {
                    case "owned":
                        _ = Task.Run(() => { Profile.ownedCards(component, userCollection, itemCollection); });
                        break;
                    case "create": _ = Task.Run(() => { Profile.createdCards(component, userCollection, itemCollection); });  break;
                    case "cardButton": await Profile.cardButtonClicked(component, itemCollection); break;
                    case "nextButtonCreated":
                        await Profile.nextOrPrevButtonClicked(component, userCollection, itemCollection, component.Data.CustomId.Split("_")[0]);
                        break;
                    case "previousButtonCreated":
                        await Profile.nextOrPrevButtonClicked(component, userCollection, itemCollection, component.Data.CustomId.Split("_")[0]);
                        break;
                    case "nextButtonOwned":
                        await Profile.nextOrPrevButtonClicked(component, userCollection, itemCollection, component.Data.CustomId.Split("_")[0]);
                        break;
                    case "previousButtonOwned":
                        await Profile.nextOrPrevButtonClicked(component, userCollection, itemCollection, component.Data.CustomId.Split("_")[0]);
                        break;
                    case "withdraw": await Pool.Withdraw(component, userCollection); break;
                    case "tglAuctionHouse": await ChangeSettings.toogleAuctionHouse(component, serverCollection); break;
                    case "setAuctionChannel": await ChangeSettings.setAuctionHouse(component, serverCollection); break;
                    case "leaderboardButton": await Leaderboard.leaderBoardButtonHandler(component, userCollection); break;
                    case "leaderBoardNext": await Leaderboard.leaderBoardNextOrPrevButtonHandler(component, userCollection); break;
                    case "leaderBoardPrev": await Leaderboard.leaderBoardNextOrPrevButtonHandler(component, userCollection); break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + e.StackTrace);
            }
            
            
        }
        public static async Task SelectMenuExecutedHandler(SocketMessageComponent component)
        {
            var itemCollection = Database.getCollection("Cards");
            var userCollection = Database.getCollection("Users");
            await Rate.SelectMenu(component,itemCollection,userCollection);
        }

        public static void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            var serverCollection = Database.getCollection("Server");
            var itemCollection = Database.getCollection("Cards");
            var userCollection = Database.getCollection("Users");
            Task.Run(() => { Auction.Execute(serverCollection, itemCollection, userCollection); });
        }
    }
}
