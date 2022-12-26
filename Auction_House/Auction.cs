using Auction_Dbot.Auction_House.Commands;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Reactive;
using System.Timers;
using static MongoDB.Bson.Serialization.Serializers.SerializerHelper;
using Timer = System.Timers.Timer;

namespace Auction_Dbot.Auction_House
{
    public class Auction
    {
        public static async Task Execute(IMongoCollection<BsonDocument> serverCollection, IMongoCollection<BsonDocument> itemCollection, IMongoCollection<BsonDocument> userCollection)
        {
            try
            {
                // This code executes when the timer event is fired
                Random r = new Random();
                var auctionCollection = Database.getCollection("Auctions");
                var pinnedFilter = Database.getPinFilter();
                BsonDocument pinnedDoc = await auctionCollection.Find(pinnedFilter).FirstAsync();

                var serverFilter = Builders<BsonDocument>.Filter.Eq("isAuctionHouseOn", true);
                var itemFilter = Builders<BsonDocument>.Filter.Eq("owner", (long)0);
                var rarityFilter = Builders<BsonDocument>.Filter.Gt<double>("rarity", 0);
                var failedAuctionFilter = Builders<BsonDocument>.Filter.Lte<Int32>("failedAuctions", 3);
                itemFilter &= rarityFilter;
                itemFilter &= failedAuctionFilter;

                List<BsonDocument> itemList = itemCollection.Find(itemFilter).ToListAsync().Result;
                List<BsonDocument> serverList = serverCollection.Find(serverFilter).ToListAsync().Result;
                int rServer = r.Next(0, serverList.Count);
                int rItem = r.Next(0, itemList.Count);

                BsonDocument house = serverList[rServer];
                BsonDocument item ;
                if (itemList.Count == 0)
                {
                    Console.WriteLine("No cards found for auction");
                    return;
                }
                if (itemList.Count == 1)
                {
                    item = itemList[0];
                }
                else
                {
                    item = itemList[rItem];
                }
                int auctionid = pinnedDoc.GetValue("totalAuctions").AsInt32 + 1;

                BsonDocument newDoc = new BsonDocument()
                {
                {"auctionId",auctionid },
                {"itemId", item.GetValue("_id").AsObjectId},
                {"serverId",house.GetValue("gid").AsInt64 },
                {"announcementMessage",(long)0 },
                {"active",true },
                {"currentBid",0 },
                {"bids", new BsonArray() },
                {"participantes", new BsonArray() }
                };

                await auctionCollection.InsertOneAsync(newDoc);

                var addAuctionIdUpdate = Database.createUpdateSet("totalAuctions", auctionid);
                await auctionCollection.UpdateOneAsync(pinnedFilter, addAuctionIdUpdate);


                SocketTextChannel channel = Program._client.GetChannel(ulong.Parse(house.GetValue("auctionChannel").AsInt64.ToString())) as SocketTextChannel;

                var embed = Card.createCard(item);

                // sending the message
                TimestampTag timestamp = new() { Time = DateTime.Now.AddMilliseconds(600000), Style = TimestampTagStyles.Relative };
                SocketTextChannel auctionChannel = Program._client.GetChannel(1056956035849535508) as SocketTextChannel;
                await auctionChannel.SendMessageAsync($"@here\n**New Auction**\n\nAuction House: {channel.Guild.Name}\nInvite Link: <{channel.CreateInviteAsync(3600).Result.Url}>\n\nAuction Starts {timestamp}\n\nAuction Card:", embed: embed.Build());
                RestUserMessage message = await channel.SendMessageAsync("@here\n🔴This server is selected as the Auction House for the following card.🔴\nThe auction will start " + timestamp.ToString(), embed: embed.Build());

                var auctionFilter = Builders<BsonDocument>.Filter.Eq("auctionId", auctionid);

                Thread.Sleep(600000);
                // after the perticular duration the auction will start
                TimestampTag endtimestamp = new() { Time = DateTime.Now.AddMilliseconds(1800000), Style = TimestampTagStyles.Relative };
                RestUserMessage Endmessage = await channel.SendMessageAsync("**The auction is ending **" + endtimestamp.ToString());

                await processAuction(message, Endmessage, auctionid, item, auctionFilter, auctionCollection, userCollection, itemCollection, embed);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\n" + e.StackTrace);
            }
        }
        public static async Task processAuction(RestUserMessage message, RestUserMessage Endmessage, int auctionId, BsonDocument item, FilterDefinition<BsonDocument> auctionFilter, IMongoCollection<BsonDocument> auctionCollection, IMongoCollection<BsonDocument> userCollection, IMongoCollection<BsonDocument> itemCollection, EmbedBuilder card)
        {
            try
            {
                ComponentBuilder button = new ComponentBuilder().WithButton("Bid", "bidButton_" + auctionId);
                await message.ModifyAsync(message => { message.Content = "Now, commencing the auction for this following card.\n**Click on Bid buttom below**, if you wish to bid for this card.\nBidding Amount: 0🪙"; message.Components = button.Build(); });
                // after this the Bid Model handles the auction and the thread is put to sleep until the auction is over
                Thread.Sleep(1800000);
                //the auction must be set inactive so no bid can be accepted after this
                await setActive(auctionFilter, auctionCollection);
                // now auction is over so doing all database updates
                await Endmessage.DeleteAsync();
                
                var itemFilter = Database.getItemFilter(item.GetValue("_id").AsObjectId);
                
                BsonDocument endItem = itemCollection.Find(itemFilter).FirstAsync().Result;              
                BsonDocument auction = auctionCollection.Find(auctionFilter).FirstAsync().Result;
                
                int endBid = auction.GetValue("currentBid").AsInt32;
                if (endBid == 0)
                {
                    // reserverd code for later use
                    /*int failedAuctions = endItem.GetValue("failedAuctions").AsInt32;
                    var failedCardCreator = await Program._client.GetUserAsync(ulong.Parse(endItem.GetValue("creator").AsInt64.ToString()));
                    if (failedAuctions + 1 >= 3)
                    {
                        await failedCardCreator.SendMessageAsync("Nobody bid on your following card in recent auction.\nYour card failed in auction total of 3 times, therefore it will not be auctioned again.",embed:card.Build());
                    }
                    else
                    {
                        await failedCardCreator.SendMessageAsync($"Nobody bid on your following card in recent auction.\nYour card failed in auction total of {failedAuctions + 1} times. If your card fails in auction 3 times, it will be removed from auction list.", embed: card.Build());
                    }
                    var failedAuctionUpdate = Builders<BsonDocument>.Update.Inc("failedAuctions", 1);
                    await itemCollection.UpdateOneAsync(itemFilter, failedAuctionUpdate);*/
                    await message.ModifyAsync(message => { message.Content = "Auction has ended.\nResult: No one bid for this card."; message.Components = null; });
                    return;
                }
                long winnerUserId = auction.GetValue("participantes").AsBsonArray.Last().AsInt64;
                long creatorUserId = endItem.GetValue("creator").AsInt64;
                IUser winner = Program._client.GetUserAsync(ulong.Parse(winnerUserId.ToString())).Result;
                IUser creator = Program._client.GetUserAsync(ulong.Parse(creatorUserId.ToString())).Result;
        
                await message.ModifyAsync(message => { message.Content = $"@here\nCongrats to {winner.Mention} for winning this auction.\nThe highest bid was {endBid}🪙"; message.Components = null;});
                
                //item updates
                var itemownerUpdate = Database.createUpdateSet("owner", winnerUserId);
                await itemCollection.UpdateOneAsync(itemFilter, itemownerUpdate); // item owner update
                var itempriceUpdate = Database.createUpdateSet("price", endBid);
                await itemCollection.UpdateOneAsync(itemFilter, itempriceUpdate); // the price update
                                                                                  

                //user updates
                var winnerUserFilter = Database.getUserFilter(winnerUserId);
                var userOwnUpdate = Builders<BsonDocument>.Update.Push("cardListOwned", endItem.GetValue("_id").AsObjectId);
                await userCollection.UpdateOneAsync(winnerUserFilter, userOwnUpdate);
                
                
                BsonDocument winnerUserData = userCollection.Find(winnerUserFilter).FirstAsync().Result;
                int creatorCut;
                string ownerMessage;
                // if the winner is also creator, he will only get 30% of winning amount
                // if not winner gets 70%, rest money goes to pool
                if (winnerUserId == creatorUserId)
                {
                    creatorCut = (endBid * 30) / 100;
                    ownerMessage = $"You have bought the following card for {endBid}🪙 in recent auction.\nOnly 30% of your cash has been credited to your account because you are also the creator of this card.\nAdded Cash: {creatorCut}🪙\n\nWondering where the 70% go? Read /help.";
                }
                else
                {
                    creatorCut = (endBid * 70) / 100;
                    ownerMessage = $"Your following item has recently been sold for ${endBid}🪙 in a recent auction.\n70% of that cash has been credited to your account.\nAdded Cash: {creatorCut}🪙\n\nWondering where the 30% go? See the /help.";
                }

                //user updates
                var cashUpdate = Database.createUpdateSet("cash", (winnerUserData.GetValue("cash").AsInt32 - endBid));
                await userCollection.UpdateOneAsync(winnerUserFilter, cashUpdate); // subtracting bid ammunt from bid winner

                var creatorUserFilter = Database.getUserFilter(creatorUserId);
                var creatorCashUpdate = Builders<BsonDocument>.Update.Inc("cash", creatorCut); // giving creator his cut
                await userCollection.UpdateOneAsync(creatorUserFilter, creatorCashUpdate);
                await creator.SendMessageAsync(ownerMessage,embed:card.Build()); // sending dm to owner

                var pinDoc = Database.getPinFilter();
                BsonDocument pool = userCollection.Find(pinDoc).FirstAsync().Result;
                var poolUpdate = Builders<BsonDocument>.Update.Inc("moneyPool", endBid - creatorCut);
                await userCollection.UpdateOneAsync(pinDoc, poolUpdate); // updating moneypool
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public static async Task setActive(FilterDefinition<BsonDocument> auctionFilter, IMongoCollection<BsonDocument> auctionCollection)
        {
            var updateSetActive = Database.createUpdateSet("active", false);
            await auctionCollection.UpdateOneAsync(auctionFilter, updateSetActive);
        }
        public static async Task Bid(SocketMessageComponent component)
        {
            var modal = new ModalBuilder().WithTitle("Bid").WithCustomId($"bidModal_{component.Data.CustomId.Split("_")[1]}").AddTextInput("Bid Amount","userBid" , placeholder: "An integer value ranging from current bid amount to whatever amount you wish to bid");
            await component.RespondWithModalAsync(modal.Build());
        } // just sending a button
        public static async Task BidHandle(SocketModal modal, IMongoCollection<BsonDocument> auctionCollection, IMongoCollection<BsonDocument> userCollection)
        {
            await modal.DeferAsync();
            int auctionID = int.Parse(modal.Data.CustomId.Split("_")[1]);

            var auctionFilter = Database.getAuctionFilter(auctionID);
            BsonDocument auctionDoc = auctionCollection.Find(auctionFilter).FirstAsync().Result;
            if (!auctionDoc.GetValue("active").AsBoolean)
            {
                await modal.FollowupAsync("This auction is already over.", ephemeral: true);
                return;
            }
            int currentBid = auctionDoc.GetValue("currentBid").AsInt32;


            if (int.TryParse(modal.Data.Components.First(x=> x.CustomId == "userBid").Value,out int bid))
            {
                BsonDocument userdata = await Database.getUserData((long)modal.User.Id, userCollection);
                if (userdata.GetValue("cash").AsInt32 < bid)
                {
                    await modal.FollowupAsync("You do not have sufficient cash to bid this high.\nCash: " + userdata.GetValue("cash").AsInt32, ephemeral: true);
                    return;
                }
                if (bid > currentBid)
                {
                    var bidUpdate = Database.createUpdateSet("currentBid", bid);
                    var bidsListUpdate = Builders<BsonDocument>.Update.Push("bids", bid);
                    var participateListUpdate = Builders<BsonDocument>.Update.Push("participantes", modal.User.Id);

                    await auctionCollection.UpdateOneAsync(auctionFilter, bidUpdate);
                    await auctionCollection.UpdateOneAsync(auctionFilter, bidsListUpdate);
                    await auctionCollection.UpdateOneAsync(auctionFilter, participateListUpdate);

                    await modal.FollowupAsync("You have successfully bid for this card.\nBid Amount: " + bid, ephemeral: true);

                    await modal.ModifyOriginalResponseAsync(m => m.Content = $"**Auction**\nCurrent Bid: {bid}🪙 \nBidder: {modal.User.Mention}");
                }
                else
                {
                    await modal.FollowupAsync("The bid amount is less than current bid",ephemeral:true);
                }
                
            }
            else
            {
                await modal.FollowupAsync("Invalid bid amount",ephemeral:true);
            }
        }
    }
}
