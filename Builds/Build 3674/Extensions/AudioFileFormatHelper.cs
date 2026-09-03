using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Extensions;

public static class AudioFileFormatHelper
{
	private const AudioType FlacAudioType = (AudioType)7;

	private const int AudioHeaderLength = 12;

	private const int FormatTagOffset = 8;

	private const byte MpegAudioFrameFirstByte = byte.MaxValue;

	private const string MpegTagPrefix = "ID3";

	private const string OggTag = "OggS";

	private const string FlacTag = "fLaC";

	private const string WavContainerTag = "RIFF";

	private const string WavFormatTag = "WAVE";

	private const string AiffContainerTag = "FORM";

	private const string AiffFormatTag = "AIFF";

	private const string AiffCompressedFormatTag = "AIFC";

	private static readonly string[] UnsupportedAudioExtensions = new string[7] { ".m4a", ".m4b", ".aac", ".wma", ".opus", ".mid", ".midi" };

	private static readonly Dictionary<string, bool> ValidatedFiles = new Dictionary<string, bool>();

	public static AudioType GetAudioTypeFromExtension(string filePath)
	{
		return Path.GetExtension(filePath).ToLowerInvariant() switch
		{
			".wav" => AudioType.WAV, 
			".ogg" => AudioType.OGGVORBIS, 
			".aiff" => AudioType.AIFF, 
			".aif" => AudioType.AIFF, 
			".flac" => (AudioType)7, 
			".mp3" => AudioType.MPEG, 
			".mp2" => AudioType.MPEG, 
			_ => AudioType.UNKNOWN, 
		};
	}

	public static IEnumerator FindUnsupportedAudioFiles(string folderPath, List<string> unsupportedFileNames)
	{
		unsupportedFileNames.Clear();
		string[] files = Directory.GetFiles(folderPath);
		foreach (string filePath in files)
		{
			AudioType audioTypeFromExtension = GetAudioTypeFromExtension(filePath);
			if (audioTypeFromExtension == AudioType.UNKNOWN)
			{
				if (UnsupportedAudioExtensions.InCollection(Path.GetExtension(filePath).ToLowerInvariant()))
				{
					unsupportedFileNames.Add(Path.GetFileName(filePath));
				}
				continue;
			}
			string fileSignature = GetFileSignature(filePath);
			if (!ValidatedFiles.TryGetValue(fileSignature, out var value))
			{
				if (HasKnownAudioHeader(filePath))
				{
					using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(filePath, audioTypeFromExtension);
					((DownloadHandlerAudioClip)www.downloadHandler).compressed = true;
					yield return www.SendWebRequest();
					AudioClip audioClip = ((www.result == UnityWebRequest.Result.Success) ? DownloadHandlerAudioClip.GetContent(www) : null);
					value = audioClip != null && audioClip.samples > 0;
					if (audioClip != null)
					{
						UnityEngine.Object.Destroy(audioClip);
					}
				}
				ValidatedFiles[fileSignature] = value;
			}
			if (!value)
			{
				unsupportedFileNames.Add(Path.GetFileName(filePath));
			}
		}
	}

	public static bool IsKnownUnsupported(string filePath)
	{
		if (ValidatedFiles.TryGetValue(GetFileSignature(filePath), out var value))
		{
			return !value;
		}
		return false;
	}

	public static void MarkSupported(string filePath)
	{
		ValidatedFiles[GetFileSignature(filePath)] = true;
	}

	public static void MarkUnsupported(string filePath)
	{
		ValidatedFiles[GetFileSignature(filePath)] = false;
	}

	private static string GetFileSignature(string filePath)
	{
		return $"{filePath}|{File.GetLastWriteTimeUtc(filePath).Ticks}";
	}

	public static bool HasKnownAudioHeader(string filePath)
	{
		if (!TryReadFileHeader(filePath, out var header))
		{
			return false;
		}
		if (header[0] == byte.MaxValue)
		{
			return true;
		}
		string text = Encoding.ASCII.GetString(header);
		if (text.StartsWith("ID3", StringComparison.Ordinal) || text.StartsWith("OggS", StringComparison.Ordinal) || text.StartsWith("fLaC", StringComparison.Ordinal))
		{
			return true;
		}
		if (text.StartsWith("RIFF", StringComparison.Ordinal))
		{
			return HasFormatTag(text, "WAVE");
		}
		if (text.StartsWith("FORM", StringComparison.Ordinal))
		{
			if (!HasFormatTag(text, "AIFF"))
			{
				return HasFormatTag(text, "AIFC");
			}
			return true;
		}
		return false;
	}

	private static bool TryReadFileHeader(string filePath, out byte[] header)
	{
		header = new byte[12];
		try
		{
			using FileStream fileStream = File.OpenRead(filePath);
			return fileStream.Read(header, 0, header.Length) == header.Length;
		}
		catch (IOException)
		{
			return false;
		}
		catch (UnauthorizedAccessException)
		{
			return false;
		}
	}

	private static bool HasFormatTag(string headerText, string formatTag)
	{
		return string.CompareOrdinal(headerText, 8, formatTag, 0, formatTag.Length) == 0;
	}
}
