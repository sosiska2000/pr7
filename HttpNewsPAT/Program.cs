using System;
using System.Collections.Generic;
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
            using (HttpWebResponse Response = (HttpWebResponse)Request.GetResponse()){
                Console.WriteLine(Response.StatusDescription);

                using (Stream DataStream = Response.GetResponseStream()) {
                    using (StreamReader Reader = new StreamReader(DataStream))
                    {
                        string ResponseFromServer = Reader.ReadToEnd();
                        Console.WriteLine(ResponseFromServer);
                    }
                }
            }
            Console.Read();
        }
    }
}
