using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace NicoVideoCrawler
{
    public static class VideoPicker
    {
        public static ICollection<VideoInfo> PickVideosFromChannelPage(string uri)
        {
            string result;
            try
            {
                result = Utility.GetWebPageData(uri + "/video");
            }
            catch (Exception e)
            {
                throw new Exception("DL可能動画の取得に失敗 (" + uri + ")", e);
            }

            ICollection<VideoInfo> videoInfo;
            try
            {
                videoInfo = PickVideosFromChannelPageSub(result);
            }
            catch (Exception e)
            {
                throw new Exception("DL可能動画の解析に失敗 (" + uri + ")", e);
            }

            if (videoInfo.Count == 0)
            {
                throw new Exception("DL可能動画が存在しない (" + uri + ")");
            }

            return videoInfo;
        }

        private static ICollection<VideoInfo> PickVideosFromChannelPageSub(string requestResult)
        {
            var matches = Regex.Matches(requestResult,
                @"<li class=""item"">.*?<a.*?(http://www\.nicovideo\.jp/watch/\d+).*?></a>.*?<a.*?>(.*?)</a>", RegexOptions.Singleline);
            var videoInfoList = new List<VideoInfo>();
            foreach (Match match in matches)
            {
                if (!match.Value.Contains("purchase_type"))
                {
                    var uri = match.Groups[1].Value;
                    var title = match.Groups[2].Value;
                    var videoInfo = new VideoInfo(title, uri);
                    videoInfoList.Add(videoInfo);
                }
            }
            return videoInfoList;
        }
    }
}
