using System;

namespace BlackSlope.Hosts.ConsoleApp
{
    static public class Program
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Not localizing Console App")]
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
