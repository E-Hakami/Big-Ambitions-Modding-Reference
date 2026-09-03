using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using BigAmbitions.SaveSystem.Legacy;
using Extensions;
using Newtonsoft.Json;
using OdinSerializer;
using Player.SaveSystem;
using UnityEngine;

public static class SaveGameSerializationHelper
{
	private static SerializationContext SerializationContext;

	private static JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
	{
		ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
		TypeNameHandling = TypeNameHandling.Auto
	};

	public static byte[] CompressBytes(byte[] data)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Compress);
		gZipStream.Write(data, 0, data.Length);
		gZipStream.Close();
		return memoryStream.ToArray();
	}

	public static byte[] DecompressBytes(byte[] data)
	{
		using MemoryStream stream = new MemoryStream(data);
		using GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress);
		using MemoryStream memoryStream = new MemoryStream();
		gZipStream.CopyTo(memoryStream);
		return memoryStream.ToArray();
	}

	public static bool SerializeJsonData(string path, GameInstance instance)
	{
		try
		{
			string contents = JsonConvert.SerializeObject(instance, Formatting.None, JsonSerializerSettings);
			File.WriteAllText(path, contents);
			return true;
		}
		catch (InvalidOperationException exception)
		{
			Debug.Log("SaveGame serialization failed:");
			Debug.LogException(exception);
			return false;
		}
	}

	public static bool SerializeBinaryData(string path, GameInstance instance, bool compressed = true)
	{
		try
		{
			using FileStream fileStream = File.Create(path);
			using GZipStream gZipStream = new GZipStream(fileStream, System.IO.Compression.CompressionLevel.Optimal);
			if (SerializationContext == null)
			{
				SerializationContext = new SerializationContext();
				SerializationContext.Config.SerializationPolicy = new SaveGameSerializationPolicy();
				SerializationContext.Config.DebugContext.ErrorHandlingPolicy = ErrorHandlingPolicy.ThrowOnErrors;
			}
			SerializationContext.ResetInternalReferences();
			SerializationUtility.SerializeValue(instance, compressed ? ((Stream)gZipStream) : ((Stream)fileStream), DataFormat.Binary, SerializationContext);
			return true;
		}
		catch (InvalidOperationException exception)
		{
			Debug.Log("SaveGame serialization failed:");
			Debug.LogException(exception);
			return false;
		}
	}

	public static bool CompressBinaryData(string path, string outputPath)
	{
		try
		{
			using FileStream fileStream = File.OpenRead(path);
			using GZipStream destination = new GZipStream(File.Create(outputPath), System.IO.Compression.CompressionLevel.Optimal);
			fileStream.CopyTo(destination);
			return true;
		}
		catch (Exception exception)
		{
			Debug.Log("SaveGame compression failed:");
			Debug.LogException(exception);
			return false;
		}
	}

	public static GameInstance DeserializeJsonData(string path)
	{
		return JsonConvert.DeserializeObject<GameInstance>(File.ReadAllText(path), new JsonSerializerSettings
		{
			Converters = new List<JsonConverter>
			{
				new StackConverter()
			},
			TypeNameHandling = TypeNameHandling.Auto
		});
	}

	public static async Task<GameInstance> DeserializeJsonDataAsync(string path, IProgress<(float, string)> progress = null)
	{
		progress?.Report((0.1f, "Reading file..."));
		using StreamReader streamReader = new StreamReader(path);
		Newtonsoft.Json.JsonTextReader jsonReader = new Newtonsoft.Json.JsonTextReader(streamReader);
		try
		{
			JsonSerializer serializer = new JsonSerializer
			{
				TypeNameHandling = TypeNameHandling.Auto
			};
			serializer.Converters.Add(new StackConverter());
			long length = new FileInfo(path).Length;
			progress?.Report((0.5f, "Deserializing (" + length.FormatBytes() + ")..."));
			GameInstance result = await Task.Run(() => serializer.Deserialize<GameInstance>(jsonReader));
			progress?.Report((1f, "Done"));
			return result;
		}
		finally
		{
			if (jsonReader != null)
			{
				((IDisposable)jsonReader).Dispose();
			}
		}
	}

	public static GameInstance DeserializeBinaryData(string path)
	{
		try
		{
			using FileStream stream = File.OpenRead(path);
			using GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress);
			using MemoryStream memoryStream = new MemoryStream();
			gZipStream.CopyTo(memoryStream);
			using MemoryStream stream2 = new MemoryStream(memoryStream.ToArray());
			using BinaryDataReader inner = new BinaryDataReader(stream2, GetDeserializationContext());
			using IntToStringMigratingDataReader reader = new IntToStringMigratingDataReader(inner, LegacyHelper.GetMigrations(), LegacyHelper.GetLegacyEnumTypeNames(), LegacyHelper.GetLegacyTypeAliases());
			GameInstance gameInstance = SerializationUtility.DeserializeValue<GameInstance>(reader);
			if (gameInstance != null)
			{
				return gameInstance;
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
		return DeserializeBinaryDataLegacy(path);
	}

	public static async Task<GameInstance> DeserializeBinaryDataAsync(string path, IProgress<(float, string)> progress = null)
	{
		try
		{
			progress?.Report((0.1f, "Reading file..."));
			await using FileStream fs = File.OpenRead(path);
			progress?.Report((0.2f, "Decompressing..."));
			GameInstance result;
			await using (GZipStream gs = new GZipStream(fs, CompressionMode.Decompress))
			{
				progress?.Report((0.4f, "Copying to memory..."));
				GameInstance gameInstance3;
				await using (MemoryStream resultStream = new MemoryStream())
				{
					await gs.CopyToAsync(resultStream);
					progress?.Report((0.7f, "Deserializing (" + resultStream.Length.FormatBytes() + ") ..."));
					byte[] buffer = resultStream.ToArray();
					GameInstance gameInstance2;
					await using (MemoryStream ms = new MemoryStream(buffer))
					{
						using BinaryDataReader baseReader = new BinaryDataReader(ms, GetDeserializationContext());
						IntToStringMigratingDataReader reader = new IntToStringMigratingDataReader(baseReader, LegacyHelper.GetMigrations(), LegacyHelper.GetLegacyEnumTypeNames(), LegacyHelper.GetLegacyTypeAliases());
						try
						{
							GameInstance gameInstance = await Task.Run(() => SerializationUtility.DeserializeValue<GameInstance>(reader));
							progress?.Report((0.9f, "Validating..."));
							if (gameInstance != null)
							{
								progress?.Report((1f, "Done"));
								gameInstance2 = gameInstance;
								goto IL_03ef;
							}
						}
						finally
						{
							if (reader != null)
							{
								((IDisposable)reader).Dispose();
							}
						}
					}
					goto end_IL_012d;
					IL_03ef:
					gameInstance3 = gameInstance2;
					goto IL_04c1;
					end_IL_012d:;
				}
				goto end_IL_00e1;
				IL_04c1:
				result = gameInstance3;
				goto IL_0593;
				end_IL_00e1:;
			}
			goto end_IL_0095;
			IL_0593:
			return result;
			end_IL_0095:;
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
		return await Task.Run(() => DeserializeBinaryDataLegacy(path));
	}

	public static GameInstance DeserializeBinaryDataLegacy(string path)
	{
		using FileStream stream = File.OpenRead(path);
		using GZipStream serializationStream = new GZipStream(stream, CompressionMode.Decompress);
		return (GameInstance)new BinaryFormatter().Deserialize(serializationStream);
	}

	private static DeserializationContext GetDeserializationContext()
	{
		return new DeserializationContext
		{
			Config = 
			{
				AllowDeserializeInvalidData = true,
				DebugContext = 
				{
					Logger = DefaultLoggers.UnityLogger,
					LoggingPolicy = LoggingPolicy.LogWarningsAndErrors
				}
			}
		};
	}
}
