using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace NicoVideoCrawler
{
    public static class Utility
    {
        public static string GetWebPageData(string uri)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(uri);
            var response = (HttpWebResponse)request.GetResponse();
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }

        public static string GetWebPageData(string uri, CookieContainer cookieContainer)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(uri);
            request.CookieContainer = cookieContainer;
            var response = (HttpWebResponse)request.GetResponse();
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }

        public static string GetWebPageData(HttpWebResponse response)
        {
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }

        public static void StringToTextFile(string s, string path)
        {
            using (var writer = new StreamWriter(path, false, Encoding.GetEncoding(Settings.TextEncoding)))
            {
                writer.Write(s);
            }
        }

        public static string TextFileToString(string path)
        {
            using (var reader = new StreamReader(path, Encoding.GetEncoding(Settings.TextEncoding)))
            {
                return reader.ReadToEnd();
            }
        }

        public static string GetWebPageTitle(string uri)
        {
            var result = GetWebPageData(uri);
            var match = Regex.Match(result, @"<title>(.*?)</title>");
            return match.Groups[1].Value;
        }
    }
}
