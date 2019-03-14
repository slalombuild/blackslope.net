using System;

namespace BlackSlope.Hosts.ConsoleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the Blackslope Console");

            // execute program from here
            AuthenticationToken.GetAuthTokenAsync().Wait();

            Console.WriteLine("Press any button to continue...");
            Console.ReadLine();
        }

    }
}
