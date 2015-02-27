using System;
using Nexus;
using Nexus.Messages;

namespace NexusTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var core = new NexusCore();
            while (!core.ShuttingDown)
            {
                string line = (Console.ReadLine() ?? String.Empty).Trim(); // Ctrl+Z does nothing
                bool isCmd = line.StartsWith("/");
                if (isCmd)
                    line = line.Substring(1).Trim();
                if (line.Length == 0) continue;

                if (isCmd)
                {
                    string[] lineSplit = line.Split(new[] {' '}, 2);
                    string paramString = lineSplit.Length > 1 ? lineSplit[1] : null;
                    core.SendMessage(new UserCommandEvent(lineSplit[0], paramString));
                    
                }
                else
                {
                    core.SendMessage(new UserMessageEvent(line));
                }
            }
        }
    }
}
