using System;

namespace NicoVideoCrawler
{
    public static class Settings
    {
        public const string TextEncoding = "UTF-8";
        public const string ChannelListFileName = "chlist.txt";
        public const string DownloadDirectory = "download";
        public const string IndexHtmlFileName = "index.html";
        public const string MessageTextFileName = "msg.txt";
        public const int ChannelPageAccessRetry = 3;
        public const int ChannelPageAccessWaitSeconds = 3;
        public const int VideoDownloadRetry = 3;
        public const int VideoDownloadWaitSeconds = 60;
        public const int CrawlHour = 4;

        public static readonly bool NoDownload = false;
    }
}
