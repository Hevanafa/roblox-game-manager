using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Fizzler.Systems.HtmlAgilityPack;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using HtmlAgilityPack;
using System.Windows.Media;
using System.Net.Http;

namespace RobloxGameManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void println(object any) {
            txbOutput.Text += $"{any}\n";
            txbOutput.ScrollToEnd();
        }

        async Task<HtmlDocument> fetchHtml(string url) {
            var web = new HtmlWeb();
            HtmlDocument html = null;
            await Task.Run(() => { html = web.Load(url); });
            return html;
        }

        async Task<BitmapSource> fetchImageSource(string imageUrl) {
            BitmapSource bmp = null;
            var client = new HttpClient();

            using (var res = await client.GetAsync(imageUrl))
                if (res.IsSuccessStatusCode)
                    using (var stream = new MemoryStream()) {
                        await res.Content.CopyToAsync(stream);
                        stream.Seek(0, SeekOrigin.Begin);

                        bmp = BitmapFrame.Create(
                            stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    }

            return bmp;
        }

        private async void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            var url = txbURL.Text;

            btnDownload.IsEnabled = false;

            // Example:
            // https://www.roblox.com/games/6167122056/Akademi-Online
            // https://www.roblox.com/games/263761432/BOSS-Horrific-Housing

            if (Regex.IsMatch(url, @"^\d+$") || Regex.IsMatch(url, @"^https://www\.roblox\.com/games/\d+/"))
            {
                var gameID = Regex.Match(url, @"\d+");
                println("game ID: " + gameID);

                println("Fetching page...");

                var html = await fetchHtml(url);

                var document = html.DocumentNode;
                var ele = document.QuerySelector("meta[property~=\"og:title\"]");

                string title = "", imageUrl = "";

                if (ele != null) {
                    var content = ele.Attributes["content"].Value;
                    println("Found game title: " + content);
                    title = content;
                }

                ele = document.QuerySelector("meta[property=\"og:image\"]");

                if (ele != null) {
                    var content = ele.Attributes["content"].Value;
                    println("Found image URL: " + content);
                    imageUrl = content;
                }

                lblGameTitle.Content = title;
                imgGameThumb.Source = await fetchImageSource(imageUrl); // new BitmapImage(new Uri(imageUrl, UriKind.Absolute));

                // Done: save image
                // Todo: save the title & image to database

                // Save image
                println("Saving image...");

                var encoder = new PngBitmapEncoder();
                //var bitmap = new RenderTargetBitmap((int)imgGameThumb.Source.Width, (int)imgGameThumb.Source.Height, 96, 96, PixelFormats.Pbgra32);
                //bitmap.Render(imgGameThumb.Source);

                encoder.Frames.Add(BitmapFrame.Create((BitmapSource) imgGameThumb.Source));

                if (!Directory.Exists("thumbs"))
                    Directory.CreateDirectory("thumbs");

                var filename = $"thumbs\\{gameID}.png";

                using (var stream = File.Create(filename))
                    encoder.Save(stream);

                // 500 x 280
                // https://www.roblox.com/games/990566015/Cursed-Islands

                println("Image has been saved as " + filename + ".");

                btnDownload.IsEnabled = true;
            }
            else {
                println("Input URL isn't a valid URL.");
                btnDownload.IsEnabled = true;
            }
        }
    }
}
