using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Blueprints;

public static class FileSystemHelper
{
	private const FileOptions FileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;

	private const int BufferSize = 4096;

	public static async Task MoveFolderToDirectory(string folderPath, string directoryTarget)
	{
		if (!Directory.Exists(folderPath))
		{
			Debug.LogError("Couldn't move folder '" + folderPath + "' to '" + directoryTarget + "'");
			return;
		}
		if (!Directory.Exists(directoryTarget))
		{
			Directory.CreateDirectory(directoryTarget);
		}
		string[] files = Directory.GetFiles(folderPath);
		string[] array = files;
		foreach (string obj in array)
		{
			string fileName = Path.GetFileName(obj);
			string destinationFile = Path.Combine(directoryTarget, fileName);
			await CopyFileAsync(obj, destinationFile);
		}
	}

	private static async Task CopyFileAsync(string sourceFile, string destinationFile)
	{
		await using FileStream sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
		await using FileStream destinationStream = new FileStream(destinationFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
		await sourceStream.CopyToAsync(destinationStream, 4096).ConfigureAwait(continueOnCapturedContext: false);
	}

	public static void DeleteDirectory(string directoryPath)
	{
		if (!Directory.Exists(directoryPath))
		{
			return;
		}
		try
		{
			Directory.Delete(directoryPath, recursive: true);
		}
		catch (IOException ex)
		{
			Debug.LogError("Error deleting directory '" + directoryPath + "': " + ex.Message);
		}
	}

	public static string MakeValidFilename(string filename)
	{
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			filename = filename.Replace(oldChar, '_');
		}
		return filename;
	}
}
