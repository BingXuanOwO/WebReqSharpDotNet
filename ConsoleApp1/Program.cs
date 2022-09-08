using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebReqSharpDotNet;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Request request = new Request()
            {
                Type = RequestType.POST,
                Url = "https://www.runoob.com/try/ajax/demo_post2.php",
                TextDatas = new List<TextData>()
                {
                    new TextData(){Name="fname",Context="awa"},
                    new TextData(){Name="lname",Context="qwq"}
                }
            };
            int[] ints = new int[10]
            {
                1,2,3,4,5,6,7,8,9,10,
            };
            request.Send();
            // Console.WriteLine(Encoding.UTF8.GetString(request.GetResponseBytes()));
            Console.WriteLine(request.GetResponseString());
            Console.ReadKey();
        }
    }
}
