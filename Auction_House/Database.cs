using Discord;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Auction_Dbot.Auction_House
{
    public class Database
    {
        public static MongoClientSettings stgs;
        public static MongoClient client;
        public static IMongoDatabase database;
        public static void Connect()
        {
            string dbtoken = Environment.GetEnvironmentVariable("dbConString");
            var settings = MongoClientSettings.FromConnectionString(dbtoken);//.FromConnectionString(Environment.GetEnvironmentVariable("dbToken"));
            stgs = settings;
            var dbclient = new MongoClient(settings);
            client = dbclient;
            var db = dbclient.GetDatabase("AuctionHouse");
            database = db;
            Console.WriteLine("Database connected!");
        }

        public static async Task<bool> IsDocExistsAsync(IMongoCollection<BsonDocument> collection, long ID)
        {
            var documents = await collection.Find(new BsonDocument()).ToListAsync();
            foreach (var document in documents)
            {
                if (document.ContainsValue(ID))
                {
                    return true;
                }
            }
            return false;
        }

        public static Task AddUser(SocketUser socketuser, IMongoCollection<BsonDocument> collection)
        {
            var newDoc = new BsonDocument()
            {
                {"userid",(long)socketuser.Id },
                {"name",socketuser.Username + "#" + socketuser.DiscriminatorValue.ToString() },
                {"cardCreationInProgress",false },
                {"cardInCompleteId",new ObjectId() },
                {"totalCreateCardToday",0 },
                {"canCreateCardUntill", DateTime.Now},
                {"totalRatesToday",0 },
                {"rateUntil",DateTime.Now },
                {"dailyLogin",DateTime.Now },
                {"pingRole",(long)0 },
                {"payoutRate", 0.2},
                {"cardListCreated", new BsonArray() },
                {"cardListOwned", new BsonArray()},
                {"cash", 1000},
            };
            return collection.InsertOneAsync(newDoc);            
        }
        public static Task AddServer(SocketGuild guild, IMongoCollection<BsonDocument> collection)
        {
            try
            {
                var defaultguild = guild.DefaultChannel;
                var newDoc = new BsonDocument()
                {
                    {"gid",(long)guild.Id },
                    {"gname",guild.Name },
                    {"isAuctionHouseOn",true },
                    {"inviteLink",defaultguild.CreateInviteAsync(null).Result.Url },
                    {"auctionChannel",(long)defaultguild.Id },
                    {"isServer",true }
                };
                
                return collection.InsertOneAsync(newDoc);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }

        }

        public static async Task CheckAndAddServer(SocketGuild guild, IMongoCollection<BsonDocument> serverCollection)
        {
           
            if (!Cache.serverList.Contains(guild.Id))
            {
                Cache.serverList.Add(guild.Id);
                if (!IsDocExistsAsync(serverCollection, (long)guild.Id).Result)
                {
                    var serverSettings = new SlashCommandBuilder();
                    serverSettings.WithName("server_settings");
                    serverSettings.WithDescription("Change/See server settings");
                    serverSettings.WithDefaultMemberPermissions(GuildPermission.Administrator);

                    SocketApplicationCommand cmd = await guild.CreateApplicationCommandAsync(serverSettings.Build());
                    await AddServer(Program._client.GetGuild(guild.Id), serverCollection);
                }
            }
        }
        public static async Task CheckAndAddUser(SocketUser user, IMongoCollection<BsonDocument> userCollection)
        {
            if (!Cache.userList.Contains(user.Id))
            {
                Cache.userList.Add(user.Id);
                if (!IsDocExistsAsync(userCollection, (long)user.Id).Result)
                {
                    await AddUser(user, userCollection);
                }
            }
        }
        public static async Task<BsonDocument> getServerData(SocketSlashCommand cmd, IMongoCollection<BsonDocument> collection)
        {
            return await collection.Find(getServerFilter((long)cmd.GuildId)).FirstAsync();
        }
        public static async Task<BsonDocument> getUserData(long userid, IMongoCollection<BsonDocument> collection)
        {
            return await collection.Find(getUserFilter(userid)).FirstAsync();
        }
        public static async Task<BsonDocument> getItemData(ObjectId id, IMongoCollection<BsonDocument> collection)
        {
            return await collection.Find(getItemFilter(id)).FirstAsync();
        }
        public static async Task<BsonDocument> getItemData(string name, IMongoCollection<BsonDocument> collection)
        {
            return await collection.Find(getItemFilter(name.ToLower())).FirstAsync();
        }
        public static FilterDefinition<BsonDocument> getServerFilter(long ID)
        {
            return Builders<BsonDocument>.Filter.Eq("gid", ID);
        }
        public static FilterDefinition<BsonDocument> getUserFilter(long ID)
        {
            return Builders<BsonDocument>.Filter.Eq("userid", ID);
        }
        public static FilterDefinition<BsonDocument> getItemFilter(ObjectId id)
        {
            return Builders<BsonDocument>.Filter.Eq("_id", id);
        }
        public static FilterDefinition<BsonDocument> getItemFilter(string name)
        {
            return Builders<BsonDocument>.Filter.Eq("cardNameLower", name.ToLower());
        }
        public static FilterDefinition<BsonDocument> getPinFilter()
        {
            return Builders<BsonDocument>.Filter.Eq("isPinned", true);
        }
        public static FilterDefinition<BsonDocument> getAuctionFilter(int auctionId)
        {
            return Builders<BsonDocument>.Filter.Eq("auctionId", auctionId);
        }
        public static IMongoCollection<BsonDocument> getCollection(string str)
        {
            return database.GetCollection<BsonDocument>(str);
        }
        public static UpdateDefinition<BsonDocument> createUpdateSet(string attribute, object value)
        {
            return Builders<BsonDocument>.Update.Set(attribute, value);
        }
    }
}
