using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpComprCmd
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SharpCompr 0.1");
            Console.WriteLine("");

            string file1 = args[0];
            string file2 = args[1];
            string arg1 = "/c";

            if (!(string.IsNullOrEmpty(args[2])))
            {
                arg1 = args[2];
                            }

            if (arg1.ToLower() == "/c")
            {
                Console.WriteLine("File 1: " + file1);
                Console.WriteLine("File 2: " + file2);

                Console.WriteLine("");
                bool result = SharpCompr.Compare.CompareFiles(file1, file2, true);

                if (result)
                {
                    Console.WriteLine("File Match");
                }
                else
                {
                    Console.WriteLine("Files do NOT Match");
                }
            }else
            {
                Console.WriteLine("Comparison File: " + file1);
                Console.WriteLine("Search Path: " + file2);

                Console.WriteLine("");
                string result = SharpCompr.Compare.FindHashMatch(file1, file2);
                Console.WriteLine("Search Result: " + result);
            }
            
        }
    }
}
