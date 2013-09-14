using System;
using System.IO;
using System.Text;

namespace NicoVideoCrawler
{
    public class Log
    {
        private static StreamWriter writer;

        static Log()
        {
            var dateTime = DateTime.Now.ToString("yyMMddHHmmss");
            writer = new StreamWriter("log" + dateTime + ".txt", false, Encoding.GetEncoding(Settings.TextEncoding));
        }

        public static void WriteLine(string value)
        {
            Console.WriteLine(value);
            writer.WriteLine(value);
            writer.Flush();
        }

        public static void WriteException(Exception e)
        {
            Console.WriteLine(e);
            writer.WriteLine(e);
            writer.Flush();
        }
    }
}
