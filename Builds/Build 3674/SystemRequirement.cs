using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using BigAmbitions.SaveSystem;
using Extensions;
using Helpers;
using Localizor;
using Scenes.MainMenu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SystemRequirement : MonoBehaviour
{
	public class SystemRequirementData
	{
		public SystemRequirements Type;

		public Status State;

		public string ShouldValue;

		public string CurrentValue;
	}

	public enum SystemRequirements
	{
		CPUSpeed,
		Ram,
		Vram,
		DedicatedGPU,
		ShaderLevel,
		ComputeShader,
		FolderAccess
	}

	public enum Status
	{
		Ok,
		Warning,
		Error
	}

	public GameObject window;

	public Transform closeButton;

	public Transform template;

	public Sprite errorSprite;

	public Sprite warningSprite;

	public Sprite okSprite;

	public static string folderChecking;

	public void Start()
	{
		if (PlayerPrefSettings.shownSystemRequirementWarning && GameVersion.GetCurrent().GetFullVersionString() != PlayerPrefSettings.LastPlayedVersion)
		{
			PlayerPrefSettings.shownSystemRequirementWarning = false;
		}
		List<SystemRequirementData> requirements = GetRequirements();
		if (requirements.All((SystemRequirementData x) => x.Type != SystemRequirements.FolderAccess) && PlayerPrefSettings.shownSystemRequirementWarning)
		{
			Close();
			return;
		}
		template.ResetTemplate();
		foreach (SystemRequirementData item in requirements)
		{
			Transform obj = UnityEngine.Object.Instantiate(template, base.gameObject.transform);
			obj.gameObject.SetActive(value: true);
			obj.GetLanguageChangeEventByName("Type").Key = item.Type.GetLocalizeKey();
			obj.GetLabelByName("ShouldValue").text = item.ShouldValue;
			TextMeshProUGUI labelByName = obj.GetLabelByName("CurrentValue");
			labelByName.text = item.CurrentValue;
			Image imageByName = obj.GetImageByName("Status");
			switch (item.State)
			{
			case Status.Ok:
				imageByName.sprite = okSprite;
				labelByName.color = InstanceBehavior<GlobalReferences>.Instance.colors.white;
				break;
			case Status.Warning:
				imageByName.sprite = warningSprite;
				labelByName.color = InstanceBehavior<GlobalReferences>.Instance.colors.yellow;
				break;
			case Status.Error:
				imageByName.sprite = errorSprite;
				labelByName.color = InstanceBehavior<GlobalReferences>.Instance.colors.red;
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}
		closeButton.SetAsLastSibling();
		Options.SetQualityLevelToLow();
	}

	public void Close()
	{
		window.SetActive(value: false);
		PlayerPrefSettings.shownSystemRequirementWarning = true;
		InstanceBehavior<MainMenuController>.Instance.NextMainMenuAction();
	}

	public static List<SystemRequirementData> GetRequirements()
	{
		bool flag = !SystemInfo.deviceModel.StartsWith("Intel") || !SystemInfo.deviceModel.EndsWith("Graphics");
		"systemrequirements_gigahertz".GetLocalization();
		string localization = "systemrequirements_gigabyte".GetLocalization();
		Path.GetDirectoryName(Application.dataPath);
		bool flag2 = CheckIfHasAccessToAssetsFolder();
		List<SystemRequirementData> list = new List<SystemRequirementData>
		{
			new SystemRequirementData
			{
				Type = SystemRequirements.Vram,
				State = ((SystemInfo.graphicsMemorySize < 2000) ? Status.Warning : Status.Ok),
				CurrentValue = ((float)SystemInfo.graphicsMemorySize / 1000f).ToString("F0", CultureInfo.InvariantCulture) + localization,
				ShouldValue = "2" + localization
			},
			new SystemRequirementData
			{
				Type = SystemRequirements.Ram,
				State = ((SystemInfo.systemMemorySize < 8000) ? Status.Warning : Status.Ok),
				CurrentValue = ((float)SystemInfo.systemMemorySize / 1000f).ToString("F0", CultureInfo.InvariantCulture) + localization,
				ShouldValue = "8" + localization
			},
			new SystemRequirementData
			{
				Type = SystemRequirements.DedicatedGPU,
				State = ((!flag) ? Status.Warning : Status.Ok),
				CurrentValue = (flag ? "systemrequirements_dedicated" : "systemrequirements_integrated").GetLocalization(),
				ShouldValue = "systemrequirements_dedicated".GetLocalization()
			},
			new SystemRequirementData
			{
				Type = SystemRequirements.ComputeShader,
				State = ((!SystemInfo.supportsComputeShaders) ? Status.Error : Status.Ok),
				CurrentValue = (SystemInfo.supportsComputeShaders ? "systemrequirements_supported" : "systemrequirements_unsupported").GetLocalization(),
				ShouldValue = "systemrequirements_supported".GetLocalization()
			},
			new SystemRequirementData
			{
				Type = SystemRequirements.ShaderLevel,
				State = ((SystemInfo.graphicsShaderLevel < 50) ? Status.Error : Status.Ok),
				CurrentValue = ((float)SystemInfo.graphicsShaderLevel / 10f).ToString("F1", CultureInfo.InvariantCulture),
				ShouldValue = "5.0"
			}
		};
		_ = new SteamAPI.SteamAPIDll[3]
		{
			new SteamAPI.SteamAPIDll
			{
				filePath = Path.Combine(Application.dataPath, "Plugins", "x86_64", "steam_api64.dll"),
				target = RuntimePlatform.WindowsPlayer,
				hash = "500475b20083ccdc64f12d238cab687a"
			},
			new SteamAPI.SteamAPIDll
			{
				filePath = Path.Combine(Application.dataPath, "Plugins", "libsteam_api.so"),
				target = RuntimePlatform.LinuxPlayer,
				hash = "ccdf20f0b2f9abbe1fea8314b9fab096"
			},
			new SteamAPI.SteamAPIDll
			{
				filePath = Path.Combine(Application.dataPath, "PlugIns", "libsteam_api.bundle"),
				target = RuntimePlatform.OSXPlayer,
				hash = "c0ed1c993cc14528e27aceecc07a2da8"
			}
		};
		if (!flag2)
		{
			list.Add(new SystemRequirementData
			{
				Type = SystemRequirements.FolderAccess,
				State = Status.Error,
				CurrentValue = "systemrequirements_unsupported".GetLocalization(),
				ShouldValue = "systemrequirements_supported".GetLocalization()
			});
		}
		return list;
	}

	private static bool CheckIfHasAccessToAssetsFolder()
	{
		try
		{
			folderChecking = LogoHelper.GetBuildInIconsFolder();
			using (new FileInfo(Path.Combine(folderChecking, "appliance1.png")).OpenRead())
			{
				folderChecking = Path.Combine(Application.streamingAssetsPath, "BusinessLayouts");
				using (new FileInfo(Path.Combine(folderChecking, "ba:businesstype_appliancestore".GetIdWithoutType(), "C1", "HellsKitchenApplianceStore.json")).OpenRead())
				{
					folderChecking = Path.Combine(Application.streamingAssetsPath, "locale");
					using (new FileInfo(Path.Combine(folderChecking, "en.json")).OpenRead())
					{
						return true;
					}
				}
			}
		}
		catch (Exception arg)
		{
			Debug.Log($"No access to the folder '{folderChecking}':\n{arg}");
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string CalculateMD5Hash(string path)
	{
		MD5 mD = MD5.Create();
		StringBuilder stringBuilder = new StringBuilder();
		byte[] array = mD.ComputeHash(File.ReadAllBytes(path));
		foreach (byte b in array)
		{
			stringBuilder.Append(b.ToString("x2"));
		}
		return stringBuilder.ToString();
	}
}
