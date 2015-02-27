using System;
using System.Data.Services.Client;
using System.Linq;
using System.Net;
using Nexus;
using Nexus.Messages;

namespace Bing
{
    public class Bing : NexusComponent
    {
        private const string bingCommand = "bing";
        private const string bingUri = "https://api.datamarket.azure.com/Bing/Search/v1/";
        private const string accountKey = "9qNH2/3GdYgK/8z/VxhCJogo+BC4t4KxRtIcVEvlHjU=";
        private const int numResults = 3;

        public Bing()
        {
            RegisterListener<IRCCommandEvent>(Search);
        }

        private void Search(IRCCommandEvent ev)
        {
            if (ev.Command != bingCommand) return;

            string terms = String.Join(" ", ev.Parameters);

            var bingContext = new BingSearchContainer(new Uri(bingUri));
            bingContext.Credentials = new NetworkCredential(accountKey, accountKey);
            DataServiceQuery<WebResult> query = bingContext.Web(terms);
            

            try
            {

                var webResults = query.Execute();
                if (webResults == null)
                {
                    const string message = "No results.";
                    IRCReply(ev, message);
                    return;
                }
                foreach (var result in webResults.Take(numResults))
                {
                    string message = String.Format("{0} : {1}", result.Description, result.Url);
                    IRCReply(ev, message);
                }
            }
            catch(Exception ex)
            {
                const string message = "Error occurred when trying to get/send search results:";
                IRCReply(ev, message);
                IRCReply(ev, ex.Message);
                if (ex.InnerException != null)
                    IRCReply(ev, ex.InnerException.Message);
            }
        }

        private void IRCSay( int connectionId, string target, string message)
        {
            CallMethod("IRC.Say", connectionId, target, message);
        }

        private void IRCReply(IRCMessageEvent ev, string message)
        {
            CallMethod("IRC.Reply", ev, message);
        }
    }
}
