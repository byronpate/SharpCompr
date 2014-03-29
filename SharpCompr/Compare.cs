using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SharpCompr
{
    public class Compare
    {
        private static bool _verbose;

        public static bool CompareFiles(string filePath1, string filePath2, bool verbose = false)
        {
            _verbose = verbose;

            // Check if both files exist first
            if (System.IO.File.Exists(filePath1) == false) 
            {
                Write(filePath1 + " does not exist.");
                return false; 
            }

            if (System.IO.File.Exists(filePath2) == false) 
            {
                Write(filePath2 + " does not exist.");
                return false; 
            }

            // Compare Files
            return CompareFileHashes(filePath1, filePath2);
        }

        public static string FileHash(string filePath1)
        {
            byte[] hash = GenerateHash(filePath1);

            return BitConverter.ToString(hash);
        }

        private static bool CompareFileHashes(string fileName1, string fileName2)
        {
            // Compare file sizes before continuing. 
            // If sizes are equal then compare bytes.
            if (CompareFileSizes(fileName1, fileName2))
            {

                // Declare byte arrays to store our file hashes
                byte[] fileHash1 = GenerateHash(fileName1);
                byte[] fileHash2 = GenerateHash(fileName2);

                // Return a comparison of the Hash Strings
                string hashstr1;
                string hashstr2;

                hashstr1 = BitConverter.ToString(fileHash1); 
                hashstr2 = BitConverter.ToString(fileHash2);

                Write(fileName1 + ": " + hashstr1);
                Write(fileName2 + ": " + hashstr2);

                return hashstr1 == hashstr2;
            }
            else
            {
                // File Sizes are not equal
                Write("File Sizes are not equal");
                return false;
            }
        }

        private static bool CompareFileSizes(string fileName1, string fileName2)
        {
            bool fileSizeEqual = true;

            // Create System.IO.FileInfo objects for both files
            FileInfo fileInfo1 = new FileInfo(fileName1);
            FileInfo fileInfo2 = new FileInfo(fileName2);

            // Compare file sizes
            if (fileInfo1.Length != fileInfo2.Length)
            {
                // File sizes are not equal therefore files are not identical
                fileSizeEqual = false;
            }

            return fileSizeEqual;
        }

        private static byte[] GenerateHash(string fileName)
        {
            // Create a HashAlgorithm object
            HashAlgorithm hash = HashAlgorithm.Create();

            using (FileStream fileStream1 = new FileStream(fileName, FileMode.Open))
            {
                // Compute file hashes
                return hash.ComputeHash(fileStream1);
            }
        }
    
        private static void Write(string msg)
        {
            if (_verbose){ Console.WriteLine(msg); }
        }
    }
}
