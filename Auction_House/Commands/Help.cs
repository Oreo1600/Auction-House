
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
                "\n\n**Money Pool**\nMoney pool is a global pool which resets every 3 days. Everyone has a payout rate, that percentage of cash can be withdrawn from pool at any time." +
                " Once a pool resets your payout rate also resets. Payout rate increases 0.5% every hour; it can be increased further by creating or rating cards.\nCreating a card increases your payout rate by **0.2%** and rating a card increases your payout rate by **0.05%**." +
                "\n\nEnjoy the Auction House!" +
                "\n\n**Commands**" +
                "\n`/create` - Create a card" +
                "\n`/card` - Search for a card" +
                "\n`/rate` - decide rarity of a random card" +
                "\n`/pool` - Withdraw cash from Money Pool" +
                "\n`/profile` - See your profile including all the cards you have created or owned" +
                "\n`/leaderboard` - See global leaderboard" +
                "\n`/server_settings` - Edit server settings (admin only)" +
                "\n`/set_ping_role` - Set a role that bot should ping when an auction appear in the server (admin only)" +
                "\n`/help` - Know how to use the bot" +
                "\n\nJoin the support server: https://discord.gg/4ENS22mNxx";
            var embedBuiler = new EmbedBuilder()
                .WithAuthor(cmd.User.Username+"#" + cmd.User.DiscriminatorValue)
                .WithTitle("Help")
                .WithDescription(help)
                .WithColor(Color.Green)
                .WithCurrentTimestamp();
            cmd.RespondAsync(ephemeral:true,embed:embedBuiler.Build());
            return Task.CompletedTask;
        }
    }
}
