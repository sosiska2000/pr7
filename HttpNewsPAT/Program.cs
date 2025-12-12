using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace HttpNewsPAT
{
    public class NewsItem
    {
        public string Src { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    internal class Program
    {
        private static readonly string LogFilePath = "debug.log";

        static async Task Main(string[] args)
        {
            Console.WriteLine("1. Получить новости с сайта");
            Console.WriteLine("2. Добавить новость на сайт");
            Console.Write("Выберите действие (1 или 2): ");

            string choice = Console.ReadLine();

            if (choice == "1")
            {
                await GetNewsFromSite();
            }
            else if (choice == "2")
            {
                await AddNewsToSite();
            }
            else
            {
                Console.WriteLine("Неверный выбор!");
            }

            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        static async Task GetNewsFromSite()
        {
            Console.WriteLine("\nПолучение новостей");

            try
            {
                // Авторизация
                Cookie token = await SingInAsync("admin", "admin");
                if (token == null)
                {
                    Console.WriteLine("Ошибка авторизации!");
                    return;
                }
                Console.WriteLine("Авторизация успешна!");

                // Получение контента
                string content = await GetContentAsync(token);
                if (string.IsNullOrEmpty(content))
                {
                    Console.WriteLine("Не удалось получить контент!");
                    return;
                }
                Console.WriteLine("Контент успешно получен!");

                // Парсинг HTML
                var newsList = ParsingHtml(content);
                Console.WriteLine($"\nНайдено новостей: {newsList.Count}");

                foreach (var news in newsList)
                {
                    Console.WriteLine($"\nНазвание: {news.Name}");
                    Console.WriteLine($"Изображение: {news.Src}");
                    Console.WriteLine($"Описание: {news.Description}");
                    Console.WriteLine(new string('-', 50));
                }

                // Предложение добавить новость
                Console.Write("\nХотите добавить новость? (y/n): ");
                if (Console.ReadLine().ToLower() == "y")
                {
                    await AddNewsManuallyAsync(token);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                LogToFile($"Ошибка в GetNewsFromSite: {ex.Message}");
            }
        }

        static async Task AddNewsToSite()
        {
            Console.WriteLine("\nДобавление новости");

            try
            {
                // Авторизация
                Cookie token = await SingInAsync("admin", "admin");
                if (token == null)
                {
                    Console.WriteLine("Ошибка авторизации!");
                    return;
                }
                Console.WriteLine("Авторизация успешна!");

                // Добавление новости
                await AddNewsManuallyAsync(token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                LogToFile($"Ошибка в AddNewsToSite: {ex.Message}");
            }
        }

        public static async Task<Cookie> SingInAsync(string login, string password)
        {
            string url = "http://10.111.20.114/ajax/login.php";
            string message = $"Выполняем запрос авторизации: {url}";
            Debug.WriteLine(message);
            LogToFile(message);

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

                    message = $"Статус авторизации: {response.StatusCode}";
                    Debug.WriteLine(message);
                    LogToFile(message);

                    var responseFromServer = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Ответ сервера: {responseFromServer}");
                    LogToFile($"Ответ сервера: {responseFromServer}");

                    var cookies = cookieContainer.GetCookies(new Uri(url));
                    var token = cookies["token"];

                    if (token != null)
                    {
                        return new Cookie(token.Name, token.Value, token.Path, token.Domain);
                    }

                    Console.WriteLine("Токен не найден в ответе!");
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                message = $"Ошибка HTTP запроса при авторизации: {ex.Message}";
                Console.WriteLine(message);
                LogToFile(message);
                return null;
            }
            catch (Exception ex)
            {
                message = $"Ошибка авторизации: {ex.Message}";
                Console.WriteLine(message);
                LogToFile(message);
                return null;
            }
        }

        public static async Task<string> GetContentAsync(Cookie token)
        {
            if (token == null)
            {
                Console.WriteLine("Токен не получен!");
                return null;
            }

            string url = "http://10.111.20.114/main";
            string message = $"Выполняем запрос контента: {url}";
            Debug.WriteLine(message);
            LogToFile(message);

            try
            {
                var cookieContainer = new CookieContainer();
                var handler = new HttpClientHandler
                {
                    CookieContainer = cookieContainer,
                    UseCookies = true
                };

                cookieContainer.Add(new Uri(url),
                    new Cookie(token.Name, token.Value, token.Path, token.Domain));

                using (var client = new HttpClient(handler))
                {
                    var response = await client.GetAsync(url);

                    message = $"Статус получения контента: {response.StatusCode}";
                    Debug.WriteLine(message);
                    LogToFile(message);

                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();

                    Debug.WriteLine($"Получено символов: {content.Length}");
                    LogToFile($"Получено символов: {content.Length}");

                    return content;
                }
            }
            catch (HttpRequestException ex)
            {
                message = $"Ошибка HTTP запроса при получении контента: {ex.Message}";
                Console.WriteLine(message);
                LogToFile(message);
                return null;
            }
            catch (Exception ex)
            {
                message = $"Ошибка получения контента: {ex.Message}";
                Console.WriteLine(message);
                LogToFile(message);
                return null;
            }
        }

        public static List<NewsItem> ParsingHtml(string htmlCode)
        {
            var newsList = new List<NewsItem>();

            if (string.IsNullOrEmpty(htmlCode))
            {
                Console.WriteLine("HTML код пуст!");
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
                    var name = DivNews.ChildNodes[3].InnerHtml;
                    var description = DivNews.ChildNodes[5].InnerHtml;

                    newsList.Add(new NewsItem
                    {
                        Src = src,
                        Name = name,
                        Description = description
                    });
                }

                Debug.WriteLine($"Найдено элементов с классом 'news': {newsList.Count}");
                LogToFile($"Найдено элементов с классом 'news': {newsList.Count}");
            }
            catch (Exception ex)
            {
                string message = $"Ошибка парсинга HTML: {ex.Message}";
                Console.WriteLine(message);
                Debug.WriteLine(message);
                LogToFile(message);
            }

            return newsList;
        }

        public static async Task AddNewsManuallyAsync(Cookie token)
        {
            if (token == null)
            {
                Console.WriteLine("Ошибка: Токен не получен. Авторизуйтесь снова.");
                return;
            }

            Console.WriteLine("\nДобавление новой новости");
            Console.Write("Введите URL изображения: ");
            string src = Console.ReadLine();

            Console.Write("Введите название новости: ");
            string name = Console.ReadLine();

            Console.Write("Введите описание новости: ");
            string description = Console.ReadLine();

            Console.WriteLine("\nПроверьте введенные данные");
            Console.WriteLine($"Изображение: {src}");
            Console.WriteLine($"Название: {name}");
            Console.WriteLine($"Описание: {description}");
            Console.Write("\nДобавить эту новость? (y/n): ");

            if (Console.ReadLine().ToLower() != "y")
            {
                Console.WriteLine("Добавление отменено.");
                return;
            }

            bool result = await AddNewsToDatabaseAsync(new NewsItem
            {
                Src = src,
                Name = name,
                Description = description
            }, token);

            if (result)
            {
                Console.WriteLine($"\nНовость '{name}' успешно добавлена!");
            }
            else
            {
                Console.WriteLine($"\nОшибка при добавлении новости '{name}'");
            }
        }

        public static async Task<bool> AddNewsToDatabaseAsync(NewsItem news, Cookie token)
        {
            try
            {
                string url = "http://10.111.20.114/add";
                string message = $"Добавляем новость: {news.Name}";
                Debug.WriteLine(message);
                LogToFile(message);

                var cookieContainer = new CookieContainer();
                var handler = new HttpClientHandler
                {
                    CookieContainer = cookieContainer,
                    UseCookies = true
                };

                cookieContainer.Add(new Uri(url),
                    new Cookie(token.Name, token.Value, token.Path, token.Domain));

                using (var client = new HttpClient(handler))
                {
                    var postData = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("src", news.Src),
                        new KeyValuePair<string, string>("name", news.Name),
                        new KeyValuePair<string, string>("description", news.Description)
                    });

                    var response = await client.PostAsync(url, postData);

                    message = $"Статус добавления: {response.StatusCode}";
                    Debug.WriteLine(message);
                    LogToFile(message);

                    string responseFromServer = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Ответ сервера: {responseFromServer}");
                    LogToFile($"Ответ сервера: {responseFromServer}");

                    Console.WriteLine($"Сервер ответил: {responseFromServer}");

                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (HttpRequestException ex)
            {
                string message = $"Ошибка HTTP запроса при добавлении новости: {ex.Message}";
                Console.WriteLine(message);
                Debug.WriteLine(message);
                LogToFile(message);

                if (ex.InnerException != null)
                {
                    message = $"Внутренняя ошибка: {ex.InnerException.Message}";
                    Console.WriteLine(message);
                    LogToFile(message);
                }
                return false;
            }
            catch (Exception ex)
            {
                string message = $"Ошибка при добавлении новости: {ex.Message}";
                Console.WriteLine(message);
                Debug.WriteLine(message);
                LogToFile(message);
                return false;
            }
        }

        private static void LogToFile(string message)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(LogFilePath, true, Encoding.UTF8))
                {
                    sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка записи в лог-файл: {ex.Message}");
            }
        }
    }
}