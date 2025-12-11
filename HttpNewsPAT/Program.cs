using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace HttpNewsPAT
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Cookie token = SingIn("user", "user");
            string Content = GetContent(token);
            ParsingHtml(Content);
            
            Console.Read();
        }

        public static void ParsingHtml(string htmlCode)
        {
           var html = new HtmlDocument();
            html.LoadHtml(htmlCode);

            var Document = html.DocumentNode;
            IEnumerable<HtmlNode> DivsNews = Document.Descendants(0).Where(x => x.HasClass("news"));

            foreach(var DivNew in DivsNews)
            {
                var src = DivNew.ChildNodes[1].GetAttributeValue("src", "node");
                var name = DivNew.ChildNodes[3].InnerHtml;
                var description = DivNew.ChildNodes[5].InnerHtml;

                Console.WriteLine($"{name} \nИзображение: {src} \nОписание: {description}");
            }
        }

        public static string GetContent(Cookie token)
        {
            string Content = null;
            string Url = "https://edu.permaviat.ru/my/courses.php";
            Debug.WriteLine($"Выполняем запрос: {Url}");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(token);

            using (HttpWebResponse Response = (HttpWebResponse)request.GetResponse())
            {
                Debug.WriteLine($"Статус выполнения: {Response.StatusCode}");

                Content = new StreamReader(Response.GetResponseStream()).ReadToEnd();
            }
             return Content;
        }
        public static Cookie SingIn(string login, string password)
        {
            Cookie token = null;

            string Url = "https://edu.permaviat.ru/my/courses.php";

            Debug.WriteLine($"Выполняем запрос: {Url}");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CookieContainer = new CookieContainer();
            byte[] Data = Encoding.ASCII.GetBytes($"login={login}&password={password}");
            request.ContentLength = Data.Length;
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(Data, 0, Data.Length);
            }
            using (HttpWebResponse Response = (HttpWebResponse)request.GetResponse())
            {
                string ResponseFromServer= new StreamReader(Response.GetResponseStream()).ReadToEnd();
                Console.WriteLine(ResponseFromServer);

                token = Response.Cookies["token"];
            }
            return token;
        }
    }
}
