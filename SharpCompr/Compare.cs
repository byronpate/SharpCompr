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
        private static System.Collections.Generic.Dictionary<string, string> _Dir1Files;
        private static System.Collections.Generic.Dictionary<string, string> _Dir2Files;

        public static string GetFileHash(string filePath, bool verbose = false)
        {
            if (System.IO.File.Exists(@filePath))
            {
                // File Exists
                return FileHash(filePath);
            }
            else
            {
                Write(filePath + " does NOT exist.");
                return "";
            }
        }

        public static string FindHashMatch(string filePath, string searchPath)
        {
            string hash = FileHash(filePath);
            string fileName = Path.GetFileName(filePath);
            string result = "";

            string[] files = Directory.GetFiles(@searchPath, fileName, SearchOption.AllDirectories);

            foreach (string f in files)
            {
                string fhash = FileHash(f);

                if (fhash == hash)
                {
                    result = f;
                    break;
                }
            }

            return result;

        }
        public static bool CompareFiles(string filePath1, string filePath2, bool verbose = false)
        {
            _verbose = verbose;

            //// Check to see if the file exists.
            //FileInfo fInfo = new FileInfo(filePath1);

            //// You can throw a personalized exception if  
            //// the file does not exist. 
            //if (!fInfo.Exists)
            //{
            //    Write(filePath1 + " does NOT exist.");
            //    return false; 
            //}

            //// Check to see if the file exists.
            //FileInfo fInfo2 = new FileInfo(filePath2);

            //// You can throw a personalized exception if  
            //// the file does not exist. 
            //if (!fInfo2.Exists)
            //{
            //    Write(filePath2 + " does NOT exist!");
            //    return false;
            //}

            // Check if both files exist first
            if (System.IO.File.Exists(@filePath1))
            {
                // File Exists
            }
            else
            {
                Write(filePath1 + " does NOT exist.");
                return false;
            }

            if (System.IO.File.Exists(@filePath2))
            {
                // File Exists
            }
            else
            {
                Write(filePath2 + " does Not exist.");
                return false;
            }

            // Compare Files
        
            return CompareFileHashes(filePath1, filePath2);
        }

        public static bool CompareDirectories(string directoryPath1, string directoryPath2, bool verbose = false)
        {
            _Dir1Files = new System.Collections.Generic.Dictionary<string, string>();
            _Dir2Files = new System.Collections.Generic.Dictionary<string, string>();

            // Check if both files exist first
            if (System.IO.Directory.Exists(directoryPath1) == false)
            {
                Write(directoryPath1 + " does not exist.");
                return false;
            }

            if (System.IO.File.Exists(directoryPath2) == false)
            {
                Write(directoryPath2 + " does not exist.");
                return false;
            }

            // Compare Directories
            return CompareDirectoryHashes(directoryPath1, directoryPath2);
        }
        private static string FileHash(string filePath1)
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

        private static string GetHashString(string fileName)
        {
            byte[] fileHash = GenerateHash(fileName);

            string hashstr1;
           
            hashstr1 = BitConverter.ToString(fileHash);

            return hashstr1;
                
        }
        private static byte[] GenerateHash(string fileName)
        {
            // Create a HashAlgorithm object
            HashAlgorithm hash = HashAlgorithm.Create();

            using (FileStream fileStream1 = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // Compute file hashes
                return hash.ComputeHash(fileStream1);
            }
        }
    
        private static void Write(string msg)
        {
            if (_verbose){ Console.WriteLine(msg); }
        }

        private static bool CompareDirectoryHashes(string dir1, string dir2)
        {
            // Build Dir1 Files
            BuildDir1List(dir1);

            // Build Dir2 Files
            BuildDir2List(dir2);

            foreach (string file1 in _Dir1Files.Keys)
            {
               string hash1 = _Dir1Files[file1];
               string file2 = file1.Replace(dir1, dir2);

                if (_Dir2Files.ContainsKey(file2))
                {
                    // Compare the Hash
                    string hash2 = _Dir2Files[file2];

                    if (hash1 == hash2)
                    { }
                    else
                    {
                        Write(file1 + ": " + hash1);
                        Write(file2 + ": " + hash2);
                        return false;
                    }
                }
                else
                {
                    Write(file2 + " does not exist");
                    return false;
                }
            }
            return true;
        }

        private static void BuildDir1List(string path)
        {
            foreach (SharpCompr.FileData fd in FastDirectoryEnumerator.EnumerateFiles(path, System.IO.SearchOption.AllDirectories))
            {
                try
                {
                    _Dir1Files.Add(fd.Path.ToLower(), GetHashString(fd.Path.ToLower()));
                }
                catch (Exception ex)
                {
          
                }
            }
        }

        private static void BuildDir2List(string path)
        {
            foreach (SharpCompr.FileData fd in FastDirectoryEnumerator.EnumerateFiles(path, System.IO.SearchOption.AllDirectories))
            {
                try
                {
                    _Dir2Files.Add(fd.Path.ToLower(), GetHashString(fd.Path.ToLower()));
                }
                catch (Exception ex)
                {

                }
            }
        }
  
    }
}
