using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
namespace EnvCmd.Common{
public class RepliCopy
{

	/// <summary>
	/// RepliCopy compresses and transfers Files to TargetFileSendingThread
	/// </summary>
	/// <remarks>
	/// 1. Send Files Info to Target
	/// 2. Receive File Requests from Target (as an arraylist of file paths)
	/// 3. Compress requested file into Cab
	/// 4. Transfer Cab to Target
	/// 5. Delete Cab
	///</remarks>
	/// 

	#region " Private Instances "
	private ManifestManager _Output;
    private List<string> _Errors;
	private const int _SleepTime = 1000;
	// Define static variables shared by class methods.

	private static System.Text.StringBuilder _Out = null;
	private bool _Send = true;
	private System.Collections.Generic.Queue<string> _ToSend;
	private int _Max = 11;
	private int _Count = 0;
	private int _RetryCounter = 0;
	private int _RetryMax = 200;
	private int _Sent = 0;
	private int _FileCount;

	private object _Lock = new object();
	private string _Target;
	private System.DateTime _StartTime;
	private bool _ValidationComplete = false;

	private bool _FirstValidationAttempt;
	private bool _RequestsComplete = false;
	private System.Collections.Generic.Dictionary<string, FileData> _SourceFiles;
	private System.Collections.Generic.Dictionary<string, FileData> _TargetFiles;
	private bool _TargetIsEmpty = false;
	private string _SourcePath;
	private string _DestinationPath;
	private int _RequestID;
	private bool _UseMultiThreading;
		#endregion
	private int _ThreadCount;

	#region " Constructor "
	public RepliCopy(string source, string destination, int requestId)
	{
		// Initialize Objects for Use

		_RequestID = requestId;
		_SourcePath = source.ToLower();
		_DestinationPath = destination.ToLower();
		//_Output = new ManifestManager(Guid.NewGuid, requestId);
        _Errors = new List<string>();
		_SourceFiles = new System.Collections.Generic.Dictionary<string, FileData>();
		_TargetFiles = new System.Collections.Generic.Dictionary<string, FileData>();
	}
	#endregion

	#region " Public Methods "

	/// <summary>
	/// Start RepliCab Processing
	/// </summary>
	/// <remarks>Thread Start Uses this</remarks>
	public List<string> Mirror()
	{

		// Set Start Timestamp
		_StartTime = DateTime.Now;
		LogMessage("RepliCopy " + _SourcePath);

		// Initialize and Error Check
		Initialize();

		// Check for Initialization Errors
		if (_Errors.Count > 0) {
			Output.WriteError("Failing Processing");

			return _Errors;
		} else {
			LogMessage("Initialized Successfully");
		}

		try {
			// Generate the Dictionary of Source Files
			BuildSourceList(_SourcePath);
			LogMessage("Built Source List of Files");

			// Generate the Dictionary of Destination Files
			BuildTargetList(_DestinationPath);
			LogMessage("Built Target List of Files");

			// Purge Files from Target that are Not in Source
			PurgeFiles();
			LogMessage("Ran Purge of Target Files");

			// Send Directory Purge Command to Target
			ManageDirectories();
			LogMessage("Ran Purge of Target Directories");

			// Copy Files
			ProcessFiles();
			LogMessage("Copied Changes");

			// Perform Validation
			Validation();
			LogMessage("Validated Changes");

			// Send Finished Command to target
			//NotifyUI("Sending Finish........")
			Finish();
		} catch (Exception ex) {
			Output.WriteError(ex.ToString());
			LogMessage(ex.ToString());

			// Mark Output as Failed
			//  _Output.Status = OutputStatus.Failed
			// _Output.Add(ex.ToString)

			// Send error message to Trace
			//NotifyUI(ex.ToString)

			// Raise Error Event to Engine
			// RaiseEvent Failed(_Output)
		}
		return _Errors;
	}

	public ManifestManager Copy()
	{
		LogMessage("Copying " + _DestinationPath);

		Initialize();

		Copy(_SourcePath, _DestinationPath);

		Finish();

		return _Errors;
	}
	#endregion

