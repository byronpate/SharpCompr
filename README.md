SharpCompr
==========

A C# File Comparison Library

SharpCompr is .NET Library for comparing any file using their File Hashes. The intended goal is to provide a quick comparision.

I originally wrote this for use with Powershell. But it can be used in other libraries or runtimes.


Powershell Example

$dllPath = "C:\Code\DotNet\SharpCompr\\bin\SharpCompr.dll"

### Load SharpSVN ###
if (!(Test-Path $DllPath)){
	Write-Error "Could not locate $DllPath. Check if present and rerun"
	exit
}
$currentScriptDirectory = Get-Location
[System.IO.Directory]::SetCurrentDirectory($currentScriptDirectory)
[Reflection.Assembly]::LoadFile($DllPath)

# Create SharpSVN Client Object
[SharpCompr.Compare]::CompareFiles("\\ATL0FLS01\Test1\TestLib.dll","\\ATL0FLS01\Test2\TestLib.dll", $true)
