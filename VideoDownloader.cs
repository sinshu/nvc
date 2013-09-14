using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace NicoVideoCrawler
{
    public class VideoDownloader
    {
        private const string loginUri = "https://secure.nicovideo.jp/secure/login?site=niconico";
        private const string apiUri = "http://www.nicovideo.jp/api/getflv/";

        private string loginName;
        private string password;

        private CookieContainer cookieContainer;

        public VideoDownloader(string loginName, string password)
        {
            this.loginName = loginName;
            this.password = password;
            cookieContainer = new CookieContainer();
        }

        public void Login()
        {
            var postText = "mail=" + loginName + "&password=" + password;
            var postData = Encoding.UTF8.GetBytes(postText);

            string result;
            try
            {
                var request = (HttpWebRequest)HttpWebRequest.Create(loginUri);
                request.Method = "POST";
                request.ContentLength = postData.Length;
                request.ContentType = "application/x-www-form-urlencoded";
                request.CookieContainer = cookieContainer;
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(postData, 0, postData.Length);
                }
                result = Utility.GetWebPageData((HttpWebResponse)request.GetResponse());
            }
            catch (Exception e)
            {
                throw new Exception("ログインに失敗", e);
            }

            if (result.Contains("wrongPass"))
            {
                throw new Exception("ログインに失敗 (不正なログイン情報)");
            }
        }

        public Stream Download(string uri, out string title, out int fileSize)
        {
            title = GetWatchPageCookie(uri);

            var videoUri = GetVideoApiInfo(uri)["url"];

            try
            {
                var request = (HttpWebRequest)HttpWebRequest.Create(videoUri);
                request.CookieContainer = cookieContainer;
                var response = (HttpWebResponse)request.GetResponse();
                fileSize = (int)response.ContentLength;
                return response.GetResponseStream();
            }
            catch (Exception e)
            {
                throw new Exception("動画データの取得に失敗 (" + uri + ")", e);
            }
        }

        private string GetWatchPageCookie(string uri)
        {
            string result;
            try
            {
                result = Utility.GetWebPageData(uri, cookieContainer);
            }
            catch (Exception e)
            {
                throw new Exception("動画視聴ページのクッキー取得に失敗 (" + uri + ")", e);
            }

            try
            {
                var match = Regex.Match(result, @"<title>(.*?)</title>");
                return match.Groups[1].Value;
            }
            catch (Exception e)
            {
                throw new Exception("動画タイトルの取得に失敗 (" + uri + ")", e);
            }
        }

        private IDictionary<string, string> GetVideoApiInfo(string uri)
        {
            string result;
            try
            {
                result = Utility.GetWebPageData(apiUri + GetVideoId(uri), cookieContainer);
            }
            catch (Exception e)
            {
                throw new Exception("動画URLの取得に失敗 (" + uri + ")", e);
            }
            try
            {
                return GetDictionaryFromUrlEncodedData(result);
            }
            catch (Exception e)
            {
                throw new Exception("動画URLの解析に失敗 (" + uri + ")", e);
            }
        }

        private static string GetVideoId(string uri)
        {
            var s = uri.Split('/');
            return s[s.Length - 1];
        }

        private static IDictionary<string, string> GetDictionaryFromUrlEncodedData(string data)
        {
            var dst = new Dictionary<string, string>();
            var s = data.Split('&');
            foreach (var t in s)
            {
                var u = t.Split('=');
                dst.Add(u[0], HttpUtility.UrlDecode(u[1]));
            }
            return dst;
        }
    }
}
