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
            WebRequest Request = WebRequest.Create("http://news.permaviat.ru/main");
            using (HttpWebResponse Response = (HttpWebResponse)Request.GetResponse())
            {
                Console.WriteLine(Response.StatusDescription);

                using (Stream DataStream = Response.GetResponseStream())
                {
                    using (StreamReader Reader = new StreamReader(DataStream))
                    {
                        string ResponseFromServer = Reader.ReadToEnd();
                        Console.WriteLine(ResponseFromServer);
                    }
                }
            }
            Console.Read();
        }
        public static void SingIn(string login, string password)
        {
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
            }
        }
    }
}
