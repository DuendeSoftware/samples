using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client;
using Microsoft.Owin.Hosting;

namespace SimpleApi
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = Urls.SampleOwinApi;

            // Start OWIN host 
            using (WebApp.Start<Startup>(url: baseAddress))
            {
                Console.WriteLine("Simple API Running. Press [enter] to quit...");
                Console.ReadLine();
            }
        }
    }
}
