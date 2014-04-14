using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EnvCmd.Common
{
    public class FileSync
    {
        // Central switch to turn verbose logging on/off.
        public const bool _VERBOSE = true;
        public const bool _INFO = true;

        private string _source;
        private string _target;
        private FileSyncOptions _options;
        private List<string> _folders;
        private List<string> _files;

        public FileSync(string source, string target, FileSyncOptions options)
        {
            _source = source;
            _target = target;
            _options = options;
            _files = new List<string>();
            _folders = new List<string>();
        }

        private static void VerboseLog(string text, params object[] args)
        {
            verboseLog(string.Format(text, args));
        }

        private static void verboseLog(string text)
        {
            if (_VERBOSE) { Output.Write(text);}
        }

        private static void InfoLog(string text)
        {
            if (_INFO) { Output.Write(text); }
        }

        public static void Sync(string source, string target)
        {
            var fsOptions = new FileSyncOptions
            {
                FilesPattern = "*.*",
                RecurseFolders = true,
                CopyEmptyFolders = true,
                CopyHiddenAndSystemFiles = false,
                OverwriteExistingFiles = false,
                CopyOnlyIfSourceIsNewer = true,
                FoldersPattern = "*"
            }
            .AddExcludeSubStrings("\\.svn\\", "\\_svn\\", "\\_Temporary\\");

            var xc = new FileSync(source, target, fsOptions);
            
            xc.CopyFolderTree(xc._source, xc._target);
            xc.PrintSummery();
        }

        public static void Audit(string source, string target)
        {
            var fsOptions = new FileSyncOptions
            {
                FilesPattern = "*.*",
                RecurseFolders = true,
                CopyEmptyFolders = true,
                CopyHiddenAndSystemFiles = false,
                OverwriteExistingFiles = false,
                CopyOnlyIfSourceIsNewer = true,
                FoldersPattern = "*",
                AuditOnly = true 
            }
           .AddExcludeSubStrings("\\.svn\\", "\\_svn\\", "\\_Temporary\\");

            var xc = new FileSync(source, target, fsOptions);

            xc.CopyFolderTree(xc._source, xc._target);
            xc.PrintSummery();
        }

        private static bool doesStringNotContainSubString(string searchIn, List<string> subStrings)
        {
            return !doesStringContainSubString(searchIn, subStrings);
        }

        private static bool doesStringContainSubString(string searchIn, List<string> subStrings)
        {
            if (string.IsNullOrEmpty(searchIn))
            {
                return false;
            }
            else
            {
                if (subStrings.Count <= 0)
                {
                    // None present means "ALL".
                    return true;
                }
                else
                {
                    foreach (var subString in subStrings)
                    {
                        if (searchIn.IndexOf(subString, StringComparison.InvariantCultureIgnoreCase) >= 0)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }
        }

        private static string PathHelperCombine(string path1, string path2)
        {
            if (string.IsNullOrEmpty(path1))
            {
                return path2;
            }
            else if (string.IsNullOrEmpty(path2))
            {
                return path1;
            }
            else
            {
                path1 = path1.TrimEnd('\\', '/').Replace('/', '\\');
                path2 = path2.TrimStart('\\', '/').Replace('/', '\\');

                return path1 + "\\" + path2;
            }
        }

        private void CopyFolderTree(string sourceFolderPath, string destinationFolderPath)
        {
           
            var dst = new DirectoryInfo(destinationFolderPath);

            //CheckCreateFolder(dst);

            var sourceFilePaths = getFiles(sourceFolderPath, _options.FilesPattern);

            if (sourceFilePaths != null)
            {
                foreach (var sourceFilePath in sourceFilePaths)
                {
                    var fileName = Path.GetFileName(sourceFilePath);
                    var destinationFilePath =
                        PathHelperCombine(
                            dst.FullName,
                            fileName);

                    bool copy = OnProgressFile(new FileInfo(sourceFilePath), new FileInfo(destinationFilePath));

                    if (copy)
                    {
               
                        CopyFile(sourceFilePath, destinationFilePath);
                    }
                    else
                    {
                
                    }
                }
            }

            if (_options.RecurseFolders)
            {
               // verboseLog("RECURSING folders.");

                var srcFolders = Directory.GetDirectories(sourceFolderPath, _options.FoldersPattern);

                //verboseLog("Got {0} child folders in source folder '{1}' with pattern '{2}'.", srcFolders.Length, sourceFolderPath, options.FoldersPattern);

                if (srcFolders != null)
                {
                    foreach (string srcFolder in srcFolders)
                    {
                        var path = new DirectoryInfo(srcFolder);

                        if (!isFolderEmpty(path) || _options.CopyEmptyFolders)
                        {
                            var diff = srcFolder.Substring(sourceFolderPath.Length);

                            var destinationSubFolderPath = new DirectoryInfo(PathHelperCombine(dst.FullName, diff));

                            bool copy = OnProgressFolder(path, destinationSubFolderPath);

                            if (copy)
                            {
                                //verboseLog("Recursing into folder '{0}' (destination folder '{1}').",
                                //    path.FullName,
                                //    destinationSubFolderPath);

                                // Recurse into.
                                CopyFolderTree(path.FullName, destinationSubFolderPath.FullName);
                            }
                            else
                            {
                                //verboseLog("NOT recursing into folder '{0}' (destination folder '{1}').",
                                //    path.FullName,
                                //    destinationSubFolderPath);
                            }
                        }
                    }
                }
       
            }
            else
            {
                verboseLog("NOT recursing folders.");
            }
        }

        private void PrintSummery()
        {
            Output.WriteSync("================================================");
            Output.WriteSync("Files Copied: " + _files.Count.ToString());
            Output.WriteSync("Folders Created: " + _folders.Count.ToString());
            Output.WriteSync("================================================");

        }
        private static bool isFolderEmpty(
            DirectoryInfo folderPath)
        {
            return folderPath.GetFiles().Length <= 0;
        }

        private bool OnProgressFolder(DirectoryInfo sourceFolderPath, DirectoryInfo destinationFolderPath)
        {
            var sfp = sourceFolderPath.FullName.TrimEnd('\\') + "\\";

            if (_options.ExcludeSubStrings.Count > 0 &&
                doesStringContainSubString(sfp, _options.ExcludeSubStrings))
            {
                return false;
            }
            else
            {
                if (_options.IncludeSubStrings.Count <= 0 ||
                    doesStringContainSubString(sfp, _options.IncludeSubStrings))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private bool OnProgressFile(FileInfo sourceFilePath, FileInfo destinationFilePath)
        {
            if (_options.CopyHiddenAndSystemFiles || !IsSystemOrHiddenFile(sourceFilePath))
            {
                if (!_options.CopyOnlyIfSourceIsNewer || IsFileOneNewerThanFileTwo(sourceFilePath, destinationFilePath))
                {
                    if (_options.OverwriteExistingFiles || !destinationFilePath.Exists)
                    {
                        if (_options.ExcludeSubStrings.Count > 0 &&
                            doesStringContainSubString(sourceFilePath.FullName, _options.ExcludeSubStrings))
                        {
                            return false;
                        }
                        else
                        {
                            if (_options.IncludeSubStrings.Count <= 0 ||
                                doesStringContainSubString(sourceFilePath.FullName, _options.IncludeSubStrings))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private void CopyFile(string sourceFilePath, string destinationFilePath)
        {
            CheckCreateFolder(new FileInfo(destinationFilePath).Directory);

            if (_options.AuditOnly)
            {
                Output.WriteSync("AUDIT: Copy " + sourceFilePath + " to " + destinationFilePath);
             
            }else
            {
                File.Copy(sourceFilePath, destinationFilePath, _options.OverwriteExistingFiles);
            }
            if (!_files.Contains(destinationFilePath))
            {
                _files.Add(destinationFilePath);
            }
        }

        private DirectoryInfo CheckCreateFolder(DirectoryInfo folderPath)
        {
            if (folderPath != null && !folderPath.Exists)
            {
                //verboseLog("Creating folder '{0}'.",
                //    folderPath.FullName);
                if (_options.AuditOnly)
                {
                    Output.WriteSync("AUDIT: Create " + folderPath.FullName);
                }
                else
                {
                    folderPath.Create();
                }

                if (!_folders.Contains(folderPath.FullName))
                {
                    _folders.Add(folderPath.FullName);
                }
              
            }

            return folderPath;
        }

        private static bool IsSystemOrHiddenFile(FileInfo filePath)
        {
            if (filePath == null || !filePath.Exists)
            {
                return false;
            }
            else
            {
                var attributes = filePath.Attributes;

                if ((attributes & FileAttributes.Hidden) != 0 ||
                    (attributes & FileAttributes.System) != 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private static bool IsSystemOrHiddenFolder(DirectoryInfo folderPath)
        {
            if (folderPath == null || !folderPath.Exists)
            {
                return false;
            }
            else
            {
                var attributes = folderPath.Attributes;

                if ((attributes & FileAttributes.Hidden) != 0 ||
                    (attributes & FileAttributes.System) != 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private static bool IsFileOneNewerThanFileTwo(string one, string two)
        {
            if (string.IsNullOrEmpty(one) || !File.Exists(one))
            {
                return false;
            }
            else if (string.IsNullOrEmpty(two) || !File.Exists(two))
            {
                return true;
            }
            else
            {
                var d1 = File.GetLastWriteTime(one);
                var d2 = File.GetLastWriteTime(two);

                var b = d1 > d2;
                return b;
            }
        }

        private static bool IsFileOneNewerThanFileTwo(FileInfo one, FileInfo two)
        {
            if (one == null)
            {
                return false;
            }
            else if (two == null)
            {
                return true;
            }
            else
            {
                return
                    IsFileOneNewerThanFileTwo(
                    one.FullName,
                    two.FullName);
            }
        }

        private static string[] getFiles(string path, string searchPattern)
        {
            return getFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        // Since Directory.GetFiles() does only allow one wildcard a time,
        // split e.g. "*.jpg;*.gif;*.png" into separate items and query for.
        // See http://social.msdn.microsoft.com/Forums/en-US/netfxbcl/thread/b0c31115-f6f0-4de5-a62d-d766a855d4d1
        private static string[] getFiles(string path, string searchPattern, SearchOption searchOption)
        {
            var searchPatterns = searchPattern.Split(new char[] { ';', ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
            var files = new List<string>();
            foreach (string sp in searchPatterns)
            {
                files.AddRange(Directory.GetFiles(path, sp.Trim(), searchOption));
            }

            files.Sort();
            return files.ToArray();
        }
    }

    public class FileSyncOptions
    {
        public FileSyncOptions()
        {
            FilesPattern = "*.*";
            FoldersPattern = "*";
        }

        private readonly List<string> _excludeSubStrings = new List<string>();
        private readonly List<string> _includeSubStrings = new List<string>();

        public bool RecurseFolders { get; set; }
        public bool CopyEmptyFolders { get; set; }
        public bool CopyHiddenAndSystemFiles { get; set; }
        public bool OverwriteExistingFiles { get; set; }
        public bool CopyOnlyIfSourceIsNewer { get; set; }
        public bool AuditOnly { get; set; }
        public List<string> ExcludeSubStrings { get { return _excludeSubStrings; } }
        public List<string> IncludeSubStrings { get { return _includeSubStrings; } }

        // E.g. "*.exe;*.dll"
        public string FilesPattern { get; set; }
        public string FoldersPattern { get; set; }

        public FileSyncOptions AddExcludeSubStrings(
            params string[] items)
        {
            ExcludeSubStrings.AddRange(items);
            return this;
        }

        public FileSyncOptions AddIncludeSubStrings(
            params string[] items)
        {
            IncludeSubStrings.AddRange(items);
            return this;
        }
    }

}
namespace Zeta
{
    /*
    ===========================
    Zeta XCOPY.

        Last modified: 2009-09-19.

    ---------------------------
    Description:

        This is a quickly assembled (but rather well tested and used) piece of
        code to simulate parts of the XCOPY command line tool.

        It has fewer features as XCOPY and is intended for copying folders
        (the original XCOPY works for both files and folders). In addition it is 
        being designed to be used in a console environment (i.e. the operations
        are synchronously and blocking).

        I am using this in a C# Script[1] file to copy files and building a setup 
        with NSIS[2] for our Zeta Test[3] and Zeta Producer[4] software.

    ---------------------------
    Example usage:

        var options = new ZetaFolderXCopyOptions
            {
                FilesPattern = "*.*",
                RecurseFolders = true,
                CopyEmptyFolders = true,
                CopyHiddenAndSystemFiles = true,
                OverwriteExistingFiles = true,
                CopyOnlyIfSourceIsNewer = false,
                FoldersPattern = "*"
            }
            .AddExcludeSubStrings( 
                "\\.svn\\", 
                "\\_svn\\", 
                "\\_Temporary\\" );

        var xc = new ZetaFolderXCopy();
        xc.Copy(
            sourceFolderPath,
            destinationFolderPath,
            options );


    */



  
  
}