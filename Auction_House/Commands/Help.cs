﻿
using Discord;
using Discord.WebSocket;

namespace Auction_Dbot.Auction_House.Commands
{
    public static class Help
    {
        public static Task Execute(SocketSlashCommand cmd)
        {
            string help = "**Basics**\nYou can create various cards of different categories. The card can be about any existing entity in this world." +
                "\nYou can earn 500🪙 by creating a card. After your card has been created, it will be evaluated by other people and given an average rarity of user responses." +
                "\nThen, Your card will be auctioned and a owner will be the one who has highest bid in the auction." +
                "\n\n**Auction House**\r\nThe auction house is a server that is chosen for an auction for a particular card. The auction house is chosen at random. You can turn off the auction house for your server from server settings." +
                "\n**Note**: If your server is private and you don't want anyone to join your server, Make sure you disable the auction house from /server_settings command." +
                "\n\n**Money Pool**\nMoney pool is a global pool which resets every 3 days. Everyone has a payout rate, that percentage of cash can be withdrawn from pool at any time." +
                " Once a pool resets your payout rate also resets. Payout rate increases 0.5% every hour; it can be increased further by creating or rating cards.\nCreating a card increases your payout rate by **0.2%** and rating a card increases your payout rate by **0.05%**." +
                "\n\nEnjoy the Auction House!" +
                "\n\n**Commands**" +
                "\n</create:1049238076578344971> - Create a card" +
                "\n</card:1049238079011029023> - Search for a card" +
                "\n</rate:1049238081783468094> - decide rarity of a random card" +
                "\n</pool:1054057929646546974> - Withdraw cash from Money Pool" +
                "\n</profile:1049238083947728906> - See your profile including all the cards you have created or owned" +
                "\n</leaderboard:1059005652363640884> - See global leaderboard" +
                "\n/server_settings - Edit server settings (admin only)" +
                "\n</set_ping_role:1061006721688027236> - Set a role that bot should ping when an auction appear in the server (admin only)" +
                "\n</vote:1063541819901747261> - Vote on top.gg to get nice rewards" +
                "\n</help:1049238073910763521> - Know how to use the bot" +
                "\n\n**To get notified when an auction appear on any server, join our support server.**\nJoin the support server: https://discord.gg/4ENS22mNxx" +
                "\n\nif you find any images that are explicit and not marked as nsfw, please report to the support server.";
            var embedBuiler = new EmbedBuilder()
                .WithAuthor(cmd.User.Username+"#" + cmd.User.DiscriminatorValue)
                .WithTitle("Help")
                .WithDescription(help)
                .WithColor(Color.Green)
                .WithCurrentTimestamp();
            cmd.RespondAsync(embed:embedBuiler.Build());
            return Task.CompletedTask;
        }
    }
}
