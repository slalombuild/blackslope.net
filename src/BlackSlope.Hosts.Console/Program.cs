using System;

namespace BlackSlope.Hosts.ConsoleApp
{
    static public class Program
    {
        public static void Main()
        {
            Console.WriteLine("Welcome to the Blackslope Console");

            // execute program from here
            AuthenticationToken.GetAuthTokenAsync().Wait();

            Console.WriteLine("Press any button to continue...");
            Console.ReadLine();
        }

    }
}
