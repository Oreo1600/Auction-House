using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auction_Dbot.Auction_House.Commands
{
    public class SetRolePing
    {
        public static async Task Execute(SocketSlashCommand cmd, IMongoCollection<BsonDocument> collection)
        {
            SocketRole role = cmd.Data.Options.First().Value as SocketRole;
            var filter = Database.getServerFilter((long)cmd.GuildId);
            var update = Database.createUpdateSet("mentionRole", role.Mention);
            await collection.UpdateOneAsync(filter, update);

            await cmd.RespondAsync("Successfully set the role" + role.Mention + " as ping role for auctions",ephemeral:true);
        }
    }
}