	#region " Core Processing Methods "
	private void Initialize()
	{
		try {
			if (Directory.Exists(_SourcePath) == false) {
				Output.WriteError("Source Path (" + _SourcePath + ") does not exist, failing.");
			}

			if (Directory.Exists(_DestinationPath) == false) {
				Directory.CreateDirectory(_DestinationPath);
			}

			//_Output.InitializeManifest(_SourcePath, _DestinationPath);
		} catch (Exception ex) {
			ErrorOut(ex, "Error Initializing RepliCopy ");
		}

		try {
			bool result = false;

			result = Convert.ToBoolean(ConfigurationManager.AppSettings["EnableMultiThreading"]);
            
			if (result == true) {
				_UseMultiThreading = true;

				int tcount = 0;

				tcount = Convert.ToInt32(ConfigurationManager.AppSettings["NumberOfThreads"]);
                
				_ThreadCount = tcount;
			} else {
				_UseMultiThreading = false;
				_ThreadCount = 0;
			}
		} catch (Exception ex) {
            EnvCmd.Common.Output.WriteError("Error reading EnableMultiThreading from app.config");
			Output.WriteError(ex.ToString());
			_UseMultiThreading = false;
			_ThreadCount = 0;
		}
	}

	private void ProcessFiles()
	{
		System.Threading.ThreadPool.SetMaxThreads(_ThreadCount, 10);
		System.Threading.ThreadPool.SetMinThreads(3, 2);

		int i = 0;
		foreach (FileData fd in _SourceFiles.Values) {
			//fd = fd_loopVariable;
			if (RequestFileCopy(fd)) {
				string destFile = SourceFileToTargetFile(fd);

				if (_UseMultiThreading) {
					i += 1;
					//_Output.AddManifestEntry(destFile, fd.Size, fd.LastWriteTime);
					FileCopyThread(fd.Path, destFile);
				} else {
					i += 1;
					File.Copy(fd.Path, destFile, true);
				}

			} else {
				// Files are Identical, No Copy
			}
		}

		while (i > _Sent) {
			System.Threading.Thread.Sleep(2000);
			LogMessage("Copying changed files..." + _Sent + "/" + i);
		}
	}

	private void Finish()
	{
		//_er.CommitManifest();

		LogMessage("Finished Operations");
	}

	private void BuildSourceList(string path)
	{
		foreach (FileData fd in FastDirectoryEnumerator.EnumerateFiles(path, System.IO.SearchOption.AllDirectories)) {
			try {
				_SourceFiles.Add(fd.Path.ToLower(), fd);
			} catch (Exception ex) {
				Output.WriteError("Error adding file to Source List -> (" + fd.Path.ToLower() + ") " + ex.ToString());
				LogMessage("Error adding file to Source List -> (" + fd.Path.ToLower() + ") " + ex.ToString());
			}
		}
	}

	private void PurgeFiles()
	{
		try {
			foreach (string f in _TargetFiles.Keys) {
				// LogMessage("Checking " & f)
				string srcFile = f.Replace(_DestinationPath, _SourcePath);

                if (_SourceFiles.ContainsKey(srcFile.ToLower()) == false)
                {
					//  LogMessage("Purging " & srcFile)
					try {
						File.Delete(f);
						//_Output.AddManifestEntryDelete(f);
					} catch (UnauthorizedAccessException unauthex) {
						// Reset the ReadOnly Flag
						File.SetAttributes(f, FileAttributes.Normal);
						IO.File.Delete(f);
						_Output.AddManifestEntryDelete(f);
					}

				}
			}
		} catch (Exception ex) {
			ErrorOut(ex, "Error during file purge ");
		}
	}

	private void BuildTargetList(string path)
	{
		foreach (FileData fd in FastDirectoryEnumerator.EnumerateFiles(path, System.IO.SearchOption.AllDirectories)) {
            _TargetFiles.Add(fd.Path.ToLower(), fd);
		}
	}

