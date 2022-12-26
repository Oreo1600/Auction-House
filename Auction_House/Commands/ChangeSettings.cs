using Discord;
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
    public class ChangeSettings
    {
        public static async Task Execute(SocketSlashCommand cmd, IMongoCollection<BsonDocument> serverCollection)
        {
            var serverFilter = Builders<BsonDocument>.Filter.Eq("gid", (long)cmd.GuildId);
            BsonDocument serverdb = await serverCollection.Find(serverFilter).FirstAsync();
            var server = Program._client.GetGuild(cmd.GuildId.Value);
            long defaultChannel = serverdb.GetValue("auctionChannel").AsInt64;
            SocketTextChannel channel = Program._client.GetChannel(ulong.Parse(defaultChannel.ToString())) as SocketTextChannel;
            string inviteLink = serverdb.GetValue("inviteLink").AsString;
            string desc = $"**Server Settings**\n\nServer Name: {server.Name + (serverdb.GetValue("isAuctionHouseOn").AsBoolean ? "\nAuction House: On" : "\nAuction House: Off")}\nAuction Channel: {channel.Mention}\nServer Invite: {inviteLink}"+ 
                "\n\n**Meanings:**\nAuction House - Whether this server should be picked as auction house or not, *Click on Toggle Auction House button to turn it ON or OFF*\n" + 
                "Auction Channel - The server channel where auction will be held if the server were to be picked as auction house. *Click on Set Auction Channel button to set current channel as Auction Channel*";           

            var buttons = new ComponentBuilder()
                .WithButton("Toggle Auction House", $"tglAuctionHouse_{cmd.User.Id}_{cmd.GuildId}")
                .WithButton("Set Auction Channel", $"setAuctionChannel_{cmd.User.Id}_{cmd.GuildId}_{cmd.ChannelId}");
            await cmd.RespondAsync(desc, components: buttons.Build());
        }
        // customid = tglAuctionHouse_userid_guildid
        public static async Task toogleAuctionHouse(SocketMessageComponent component,IMongoCollection<BsonDocument> serverCollection)
        {
            await component.DeferAsync();
            var serverFilter = Builders<BsonDocument>.Filter.Eq("gid", long.Parse(component.Data.CustomId.Split("_")[2]));
            var serverData = await serverCollection.Find(serverFilter).FirstAsync();

            if (serverData.GetValue("isAuctionHouseOn").AsBoolean)
            {
                var update = Database.createUpdateSet("isAuctionHouseOn", false);
                await serverCollection.UpdateOneAsync(serverFilter, update);

                string message = component.Message.Content.Replace("On", "Off");
                await component.ModifyOriginalResponseAsync(m => { m.Content = message; });
            }
            else
            {
                var update = Database.createUpdateSet("isAuctionHouseOn", true);
                await serverCollection.UpdateOneAsync(serverFilter, update);

                string message = component.Message.Content.Replace("Off", "On");
                await component.ModifyOriginalResponseAsync(m => { m.Content = message; });
            }
        }
        public static async Task setAuctionHouse(SocketMessageComponent component,IMongoCollection<BsonDocument> serverCollection)
        {
            await component.DeferAsync();
            var serverFilter = Builders<BsonDocument>.Filter.Eq("gid", long.Parse(component.Data.CustomId.Split("_")[2]));
            var serverData = await serverCollection.Find(serverFilter).FirstAsync();           

            var update = Database.createUpdateSet("auctionChannel", long.Parse(component.Data.CustomId.Split("_")[3]));
            await serverCollection.UpdateOneAsync(serverFilter, update);

            string message = component.Message.Content.Replace($"<#{serverData.GetValue("auctionChannel").AsInt64}>", $"<#{component.Data.CustomId.Split("_")[3]}>");
            await component.ModifyOriginalResponseAsync(m => { m.Content = message; });
        }
    }
}
