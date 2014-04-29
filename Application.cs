using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Web;

namespace NicoVideoCrawler
{
    public class Application
    {
        private delegate void RetryAction();

        private const string downloadingPrefix = "!temp_";
        private const string videoExtension = ".mp4";
        private const int maxFileNameLength = 50;

        private string loginName;
        private string password;

        private VideoDownloader downloader;

        public Application(string loginName, string password)
        {
            Log.WriteLine("NVC - ニコニコ動画自動保存システム");
            this.loginName = loginName;
            this.password = password;
        }

        public void Run()
        {
            if (!Directory.Exists(Settings.DownloadDirectory))
            {
                Directory.CreateDirectory(Settings.DownloadDirectory);
            }

            downloader = new VideoDownloader(loginName, password);

            //Crawl();

            CreateIndexHtmlFile(GetChannelUris());

            var prevHour = DateTime.Now.Hour;
            while (true)
            {
                var hour = DateTime.Now.Hour;
                if (hour != prevHour && hour == Settings.CrawlHour)
                {
                    try
                    {
                        Crawl();
                    }
                    catch (Exception e)
                    {
                        Log.WriteException(e);
                    }
                }
                prevHour = hour;
                Thread.Sleep(1000 * 60);
            }
        }

        private void Crawl()
        {
            Log.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss 巡回開始"));

            var channelUris = GetChannelUris();

            var videoInfoCollection = GetVideoInfoCollection(channelUris);

            if (!Settings.NoDownload && videoInfoCollection.Count > 0)
            {
                Log.WriteLine(videoInfoCollection.Count + " 件の新着動画");

                Log.WriteLine("ログイン開始");
                DoRetryAction(downloader.Login, Settings.ChannelPageAccessRetry, Settings.ChannelPageAccessWaitSeconds);
                Log.WriteLine("ログイン成功");

                Thread.Sleep(3000);

                DownloadVideo(videoInfoCollection);
            }

            Log.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss 巡回終了"));

            Log.WriteLine("インデックスHTMLファイル生成");
            DoRetryAction(() => CreateIndexHtmlFile(channelUris), 3, 1);
        }

        private static void DoRetryAction(RetryAction action, int numRetry, int waitSec)
        {
            for (int i = 0; i < numRetry; i++)
            {
                try
                {
                    action();
                    return;
                }
                catch
                {
                    if (i == numRetry - 1)
                    {
                        throw;
                    }
                }
                if (i < numRetry - 1)
                {
                    Thread.Sleep(1000 * waitSec);
                }
            }
        }

        private static ICollection<string> GetChannelUris()
        {
            var uriList = new List<string>();
            using (var reader = new StreamReader(Settings.ChannelListFileName))
            {
                for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    uriList.Add(line);
                }
            }
            return uriList;
        }

        private static ICollection<VideoInfo> GetVideoInfoCollection(ICollection<string> channelUris)
        {
            var dst = new List<VideoInfo>();
            foreach (var channelUri in channelUris)
            {
                try
                {
                    DoRetryAction(() => GetVideoInfoCollectionSub(channelUri, dst),
                        Settings.ChannelPageAccessRetry, Settings.ChannelPageAccessWaitSeconds);
                }
                catch (Exception e)
                {
                    Log.WriteException(e);
                }
                Thread.Sleep(1000 * Settings.ChannelPageAccessWaitSeconds);
            }
            return dst;
        }

        private static void GetVideoInfoCollectionSub(string channelUri, ICollection<VideoInfo> dst)
        {
            var vic = VideoPicker.PickVideosFromChannelPage(channelUri);
            foreach (var vi in vic)
            {
                if (!File.Exists(GetVideoPath(vi)))
                {
                    dst.Add(vi);
                }
            }
        }

        private void DownloadVideo(ICollection<VideoInfo> videoInfoCollection)
        {
            foreach (var vi in videoInfoCollection)
            {
                try
                {
                    DoRetryAction(() => DownloadVideoSub(vi),
                        Settings.VideoDownloadRetry, Settings.VideoDownloadWaitSeconds);
                }
                catch (Exception e)
                {
                    Log.WriteException(e);
                }
                Thread.Sleep(1000 * Settings.VideoDownloadWaitSeconds);
            }
        }

