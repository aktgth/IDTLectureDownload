using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace IDTLectureDownload
{
    public class FileInfo
    {
        public string urlPath { get; set; }
        public string filePath { get; set; }

        public FileInfo(string urlPath, string filePath)
        {
            this.urlPath = urlPath;
            this.filePath = filePath;
        }
    }


    class Program
    {
        private static readonly string BASE_URL = @"https://audio.iskcondesiretree.com/02_-_ISKCON_Swamis/ISKCON_Swamis_-_D_to_P/His_Holiness_Jayapataka_Swami/English_Lectures/Various/";
        private static readonly string BASE_FOLDER_PATH = @"D:\AdMadProjs\TestProjs\HH Jayapataka Swami Maharaj\Srimad_Bhagavatam\Various\";
        
        static void Main(string[] args)
        {
            StartCrawlerAsync().GetAwaiter().GetResult();
        }
        
        private static async Task StartCrawlerAsync()
        {
            var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(BASE_URL);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            await CrawlCurrentPage(BASE_URL, BASE_FOLDER_PATH);
        }

        private static async Task CrawlCurrentPage(string urlPath, string folderPath)
        {
            var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(urlPath);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            var rowNodes = htmlDocument.DocumentNode.SelectNodes("/html/body/table")[0].ChildNodes.Where(a => a.Name == "tr");
            int rowIter = -1;
            foreach (var rowNode in rowNodes)
            {
                rowIter++;
                //if (urlPath.Equals("") && rowIter <= 12) continue;
                if (rowIter < 3) continue;
                foreach (var columNode in rowNode.ChildNodes)
                {
                    if (columNode.InnerHtml.Contains("href"))
                    {
                        if (columNode.InnerHtml.ToLower().Contains("mp3") || columNode.InnerHtml.ToLower().Contains(".pdf"))
                        {
                            string fileName = columNode.ChildNodes[0].Attributes["href"].Value;
                            await DownloadFile(Path.Combine(urlPath, fileName), Path.Combine(folderPath, fileName));
                        }
                        else if (columNode.InnerHtml.Contains(".db"))
                        {
                            continue;
                        }
                        else
                        {
                            string folderName = columNode.ChildNodes[0].Attributes["href"].Value;
                            await CrawlCurrentPage(Path.Combine(urlPath, folderName), Path.Combine(folderPath, folderName));
                        }
                    }
                }
            }
        }

        private static async Task DownloadFile(string urlPath, string localFilePath)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("audio/mpeg"));
                using (HttpRequestMessage reqMsg = new HttpRequestMessage())
                {
                    reqMsg.RequestUri = new Uri(urlPath);
                    reqMsg.Method = HttpMethod.Get;
                    var response = httpClient.SendAsync(reqMsg).GetAwaiter().GetResult();
                    if (response.IsSuccessStatusCode)
                    {
                        HttpContent content = response.Content;
                        var contentStream = content.ReadAsStreamAsync().GetAwaiter().GetResult();
                        var fs = File.Create(localFilePath);
                        contentStream.Seek(0, SeekOrigin.Begin);
                        contentStream.CopyTo(fs);
                    }
                }
            }
        }

    }
}
