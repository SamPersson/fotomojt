using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Mono.Options;

namespace Fotomojt
{
    public static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var timeBetweenImages = new TimeSpan(0, 0, 5);
            var dir = Directory.GetCurrentDirectory();
            var port = 80;

            var showHelp = false;

            var p = new OptionSet
            {
                { "t|timeout=", "{seconds} between images", (int v) => timeBetweenImages = new TimeSpan(0, 0, v)},
                { "p|port=", "{port} between images", (int v) => port = v},
                { "h|help",  "show this message and exit", v => { showHelp = v != null; } },
            };

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            if (showHelp)
            {
                p.WriteOptionDescriptions(Console.Out);
                return;
            }

            if (extra.Count > 0)
            {
                dir = extra[0];
            }

            using (var game = new Fotomojt(dir, timeBetweenImages, port))
            {
                game.Run();
            }
        }
    }
}
