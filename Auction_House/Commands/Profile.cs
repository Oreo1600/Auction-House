using Discord;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Reactive.Linq;


namespace Auction_Dbot.Auction_House.Commands
{
    public class Profile
    {
        public static async Task Execute(SocketSlashCommand cmd, IMongoCollection<BsonDocument> collection)
        {
            try
            {
                if (cmd.Data.Options.Count != 0) // if a user account is available in the options
                {
                    SocketUser user = cmd.Data.Options.First().Value as SocketUser;
                    //checking if the account is actually a bot
                    if (user.IsBot)
                    {
                        await cmd.RespondAsync("This account belongs to a bot", ephemeral: true);
                        return;
                    }
                    var embed = profileEmbedBuilder(user, collection);
                    if (embed == null)
                    {
                        await cmd.RespondAsync("This user doesn't play auction house somehow.", ephemeral: true);
                        return;
                    }
                    var buttonTP = new ComponentBuilder()
                        .WithButton("Owned Cards", $"owned_{cmd.User.Id}_{user.Id}")
                        .WithButton("Created Cards", $"create_{cmd.User.Id}_{user.Id}");

                    await cmd.RespondAsync(embed: embed.Build(), components: buttonTP.Build());
                    return;
                }
                // if not send profile of user who sent the command
                SocketUser socketUser = cmd.User;
                var button = new ComponentBuilder()
                        .WithButton("Owned Cards", $"owned_{cmd.User.Id}_{socketUser.Id}")
                        .WithButton("Created Cards", $"create_{cmd.User.Id}_{socketUser.Id}");
                var embedBuilder = profileEmbedBuilder(socketUser, collection);
                await cmd.RespondAsync(embed: embedBuilder.Build(), components: button.Build());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + e.StackTrace);
            }
            

        }
        public static EmbedBuilder profileEmbedBuilder(SocketUser socketUser, IMongoCollection<BsonDocument> collection,ulong userid = 0)
        {
            try
            {
                if (socketUser == null)
                {
                    BsonDocument userData = Database.getUserData((long)userid, collection).Result;
                    int cash = userData.GetValue("cash").AsInt32;
                    int createdCards = userData.GetValue("cardListCreated").AsBsonArray.Count();
                    int ownedCards = userData.GetValue("cardListOwned").AsBsonArray.Count();
                    double rate = userData.GetValue("payoutRate").AsDouble;
                    string desc = $"🆔 UserID: {userid}\n\n💰 Cash: {cash}🪙\n\n🃏 Total owned cards: {ownedCards}\n\n🀄 Total created cards: {createdCards}\n\n🎱 Current Payout Rate: {Math.Round(rate, 2)}%";
                    var embed = new EmbedBuilder()
                        .WithAuthor("Profile")
                        .WithTitle(userData.GetValue("name").AsString)
                        .WithDescription(desc)
                        .WithColor(Color.Teal)
                        .WithCurrentTimestamp();

                    return embed;
                }
                else
                {
                    BsonDocument userData = Database.getUserData((long)socketUser.Id, collection).Result;
                    int cash = userData.GetValue("cash").AsInt32;
                    int createdCards = userData.GetValue("cardListCreated").AsBsonArray.Count();
                    int ownedCards = userData.GetValue("cardListOwned").AsBsonArray.Count();
                    double rate = userData.GetValue("payoutRate").AsDouble;
                    string desc = $"🆔 UserID: {socketUser.Id}\n\n💰 Cash: {cash}🪙\n\n🀄 Total created cards: {createdCards}\n\n🃏 Total owned cards: {ownedCards}\n\n🎱 Current Payout Rate: {Math.Round(rate, 2)}%";
                    var embed = new EmbedBuilder()
                        .WithAuthor("Profile")
                        .WithTitle(socketUser.Username + "#" + socketUser.Discriminator)
                        .WithDescription(desc)
                        .WithColor(Color.Teal)
                        .WithImageUrl(socketUser.GetAvatarUrl())
                        .WithCurrentTimestamp();

                    return embed;
                }
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + e.StackTrace);
                if (e.Message == "One or more errors occurred. (Sequence contains no elements)")
                {
                    return null;
                }
                else
                {
                    Console.WriteLine(e.Message + e.StackTrace);
                }
                throw;
            }
            
        }

        public static (ComponentBuilder cmp, string desc) createCardProfileEmbed(BsonArray cardList, IMongoCollection<BsonDocument> itemCollection, ulong userid, int startAT, string cardType, SocketUser interactor)
        {
            string desc = "";
            var buttons = new ComponentBuilder();
            if (cardList.Count == 0) // if the card list has 0 cards display none
            {
                desc = "No cards found!";
            }
            else if (cardList.Count <= 10) // if card list has less than 10 cards we wont be adding next button
            {
                for (int i = 0; i < cardList.Count; i++)
                {
                    BsonDocument itemData = Database.getItemData(cardList[i].AsObjectId, itemCollection).Result;
                    desc = desc + (i + 1) + ". " + itemData.GetValue("cardName") + "\n";
                    buttons.WithButton((i + 1).ToString(), "cardButton_" + interactor.Id + "_" + itemData.GetValue("_id"));
                }
            }
            else // in case card list has more than 10 cards we will add next button
            {
                int i;
                bool skipNext = false;
                for (i = startAT; i <= startAT + 9; i++)
                {
                    try
                    {
                        BsonDocument itemData = Database.getItemData(cardList[i - 1].AsObjectId, itemCollection).Result;
                        desc = desc + i + ". " + itemData.GetValue("cardName") + "\n";
                        buttons.WithButton(i.ToString(), "cardButton_" + interactor.Id + "_" + itemData.GetValue("_id"));
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
                    buttons.AddRow(new ActionRowBuilder().WithButton("Next>>", $"{cardType}_" + interactor.Id + "_" + (i) + "_" + userid));
                }
            }
            
            return (buttons, desc);
        }
        // if owned card button is clicked. customId = owned_cmdUserID_profileUserID
        public static async Task ownedCards(SocketMessageComponent component, IMongoCollection<BsonDocument> userCollection, IMongoCollection<BsonDocument> itemCollection)
        {
            try
            {
                ulong userid = ulong.Parse(component.Data.CustomId.Split("_")[2]);
                SocketUser user = Program._client.GetUser(userid);
                BsonDocument userData = Database.getUserData((long)userid, userCollection).Result;
                BsonArray ownedCard = userData.GetValue("cardListOwned").AsBsonArray;

                if (user == null)
                {
                    (ComponentBuilder buttons, string desc) = createCardProfileEmbed(ownedCard, itemCollection, userid, 1, "nextButtonOwned", component.User);
                    var embed = new EmbedBuilder()
                    .WithAuthor(userData.GetValue("name").AsString)
                    .WithTitle("Owned Cards")
                    .WithDescription(desc)
                    .WithColor(Color.DarkTeal)
                    .WithCurrentTimestamp();

                    await component.UpdateAsync(x => { x.Embed = embed.Build(); x.Components = buttons.Build(); });
                    return;
                }
                else
                {
                    (ComponentBuilder buttons, string desc) = createCardProfileEmbed(ownedCard, itemCollection, user.Id, 1, "nextButtonOwned", component.User);
                    var embed = new EmbedBuilder()
                    .WithAuthor(user.Username + "#" + user.Discriminator)
                    .WithTitle("Owned Cards")
                    .WithDescription(desc)
                    .WithColor(Color.DarkTeal)
                    .WithCurrentTimestamp();

                    await component.UpdateAsync(x => { x.Embed = embed.Build(); x.Components = buttons.Build(); });
                    return;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + e.StackTrace);
            }
        }

        // if created cards button is clicked. customId = created_cmdUserID_profileUserID
        public static async Task createdCards(SocketMessageComponent component, IMongoCollection<BsonDocument> userCollection, IMongoCollection<BsonDocument> itemCollection)
        {
            try
            {
                ulong userid = ulong.Parse(component.Data.CustomId.Split("_")[2]);
                SocketUser user = Program._client.GetUser(userid);
                BsonDocument userData = Database.getUserData((long)userid, userCollection).Result;
                BsonArray createdCard = userData.GetValue("cardListCreated").AsBsonArray;


                if (user == null)
                {
                    (ComponentBuilder buttons, string desc) = createCardProfileEmbed(createdCard, itemCollection, userid, 1, "nextButtonCreated", component.User);
                    var embed = new EmbedBuilder()
                    .WithAuthor(userData.GetValue("name").AsString)
                    .WithTitle("Created Cards")
                    .WithDescription(desc)
                    .WithColor(Color.DarkTeal)
                    .WithCurrentTimestamp();

                    await component.UpdateAsync(x => { x.Embed = embed.Build(); x.Components = buttons.Build(); });
                    return;
                }
                else
                {
                    (ComponentBuilder buttons, string desc) = createCardProfileEmbed(createdCard, itemCollection, user.Id, 1, "nextButtonCreated", component.User);
                    var embed = new EmbedBuilder()
                    .WithAuthor(user.Username + "#" + user.Discriminator)
                    .WithTitle("Created Cards")
                    .WithDescription(desc)
                    .WithColor(Color.DarkTeal)
                    .WithCurrentTimestamp();

                    await component.UpdateAsync(x => { x.Embed = embed.Build(); x.Components = buttons.Build(); });
                    return;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + e.StackTrace);
                
            }
            
        }
        // if a card button is clicked, get the card info. customid = cardButton_cmdUserId_itemId
        public static async Task cardButtonClicked(SocketMessageComponent component, IMongoCollection<BsonDocument> itemCollection, IMongoCollection<BsonDocument> userCollection)
        {
            ObjectId itemid = ObjectId.Parse(component.Data.CustomId.Split("_")[2]);
            BsonDocument itemData = Database.getItemData(itemid, itemCollection).Result;

            bool isNsfw = true;
            if (component.Channel is IDMChannel) { isNsfw = false; }
            else if (component.Channel is SocketTextChannel)
            {
                SocketTextChannel channel = component.Channel as SocketTextChannel;
                isNsfw = channel.IsNsfw;
            }
            var embed = Card.createCard(itemData,userCollection,isNsfw);

            await component.UpdateAsync(x => { x.Embed = embed.Build(); });
        }
        
        // if the next or previous button is clicked
        // customID = cardtype_cmdUserId_startsWith_userId

        // card types are nextButtonCreated, previousButtonCreated, nextButtonOwned, previousButtonOwned
        // which is passed in createCardProfileEmbed function
        // starts with an integer number from which the list should starts with
        // for example if the startAt is 0 then next is clicked startAt would be 11 so list is shown from 11.
        public static async Task nextOrPrevButtonClicked(SocketMessageComponent component, IMongoCollection<BsonDocument> userCollection, IMongoCollection<BsonDocument> itemCollection, string cardType)
        {
            if (cardType == "nextButtonCreated" || cardType == "previousButtonCreated")
            {
                ulong userId = ulong.Parse(component.Data.CustomId.Split("_")[3]);
                int startAt = int.Parse(component.Data.CustomId.Split("_")[2]);
                SocketUser user = Program._client.GetUser(userId);
                if (user == null)
                {
                    BsonDocument userData = Database.getUserData((long)userId, userCollection).Result;
                    BsonArray createdCard = userData.GetValue("cardListCreated").AsBsonArray;
                    (ComponentBuilder buttons, string desc) = createCardProfileEmbed(createdCard, itemCollection, userId, startAt, cardType, component.User);
                    // if startAt is not 1 means its not the first page then add previous button
                    if (startAt != 1)
                    {
                        buttons.AddRow(new ActionRowBuilder().WithButton("<<Previous", $"previousButtonCreated_{component.User.Id}_" + (startAt - 10) + "_" + userId));
                    }
                    var embed = new EmbedBuilder()
                    .WithAuthor(userData.GetValue("name").AsString)
                    .WithTitle("Created Cards")
                    .WithDescription(desc)
                    .WithColor(Color.DarkTeal)
                    .WithCurrentTimestamp();

                    await component.UpdateAsync(x => { x.Embed = embed.Build(); x.Components = buttons.Build(); });
                    return;
                }
                else
                {
                    BsonDocument userData = Database.getUserData((long)user.Id, userCollection).Result;
                    BsonArray createdCard = userData.GetValue("cardListCreated").AsBsonArray;
                    (ComponentBuilder buttons, string desc) = createCardProfileEmbed(createdCard, itemCollection, user.Id, startAt, cardType, component.User);
                    // if startAt is not 1 means its not the first page then add previous button
                    if (startAt != 1)
                    {
                        buttons.AddRow(new ActionRowBuilder().WithButton("<<Previous", $"previousButtonCreated_{component.User.Id}_" + (startAt - 10) + "_" + user.Id));
                    }
                    var embed = new EmbedBuilder()
                    .WithAuthor(user.Username + "#" + user.Discriminator)
                    .WithTitle("Created Cards")
                    .WithDescription(desc)
                    .WithColor(Color.DarkTeal)
                    .WithCurrentTimestamp();

                    await component.UpdateAsync(x => { x.Embed = embed.Build(); x.Components = buttons.Build(); });
                    return;
                }
                
            }
            // same for owned list
            else if (cardType == "nextButtonOwned" || cardType == "previousButtonOwned")
            {
                ulong userId = ulong.Parse(component.Data.CustomId.Split("_")[3]);
                int startAt = int.Parse(component.Data.CustomId.Split("_")[2]);
                SocketUser user = Program._client.GetUser(userId);
                if (user == null)
                {                    
                    BsonDocument userData = Database.getUserData((long)userId, userCollection).Result;
                    BsonArray ownedCard = userData.GetValue("cardListOwned").AsBsonArray;
                    (ComponentBuilder buttons, string desc) = createCardProfileEmbed(ownedCard, itemCollection, userId, startAt, "nextButtonOwned", component.User);
                    if (startAt != 1)
                    {
                        buttons.WithButton("<<Previous", $"previousButtonOwned_{component.User.Id}_" + (startAt - 10) + "_" + userId);
                    }
                    var embed = new EmbedBuilder()
                        .WithAuthor(userData.GetValue("name").AsString)
                        .WithTitle("Owned Cards")
                        .WithDescription(desc)
                        .WithColor(Color.DarkTeal)
                        .WithCurrentTimestamp();

                    await component.UpdateAsync(x => { x.Embed = embed.Build(); x.Components = buttons.Build(); });
                    return;
                }
                else
                {
                    BsonDocument userData = Database.getUserData((long)user.Id, userCollection).Result;
                    BsonArray ownedCard = userData.GetValue("cardListOwned").AsBsonArray;
                    (ComponentBuilder buttons, string desc) = createCardProfileEmbed(ownedCard, itemCollection, user.Id, startAt, "nextButtonOwned", component.User);
                    if (startAt != 1)
                    {
                        buttons.WithButton("<<Previous", $"previousButtonOwned_{component.User.Id}_" + (startAt - 10) + "_" + user.Id);
                    }
                    var embed = new EmbedBuilder()
                        .WithAuthor(user.Username + "#" + user.Discriminator)
                        .WithTitle("Owned Cards")
                        .WithDescription(desc)
                        .WithColor(Color.DarkTeal)
                        .WithCurrentTimestamp();

                    await component.UpdateAsync(x => { x.Embed = embed.Build(); x.Components = buttons.Build(); });
                    return;
                }
            }
        }
    } // class ends here
} // namespace ends here
