using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

namespace RequirementsChecklistTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("\nSystem Requirements Check:");
            Console.WriteLine("\n\nAre you using Python? y/n");
            ConsoleKeyInfo yesPY = Console.ReadKey();

            //TODO: Checker for Web Api dependencies
            //Console.WriteLine("\n\nAre you using Web Api? y/n");
            //ConsoleKeyInfo yesWebApi = Console.ReadKey();

            Console.WriteLine("\n");

            if (yesPY.Key == ConsoleKey.Y)
            {
                var python = new PythonChecker();
                python.Check();
                Console.WriteLine(python.ToString());
            }

            var netFrame = new NetFrameworkChecker();
            netFrame.Check();
            Console.WriteLine(netFrame.ToString());


            var sql = new SQLServerChecker();
            sql.Check();
            Console.WriteLine(sql.ToString());


            var vs = new VisualStudioChecker();
            vs.Check();
            Console.WriteLine(vs.ToString());

            OSRequirementChecker os = new OSRequirementChecker();
            os.Check();
            Console.WriteLine(os.ToString());

            RAMRequirementChecker ram = new RAMRequirementChecker();
            ram.Check();
            Console.WriteLine(ram.ToString());

            Console.ReadLine();

        }
    }
}
