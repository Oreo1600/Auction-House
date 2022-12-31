using MongoDB.Bson;
using MongoDB.Driver;
using System.Timers;

namespace Auction_Dbot.Auction_House
{
    public class MoneyPool
    {
        public static async void MoneyPoolReset(object sender, ElapsedEventArgs e)
        {
            var userCollection = Database.getCollection("Users");
            var pinFilter = Database.getPinFilter();

            BsonDocument pool = userCollection.Find(pinFilter).FirstAsync().Result;
            var cashUpdate = Database.createUpdateSet("moneyPool",new Random().Next(30000,70000));
            await userCollection.UpdateOneAsync(pinFilter, cashUpdate);

            var timeUpdate = Database.createUpdateSet("resetTime",DateTime.Now.AddMilliseconds(259200000));
            await userCollection.UpdateOneAsync(pinFilter,timeUpdate);

            BsonArray array = pool.GetValue("recentWithdraws").AsBsonArray;
            var arrayUpdate = Builders<BsonDocument>.Update.PullAll("recentWithdraws", array);
            await userCollection.UpdateOneAsync(pinFilter, arrayUpdate);

            var usersFilter = Builders<BsonDocument>.Filter.Gt<Int64>("userid", 0);
            var rateFilter = Database.createUpdateSet("payoutRate", 0.5);
            UpdateResult result = await userCollection.UpdateManyAsync(usersFilter, rateFilter);
            
            Console.WriteLine(result.ModifiedCount+ " user's rates are changed 0.5");
        }
        public static async void MoneyPoolReset()
        {
            var userCollection = Database.getCollection("Users");
            var pinFilter = Database.getPinFilter();

            BsonDocument pool = userCollection.Find(pinFilter).FirstAsync().Result;
            var cashUpdate = Database.createUpdateSet("moneyPool",new Random().Next(30000,70000));
            await userCollection.UpdateOneAsync(pinFilter, cashUpdate);

            var timeUpdate = Database.createUpdateSet("resetTime",DateTime.Now.AddMilliseconds(259200000));
            await userCollection.UpdateOneAsync(pinFilter,timeUpdate);


            BsonArray array = pool.GetValue("recentWithdraws").AsBsonArray;
            var arrayUpdate = Builders<BsonDocument>.Update.PullAll("recentWithdraws", array);
            await userCollection.UpdateOneAsync(pinFilter, arrayUpdate);

            var usersFilter = Builders<BsonDocument>.Filter.Gt<Int64>("userid", 0);
            var rateFilter = Database.createUpdateSet("payoutRate", 0.1);
            await userCollection.UpdateManyAsync(usersFilter, rateFilter);
        }
        public static async void IncreaseRate(object sender, ElapsedEventArgs e)
        {
            var userCollection = Database.getCollection("Users");
            var usersFilter = Builders<BsonDocument>.Filter.Gt<Int64>("userid", 0);
            var rateUpdate = Builders<BsonDocument>.Update.Inc("payoutRate", 0.2);
            userCollection.UpdateManyAsync(usersFilter,rateUpdate);
        }
        public static async void IncreaseRate()
        {
            var userCollection = Database.getCollection("Users");
            var usersFilter = Builders<BsonDocument>.Filter.Gt<Int64>("userid", 0);
            var rateUpdate = Builders<BsonDocument>.Update.Inc("payoutRate", 0.2);
            userCollection.UpdateManyAsync(usersFilter,rateUpdate);
        }
    }
}
