using System;

namespace NicoVideoCrawler
{
    public class VideoInfo
    {
        private string title;
        private string uri;

        internal VideoInfo(string title, string uri)
        {
            this.title = title.Replace('\\', '＼').Replace('/', '／').Replace(':', '：').Replace('*', '＊')
                .Replace('?', '？').Replace('\"', '”').Replace('<', '＜').Replace('>', '＞').Replace('|', '｜')
                .Replace('&', '＆');
            this.uri = uri;
        }

        public string Title
        {
            get
            {
                return title;
            }
        }

        public string Uri
        {
            get
            {
                return uri;
            }
        }
    }
}
