using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace APRipper
{
    public static class Program
    {
        private static readonly HttpClient HttpClient;

        static Program()
        {
            var clientHandler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                SslProtocols = SslProtocols.Tls12,
            };

            HttpClient = new HttpClient(clientHandler, true);
        }
        
        public static async Task Main()
        {
            Console.WriteLine("Please paste the address to the chapter that you want to download from alphapolis");
            var uri = new Uri(Console.ReadLine());

            var uriString = uri.AbsoluteUri;

            var seriesString = Regex.Replace(uriString, "https:\\/\\/www[.]alphapolis[.]co[.]jp\\/manga\\/official\\/", string.Empty);
            seriesString = seriesString.Remove(seriesString.IndexOf("/"));   
            var chapterString = Regex.Replace(uriString, "https:\\/\\/www[.]alphapolis[.]co[.]jp\\/manga\\/official/[0-9]+\\/", string.Empty);
            
            var page = await HttpClient.GetStringAsync(uri);
            var pages = Regex.Matches(page, $"https:\\/\\/cdn-image[.]alphapolis[.]co[.]jp\\/official_manga\\/page\\/{seriesString}\\/{chapterString}\\/.*\\/.*[.]jpg").
                GroupBy(match => match.Value).Select(match => match.First()).ToImmutableList();

            var location = new FileInfo(new Uri(Assembly.GetEntryAssembly().GetName().CodeBase).AbsolutePath).Directory;
            var downloadLocation = location?.CreateSubdirectory($"Downloads/{seriesString}/{chapterString}");
            var tasks = new List<Task>();
            
            Console.WriteLine("Downloading pages...");
            
            for (var i = 0; i < pages.Count; i++)
            {
                string fileName;

                if (pages.Count < 9)
                {
                    fileName = $"0{i + 1}.jpg";
                }
                else if (pages.Count < 99)
                {
                    fileName = i < 9 ? $"0{i + 1}.jpg" : $"{i + 1}.jpg";
                }
                else
                {
                    if (i < 9)
                    {
                        fileName = $"00{i + 1}.jpg";
                    }
                    else if (i < 99)
                    {
                        fileName = $"0{i + 1}.jpg";
                    }
                    else
                    {
                        fileName = $"{i + 1}.jpg";
                    }
                }
                
                var pageAddress = $"{pages[i].Value.Remove(pages[i].Value.LastIndexOf("/") + 1)}1080x1536.jpg";
                tasks.Add(Download(pageAddress, downloadLocation?.FullName, fileName));
            }

            Task.WaitAll(tasks.ToArray());
            
            Console.WriteLine("Download complete!!");
            Console.WriteLine("Please run the program again if you wish to download another chapter.");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static async Task Download(string pageAddress, string downloadLocation, string fileName)
        {
            await File.WriteAllBytesAsync($"{downloadLocation}/{fileName}", await HttpClient.GetByteArrayAsync(pageAddress));
        }
    }
}