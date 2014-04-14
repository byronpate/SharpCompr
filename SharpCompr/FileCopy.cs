
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
public class FileCopy
{
	private string _Source;
	private string _Destination;

	private int _ID;
	public event CopySuccessEventHandler CopySuccess;
	public delegate void CopySuccessEventHandler(int id, string file);
	public event CopyFailedEventHandler CopyFailed;
	public delegate void CopyFailedEventHandler(int id, string source, string destination);

	public FileCopy(string source, string destination, int id)
	{
		try {
			_Source = source;
			_Destination = destination;
			_ID = id;
		} catch (Exception ex) {
			//LogManager.WriteError(id, ex);
		}

	}

	public void Start(object stateInfo)
	{
		try {
			System.IO.File.Copy(_Source, _Destination, true);
			if (CopySuccess != null) {
				CopySuccess(_ID, _Destination);
			}
			//Catch unauthex As UnauthorizedAccessException
			//    ' Reset the ReadOnly Flag
			//    Try
			//        IO.File.SetAttributes(_Destination, IO.FileAttributes.Normal)
			//        IO.File.Copy(_Source, _Destination, True)
			//    Catch ex As Exception
			//        LogManager.WriteError(_ID, ex)
			//        RaiseEvent CopyFailed(_ID, _Source, _Destination)
			//    End Try
		} catch (Exception ex) {
			//LogManager.WriteError(_ID, ex);
			if (CopyFailed != null) {
				CopyFailed(_ID, _Source, _Destination);
			}
		} finally {
			// ThreadPool Change
			((System.Threading.AutoResetEvent)stateInfo).Set();
		}
	}
}