	private void Validation()
	{
		try {
			// Check for any exceptions
			if (_Output.ContainsErrors) {
				_Output.Manifest.Successful = false;

				return;
			} else {
				_Output.Manifest.Successful = true;
			}

			// Validate File Counts
			LogMessage("Files Copied: " + _Sent.ToString() + ". File Request Count: " + _Output.CopyFileCount.ToString);
			if (_Sent != _Output.CopyFileCount) {
				LogMessage("Files requested do not equal the number copied. Marking as failure");
				_Output.Manifest.Successful = false;
				return;
			} else {
				_Output.Manifest.Successful = true;
			}

			// Validate Updated File Attributes
			int failed = 0;
			int success = 0;
			if (_Output.CopyFileCount > 0) {
				foreach (Assurant.AMM.Data.Migrations.ManifestEntry entry in _Output.Files.Values) {
					if (entry.Action.ToUpper == "COPY") {
						IO.FileInfo fi = default(IO.FileInfo);

						fi = new IO.FileInfo(entry.FileName);

						if ((fi.Length != entry.Size) | (fi.LastWriteTime != entry.LastModified)) {
							// Files Do Not Match
							failed += 1;
						} else {
							// Files Match
							success += 1;
						}
					}
				}

				LogMessage("Validated " + _Output.CopyFileCount.ToString + " files.");
				LogMessage(failed + " failed validation.");
				LogMessage(success + " passed validation.");

				if (failed > 0) {
					_Output.Manifest.Successful = false;
					return;
				} else {
					_Output.Manifest.Successful = true;
				}
			} else {
				LogMessage("No file copies to be validated");
			}
		} catch (Exception ex) {
			ErrorOut(ex, "Error duing Validation ");
		}
	}
	#endregion

	#region " IO Methods "

	/// <summary>
	/// Send Directory Purges to the Target
	/// </summary>
	/// <remarks></remarks>

	private void ManageDirectories()
	{
		//First create Obj to send
		ArrayList arl = new ArrayList();

		arl = GetSubDirList(_SourcePath);

		try {
			// Purge Directories that are not in source
            //DirectoryPurge p = new DirectoryPurge(_RequestID, _DestinationPath, arl);
            //p.Start();

			// Create any missing Directories
			DirectoryCreation.CreateDirectories(arl);
		} catch (Exception ex) {
			ErrorOut(ex);
		}
	}

	/// <summary>
	/// Retrieves Arraylist of Subdirectories
	/// </summary>
	/// <param name="path">Full Path to Directory</param>
	/// <returns>Arraylist of Strings</returns>
	/// <remarks></remarks>
	private ArrayList GetSubDirList(string path)
	{
		ArrayList arl = new ArrayList();
		DirectoryInfo dir = new DirectoryInfo(path);

		foreach (DirectoryInfo d in dir.GetDirectories("*", SearchOption.AllDirectories)) {
			// arl.Add(d.FullName.ToUpper)
			arl.Add(d.FullName.Replace(_SourcePath, _DestinationPath));
		}

		return arl;
	}

	private void Copy(string source, string target)
	{
		try {
			System.IO.DirectoryInfo sourceDir = new System.IO.DirectoryInfo(source);
			//System.IO.FileSystemInfo fsInfo = default(System.IO.FileSystemInfo);

			if (!System.IO.Directory.Exists(target)) {
				System.IO.Directory.CreateDirectory(target);
			}

			foreach (FileSystemInfo fsInfo in sourceDir.GetFileSystemInfos()) {
				string strDestFileName = System.IO.Path.Combine(target, fsInfo.Name);

				if (fsInfo is System.IO.FileInfo) {
					// If File Exists remove any ReadOnly Flags
					if (File.Exists(strDestFileName)) {
						File.SetAttributes(strDestFileName, FileAttributes.Normal);
					}
					FileInfo fi = new FileInfo(fsInfo.FullName);
					System.IO.File.Copy(fsInfo.FullName, strDestFileName, true);
					//Logger.WriteSyncLog("Copy " & fsInfo.FullName & " to " & strDestFileName)
					_Output.AddManifestEntry(fsInfo.FullName, fi.Length, fi.LastWriteTime);
					//This will overwrite files that already exist
				} else {
					Copy(fsInfo.FullName, strDestFileName);
				}
			}
		} catch (Exception ex) {
			ErrorOut(ex, "Error during file copy ");
		}
	}

