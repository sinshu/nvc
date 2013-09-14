using System;

namespace NicoVideoCrawler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var app = new Application(args[0], args[1]);
            app.Run();
        }
    }
}
