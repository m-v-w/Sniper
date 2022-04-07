using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.ClearScript.V8;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telegram.Bot;

namespace Sniper
{
    class Program
    {
        private const string TELEGRAM_TOKEN = null; // TODO
        private static readonly long[] CHAT_IDS = null;
        static void Main(string[] args)
        {
            CheckUi();
            return;
            //SendMsg("start test");
            while (true)
            {
                CheckAmdOverview();
                //CheckAmd("5496921400","6700XT");
                //CheckAmd("5458374000","6800");
                //CheckAmd("5458374000","6800");
                Thread.Sleep(TimeSpan.FromSeconds(20));
            }
        }

        static void CheckUi()
        {
            //var pageContent = LoadString("https://eu.store.ui.com/collections/unifi-protect/products/unifi-protect-g4-ptz", null, false);
            var pageContent = LoadString("https://eu.store.ui.com/collections/unifi-protect/products/unifi-video-g3-flex-camera", null, false);
            var match = Regex.Match(pageContent, "window\\.APP_DATA \\= \\{(.+?)\\}\\;", RegexOptions.Singleline);
            if (!match.Success)
                return;
            var jsonString = $"JSON.stringify({{{match.Groups[1].Value}}})";
            jsonString = Regex.Replace(jsonString, "localStorage.getItem\\(.+?\\)", "null");
            using var v8 = new V8ScriptEngine();
            var result = v8.Evaluate(jsonString) as string;
            var obj = JObject.Parse(result);
            var variants = (JArray) ((JObject) obj["product"])["variants"];
            var available = variants.Any(x => x.Value<bool>("available"));
            if (available)
            {
                SendMsg("Gogogo");
            }
        }

        static void CheckAmdOverview()
        {
            try
            {
                var pageContent = LoadString("https://www.amd.com/en/direct-buy/de",null, false);
                var url = $"https://www.amd.com/en/direct-buy/products/de";//?rand={DateTime.Now.Ticks}";
                var referer = $"https://www.amd.com/en/direct-buy/de";
                var content = LoadString(url, referer);
                foreach (var part in content.Split("<div class=\"direct-buy\">"))
                {
                    var mTitle = Regex.Match(part, "<div class=\"shop-title\">(.+?)</div>", RegexOptions.Singleline);
                    if(!mTitle.Success) continue;
                    var title = mTitle.Groups[1].Value.Trim();
                    string name;
                    if (title.Contains("RX 6700 XT Graphics", StringComparison.CurrentCultureIgnoreCase))
                        name = "O_6700XT";
                    else if (title.Contains("RX 6800 Graphics", StringComparison.CurrentCultureIgnoreCase))
                        name = "O_6800";
                    else
                    {
                        continue;
                    }
                    HandleContent(part, name, url);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                SendMsg($"Overview error {ex.Message}");
            }
        }

        static void HandleContent(string content, string name, string url)
        {
            if (content.Contains("Out of stock", StringComparison.CurrentCultureIgnoreCase))
            {
                Log($"{name} out of stock");
            }
            else if (content.Contains("Add to cart", StringComparison.CurrentCultureIgnoreCase))
            {
                SendMsg($"{name} AVAILABLE {url}");
            }
            else
            {
                SendMsg($"{name} known reply");
            }
        }
        static void CheckAmd(string productId, string name)
        {
            try
            {
                var url = $"https://www.amd.com/en/direct-buy/products/{productId}/de";//?rand={DateTime.Now.Ticks}";
                var referer = $"https://www.amd.com/en/direct-buy/{productId}/de";
                var content = LoadString(url, referer);
                HandleContent(content, name, url);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                SendMsg($"{name} error {ex.Message}");
            }
            //                  

        }

        static string LoadString(string url, string referer, bool jsReq=true)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Headers.Add(HttpRequestHeader.Accept, "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            request.Headers.Add(HttpRequestHeader.UserAgent,
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.164 Safari/537.36");
            if(referer != null)
                request.Headers.Add(HttpRequestHeader.Referer, referer);
            //sec-ch-ua: "Chromium";v="92", " Not A;Brand";v="99", "Google Chrome";v="92"
            request.Headers.Add(
                "sec-ch-ua", "\" Not;A Brand\";v=\"99\", \"Google Chrome\";v=\"91\", \"Chromium\";v=\"91\"");
            request.Headers.Add("sec-ch-ua-mobile","?0");
            request.Headers.Add("sec-fetch-dest",jsReq ? "empty" : "document");
            request.Headers.Add("sec-fetch-mode", jsReq?"cors":"navigate");
            request.Headers.Add("sec-fetch-site", jsReq?"same-origin":"none");
            request.Headers.Add("sec-fetch-user", "?1");
            request.Headers.Add("upgrade-insecure-requests", "1");
            //request.Headers.Add("x-requested-with","XMLHttpRequest");  
            request.Headers.Add(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9");
            //request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
            request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore); 
            request.Timeout = 20000;
            request.ReadWriteTimeout = 20000;
            //request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using var wresp = (HttpWebResponse)request.GetResponse();
            using var reader = new StreamReader(wresp.GetResponseStream());
            return reader.ReadToEnd();
        }
        static void SendMsg(string msg)
        {
            Log(msg);
            var text = $"##SNIPER {DateTime.Now} {msg}";
            var bot = new TelegramBotClient(TELEGRAM_TOKEN);
            foreach (var chatId in CHAT_IDS)
            {
                bot.SendTextMessageAsync(chatId, text).GetAwaiter().GetResult();
            }
        }
        static void Log(string msg)
        {
            Console.WriteLine($"{DateTime.Now} {msg}");
        }
    }
}