	private bool RequestFileCopy(FileData sourceFile)
	{
		// Check for Existence of a file
		string destFileName = SourceFileToTargetFile(sourceFile);

		if (File.Exists(destFileName)) {
			// File Exists, check version
			FileData destFileData = _TargetFiles.Item(destFileName.ToLower);
			System.DateTime dateTarget = default(System.DateTime);
			System.DateTime dateSource = default(System.DateTime);

			dateTarget = destFileData.LastWriteTime;
			dateSource = sourceFile.LastWriteTime;

			if (!(dateTarget == dateSource)) {
				// Modified Dates don't match
				// DoTrace(TraceLevel.Verbose, "Modified timestamps don't match for " & fi.TargetName)
				//DoTrace(TraceLevel.Verbose, "Source UTC " & dateSource.ToUniversalTime.ToLongTimeString)
				//DoTrace(TraceLevel.Verbose, "Target UTC " & dateTarget.ToUniversalTime.ToLongTimeString)
				//DoTrace(TraceLevel.Verbose, "Source FileTime UTC " & dateFileTime.ToUniversalTime.ToLongTimeString)
				return true;
			} else {
				if (!(sourceFile.Size == destFileData.Size)) {
					//DoTrace(TraceLevel.Verbose, "Size doesn't match for " & fi.TargetName)
					return true;
				} else {
					return false;
				}
			}
		} else {
			// File Not Found, Copy it
			return true;
		}
	}

	#endregion

	#region " Helper Methods "

	private void ErrorOut(Exception ex, string message = "")
	{
		Output.WriteError(message + ex.ToString());
		if (NotifyUI != null) {
			NotifyUI(message);
		}
		_Output.AddError(ex);
	}

	private void LogMessage(string message)
	{
		Output.Write(message);
		if (NotifyUI != null) {
			NotifyUI(message);
		}
	}

	private string SourceFileToTargetFile(FileData sourceFileData)
	{
		return SourceFileToTargetFile(sourceFileData.Path);
	}

	private string SourceFileToTargetFile(string sourceFile)
	{
		return sourceFile.Replace(_SourcePath, _DestinationPath);
	}

	/// <summary>
	/// Queue a File Copy Request
	/// </summary>
	/// <param name="source"></param>
	/// <param name="destination"></param>
	/// <remarks></remarks>

	private void FileCopyThread(string source, string destination)
	{
		System.Threading.AutoResetEvent mainEvent = new System.Threading.AutoResetEvent(false);

		FileCopy rf = new FileCopy(source, destination, _RequestID);

		rf.CopySuccess += HandleCopySuccess;
		rf.CopyFailed += FileSenderFailure;

		_Count += 1;
		System.Threading.ThreadPool.QueueUserWorkItem(rf.Start, mainEvent);
	}

	private void HandleCopySuccess(int id, string file)
	{
		try {
			lock (_Lock) {
				_Count -= 1;
				_Sent += 1;
				_Output.ManifestEntryComplete(file);
			}
		} catch (Exception ex) {
			Output.WriteError(ex.ToString());
		}
	}

	private void FileSenderFailure(int id, string source, string destination)
	{
		LogMessage("File Sending Failure has occured, attempting to retry.");
		LogMessage(destination + " failed.");

		if (_RetryCounter < _RetryMax) {
			_Count -= 1;
			_RetryCounter += 1;
			// Remove Read Only Flag
			IO.File.SetAttributes(destination, IO.FileAttributes.Normal);
			FileCopyThread(source, destination);
		} else {
			_Count -= 1;
		}
	}

	#endregion

	#region " Public Events "

	public event NotifyUIEventHandler NotifyUI;
	public delegate void NotifyUIEventHandler(string message);

	#endregion

}
}