        private bool DownloadVideoSub(VideoInfo videoInfo)
        {
            var videoPath = GetVideoPath(videoInfo);
            if (File.Exists(videoPath))
            {
                Log.WriteLine("動画 [" + videoInfo.Title + "] は既に存在している");
                return false;
            }

            string title;
            int fileSize;
            var buffer = new byte[32 * 1024];
            using (var input = downloader.Download(videoInfo.Uri, out title, out fileSize))
            {
                Log.WriteLine("ダウンロード: " + videoInfo.Title + " (" + fileSize.ToString("#,0") + " バイト)");
                var tempPath = downloadingPrefix + videoInfo.Title + videoExtension;
                var count = 0;
                using (var output = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                {
                    int read;
                    do
                    {
                        read = input.Read(buffer, 0, buffer.Length);
                        output.Write(buffer, 0, read);
                        count += read;
                        var percentage = (int)(100.0 * count / fileSize);
                        Console.Write(count.ToString("#,0") + " バイト (" + percentage + "%) 完了\r");
                    }
                    while (read > 0);
                }
                Console.WriteLine();
                if (count != fileSize)
                {
                    throw new Exception("動画ファイルが破損している");
                }
                File.Move(tempPath, videoPath);
                Log.WriteLine("ダウンロード完了");
                return true;
            }
        }

        private static string GetVideoPath(VideoInfo videoInfo)
        {
            return Settings.DownloadDirectory + "\\" + videoInfo.Title + videoExtension;
        }

        private static void CreateIndexHtmlFile(ICollection<string> channelUris)
        {
            using (var writer = new StreamWriter(Settings.IndexHtmlFileName, false, Encoding.GetEncoding(Settings.TextEncoding)))
            {
                writer.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\">");
                writer.WriteLine("<html lang=\"ja\">");
                writer.WriteLine("<head>");
                writer.WriteLine("<meta name=\"robots\" content=\"noindex, nofollow, noarchive\">");
                writer.WriteLine("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=" + Settings.TextEncoding + "\">");
                writer.WriteLine("<meta http-equiv=\"Content-Style-Type\" content=\"text/css\">");
                writer.WriteLine("<title>NVC - ニコニコ動画自動保存システム</title>");
                writer.WriteLine("<link rel=\"stylesheet\" type=\"text/css\" href=\"style.css\">");
                writer.WriteLine("</head>");
                writer.WriteLine("<body>");
                writer.WriteLine("<h3>NVC - ニコニコ動画自動保存システム</h3>");
                if (File.Exists(Settings.MessageTextFileName))
                {
                    writer.WriteLine("<div class=\"message\">" + Utility.TextFileToString(Settings.MessageTextFileName) + "</div>");
                }
                WriteChannelList(writer, channelUris);
                writer.WriteLine("<div class=\"message\">最終巡回日：" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "</div>");
                WriteVideoIndex(writer);
                writer.WriteLine("</body>");
                writer.WriteLine("</html>");
            }
        }

        private static void WriteChannelList(TextWriter writer, ICollection<string> channelUris)
        {
            writer.WriteLine("<div class=\"message\">");
            writer.WriteLine("<dl>");
            writer.WriteLine("<dt>巡回先チャンネル：</dt>");
            foreach (var ch in channelUris)
            {
                var s = ch.Split('/');
                writer.WriteLine("<dd>" + s[s.Length - 1] +"</dd>");
            }
            writer.WriteLine("</dl>");
            writer.WriteLine("</div>");
        }

        private static void WriteVideoIndex(TextWriter writer)
        {
            writer.WriteLine("<table>");
            writer.WriteLine("<tr><th>ファイル名</th><th>サイズ</th><th>DL日時</th></tr>");
            var files = Directory.GetFiles(Settings.DownloadDirectory);
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.Extension != videoExtension)
                {
                    continue;
                }
                writer.Write("<tr>");
                writer.Write("<td class=\"filename\"><a href=\"" +
                    HttpUtility.HtmlEncode(Settings.DownloadDirectory + "/" + fileInfo.Name).Replace("#", "%23").Replace(";", "%3b") + "\">" +
                    HttpUtility.HtmlEncode(AbbrFileName(fileInfo.Name)) + "</a></td>");
                writer.Write("<td class=\"filesize\">" + fileInfo.Length.ToString("#,0") + "</td>");
                writer.Write("<td class=\"datetime\">" + fileInfo.LastWriteTime.ToString("yyyy/MM/dd HH:mm:ss") + "</td>");
                writer.WriteLine("</tr>");
            }
            writer.WriteLine("</table>");
        }

        private static string AbbrFileName(string fileName)
        {
            if (fileName.Length <= maxFileNameLength)
            {
                return fileName;
            }
            else
            {
                return fileName.Substring(0, maxFileNameLength) + "...";
            }
        }
    }
}
