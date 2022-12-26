using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auction_Dbot.Auction_House
{
    public class Cache
    {
        public static List<ulong> serverList = new List<ulong>();
        public static List<ulong> userList = new List<ulong>();

        public static List<String> allowedContentType = new List<string> { "image/jpeg", "image/webp", "image/png", "image/jpg" };
        public static List<String> allowedContentTypeExt = new List<string> { ".jpeg", ".webp", ".png", ".jpg" };

    }
}
