using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HttpNewsPAT
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Cookie token = SingIn("user", "user");
            GetContent(token);
            //WebRequest Request = WebRequest.Create("http://news.permaviat.ru/main");
            //using (HttpWebResponse Response = (HttpWebResponse)Request.GetResponse())
            //{
            //    Console.WriteLine(Response.StatusDescription);

            //    using (Stream DataStream = Response.GetResponseStream())
            //    {
            //        using (StreamReader Reader = new StreamReader(DataStream))
            //        {
            //            string ResponseFromServer = Reader.ReadToEnd();
            //            Console.WriteLine(ResponseFromServer);
            //        }
            //    }
            //}
            //Console.Read();

            Console.Read();
        }
        public static string GetContent(Cookie token)
        {
            string Content = null;
            string Url = "http://news.permaviat.ru/main";
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

            string Url = "http://news.permaviat.ru/ajax/login.php";

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
