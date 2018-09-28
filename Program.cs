using System;
using System.IO;
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
                SslProtocols = SslProtocols.Tls12
            };

            HttpClient = new HttpClient(clientHandler, true);
        }

        public static async Task Main(string[] args)
        {
            Uri uri;

            if (args.Length > 0)
            {
                uri = new Uri(args[0]);
            }
            else
            {
                Console.WriteLine("Please paste the address to the chapter that you want to download from alphapolis");
                uri = new Uri(Console.ReadLine());
            }

            var seriesString = Regex.Replace(uri.AbsoluteUri, "https:\\/\\/www[.]alphapolis[.]co[.]jp\\/manga\\/official\\/", string.Empty);
            seriesString = seriesString.Remove(seriesString.IndexOf("/", StringComparison.OrdinalIgnoreCase));
            var chapterString = Regex.Replace(uri.AbsoluteUri, "https:\\/\\/www[.]alphapolis[.]co[.]jp\\/manga\\/official/[0-9]+\\/", string.Empty);

            var page = await HttpClient.GetStringAsync(uri);
            var pages = Regex.Matches(page, "_pages[.]push.*[.]jpg");

            var location = new FileInfo(new Uri(Assembly.GetEntryAssembly().GetName().CodeBase).AbsolutePath).Directory;
            var downloadLocation = location?.CreateSubdirectory($"Downloads/{seriesString}/{chapterString}");
            var tasks = new Task[pages.Count];

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
                        fileName = $"00{i + 1}.jpg";
                    else if (i < 99)
                        fileName = $"0{i + 1}.jpg";
                    else
                        fileName = $"{i + 1}.jpg";
                }

                var pageAddress = pages[i].Value.Substring(pages[i].Value.IndexOf("\"", StringComparison.OrdinalIgnoreCase) + 1);
                pageAddress = $"{pageAddress.Remove(pageAddress.LastIndexOf("/", StringComparison.OrdinalIgnoreCase) + 1)}1080x1536.jpg";

                tasks[i] = Task.Run(async () =>
                {
                    await File.WriteAllBytesAsync($"{downloadLocation}/{fileName}", await HttpClient.GetByteArrayAsync(pageAddress));
                });
            }

            Task.WaitAll(tasks);

            Console.WriteLine("Download complete!!");
            Console.WriteLine("Please run the program again if you wish to download another chapter.");

            if (args.Length == 0)
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }
    }
}