using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoapToRest;

namespace SoapToRestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Do you have an existing source account? (Y/n) ");
            ConsoleKeyInfo existingAccount = Console.ReadKey();
            Console.WriteLine();

            
            string src_user = "";
            string src_pass;

            if (existingAccount.Key == ConsoleKey.Y || existingAccount.Key == ConsoleKey.Enter)
            {
                Console.Write("Username: ");
                src_user = Console.ReadLine();

                Console.Write("Password (not hidden): ");
                src_pass = Console.ReadLine();
            }
            else
            {
                Console.Write("Desired username: ");
                src_user = Console.ReadLine();

                src_pass = TicketImport.CreateUser(src_user);
                Console.WriteLine(src_pass);
                src_pass = src_pass.Substring(src_pass.IndexOf(":") + 2);
            }

            Console.Write("Enter your eCourt URL (e.g. https://server.domain/ecourtpath/): ");
            string eC_URL = Console.ReadLine();

            Console.Write("Enter your eCourt username: ");
            string eC_user = Console.ReadLine();

            Console.Write("Enter your eCourt password (not hidden): ");
            string eC_pass = Console.ReadLine();


            TicketImport _ticketImport = new TicketImport(
                "https://customdev.journaltech.com/api/soap/TicketAPI.svc?wsdl",
                src_user,
                src_pass,
                eC_URL,
                TicketImport.GetEcourtUserAuthToken(eC_user, eC_pass)
            );


            Console.WriteLine();
            Console.Write("Ready to roll! Press any key to import all new tickets... ");
            Console.ReadKey();
            Console.WriteLine();

            _ticketImport.ImportNewTickets();

            Console.WriteLine("Complete!");
        }
    }
}
