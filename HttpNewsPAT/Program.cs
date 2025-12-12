using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace HttpClientNa5
{
    internal class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static async Task Main(string[] args)
        {
            Console.WriteLine("салам алейкум,че хочешь?");
            Console.ReadLine();

            while (true)
            {
                Console.WriteLine("\nВыбери:");
                Console.WriteLine("1. че написал андрюха и артем(чек)");
                Console.WriteLine("2. добавить dog");
                Console.Write("Твой выбор: ");

                string choice = Console.ReadLine();

                if (choice == "1")
                {
                    Console.WriteLine("\nче написал андрюха и артем");
                    Cookie token = await SingInAsync("user", "user");

                    if (token != null)
                    {
                        string content = await GetContentAsync(token);
                        var newsList = ParsingHtml(content);

                        if (newsList.Count == 0)
                        {
                            Console.WriteLine("Пока ничего не написали...");
                        }
                        else
                        {
                            foreach (var news in newsList)
                            {
                                Console.WriteLine($"\n{news.Name}");
                                if (!string.IsNullOrEmpty(news.Description))
                                {
                                    Console.WriteLine($"{news.Description}");
                                }
                            }
                        }
                    }
                }
                else if (choice == "2")
                {
                    Console.WriteLine("\nдобавляем dog");
                    Cookie token = await SingInAsync("user", "user");

                    if (token != null)
                    {
                        Console.WriteLine("\nдай ссылку на картинку (яндекс картинки (копировать ссылку на изображение))");
                        string src = Console.ReadLine();

                        Console.Write("как назовем новость: ");
                        string name = Console.ReadLine();

                        Console.Write("описание: ");
                        string description = Console.ReadLine();

                        bool result = await AddNewsToDatabaseAsync(new NewsItem
                        {
                            Src = src,
                            Name = name,
                            Description = description
                        }, token);

                        if (result)
                        {
                            Console.WriteLine("\ndog добавлен!");
                        }
                        else
                        {
                            Console.WriteLine("\nчто-то пошло не так...");
                        }
                    }
                }
            }
        }

        public class NewsItem
        {
            public string Src { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
        }

        public static async Task<Cookie> SingInAsync(string login, string password)
        {
            string url = "http://10.111.20.114/ajax/login.php";

            try
            {
                var cookieContainer = new CookieContainer();
                var handler = new HttpClientHandler
                {
                    CookieContainer = cookieContainer,
                    UseCookies = true
                };

                using (var client = new HttpClient(handler))
                {
                    var postData = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("login", login),
                        new KeyValuePair<string, string>("password", password)
                    });

                    var response = await client.PostAsync(url, postData);

                    if (response.IsSuccessStatusCode)
                    {
                        var cookies = cookieContainer.GetCookies(new Uri(url));
                        var token = cookies["token"];

                        if (token != null)
                        {
                            return new Cookie(token.Name, token.Value, token.Path, token.Domain);
                        }
                    }

                    Console.WriteLine("не зашел...");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ошибка: {ex.Message}");
                return null;
            }
        }

        public static async Task<string> GetContentAsync(Cookie token)
        {
            string url = "http://10.111.20.114/main.php";

            try
            {
                var cookieContainer = new CookieContainer();
                var handler = new HttpClientHandler
                {
                    CookieContainer = cookieContainer,
                    UseCookies = true
                };
                cookieContainer.Add(new Uri(url), new System.Net.Cookie(token.Name, token.Value, token.Path, token.Domain));

                using (var client = new HttpClient(handler))
                {
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ошибка: {ex.Message}");
                return null;
            }
        }

        public static List<NewsItem> ParsingHtml(string htmlCode)
        {
            var newsList = new List<NewsItem>();

            if (string.IsNullOrEmpty(htmlCode))
            {
                return newsList;
            }

            try
            {
                var html = new HtmlDocument();
                html.LoadHtml(htmlCode);

                var Document = html.DocumentNode;
                IEnumerable<HtmlNode> DivsNews = Document.Descendants(0).Where(n => n.HasClass("news"));

                foreach (HtmlNode DivNews in DivsNews)
                {
                    var src = DivNews.ChildNodes[1].GetAttributeValue("src", "none");
                    var name = DivNews.ChildNodes[3].InnerText.Trim();
                    var description = DivNews.ChildNodes[5].InnerText.Trim();

                    newsList.Add(new NewsItem
                    {
                        Src = src,
                        Name = name,
                        Description = description
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ошибка парсинга: {ex.Message}");
            }

            return newsList;
        }

        public static async Task<bool> AddNewsToDatabaseAsync(NewsItem news, Cookie token)
        {
            try
            {
                string url = "http://10.111.20.114/ajax/add.php";

                var cookieContainer = new CookieContainer();
                var handler = new HttpClientHandler
                {
                    CookieContainer = cookieContainer,
                    UseCookies = true
                };
                cookieContainer.Add(new Uri(url), new System.Net.Cookie(token.Name, token.Value, token.Path, token.Domain));

                using (var client = new HttpClient(handler))
                {
                    var postData = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("src", news.Src),
                        new KeyValuePair<string, string>("name", news.Name),
                        new KeyValuePair<string, string>("description", news.Description)
                    });

                    var response = await client.PostAsync(url, postData);
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ошибка: {ex.Message}");
                return false;
            }
        }
    }
}