using System;
using System.Collections.Generic;
using System.Linq;
using AI.Citizens;
using AI.Employees.SalaryNegotiation;
using Buildings.BuildingTypes.Special.FoodDelivery;
using Buildings.Office.Headquarters;
using Buildings.Retail.Businesses.CinemaTheater;
using Data.VehicleColors;
using Entities;
using Enums;
using GleyTrafficSystem;
using HGAttributes;
using Player.FoodDeliveryJob;
using Player.Sound.Radio;
using TMPro;
using UI.Smartphone.Apps.BizMan;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

public class GlobalReferences : InstanceBehavior<GlobalReferences>
{
	public Colors colors;

	public Color32[] chartColors;

	public Sprite[] gradients;

	public Sprite[] roundedGradients;

	public Sprite[] bigRoundedGradients;

	public VehicleColor[] vehicleColors;

	public BoatColor[] boatColors;

	public TMP_FontAsset[] signFontAssets;

	public Sprite[] contactIcons;

	public Sprite[] wsButtons;

	private readonly Dictionary<FontFace, TMP_FontAsset> _signFontAssetsDict = new Dictionary<FontFace, TMP_FontAsset>();

	public GameObject autoParkSpot;

	public RadioStationClips[] radioStationClips;

	public AudioClip[] radioJingles;

	public PhysicMaterial vehicleSafePhysicsMaterial;

	public DifficultySetting[] difficultySettings;

	public Vector3 vehicleRespawnSafetySpot;

	public InputActionAsset PlayerInput;

	public InputActionAsset VehicleInput;

	public VehiclePool vehiclePool;

	[SerializeField]
	private VideoClipData[] _videoClipDatas;

	public readonly Dictionary<VideoClipData.VideoType, List<VideoClipData>> VideoClips = new Dictionary<VideoClipData.VideoType, List<VideoClipData>>();

	public CitizenDataExamples dataExamples;

	public Color32[] pedestrianUmbrellasColors;

	public AudioMixerGroup loudspeakerMixerGroup;

	public AudioMixerGroup playerVehicleMixerGroup;

	public AudioMixerGroup factoryMachinesMixerGroup;

	public AudioMixerGroup outsideBuildingMusicMixerGroup;

	public AudioMixerGroup indoorMixerGroup;

	public AudioMixerGroup foleyMixerGroup;

	public AudioMixerGroup cityMixerGroup;

	public AudioMixer vehicleAudioMixer;

	public CandidateSalaryNegotiationParams candidateSalaryNegotiationParams;

	public HealthInsuranceNegotiationParams healthInsuranceNegotiationParams;

	public PricingManagerSettings pricingManagerSettings;

	public CinemaTheaterParams cinemaTheaterParams;

	public FoodDeliverySettings foodDeliverySettings;

	public AutoParkSettings autoParkSettings;

	public FoodDeliveryJobConfig foodDeliveryJobConfig;

	public ActorEmployeeAnimationSet actorEmployeeAnimationSet;

	public Material shadowOnlyMaterial;

	public BusinessNameGenerator businessNameGenerator;

	public GameObject videoGameVolumePrefab;

	[Header("Character Locomotion")]
	public float walkingSpeedZombie = 1.5f;

	public float walkingSpeedWalk = 1.2f;

	public float walkingSpeedWalkFast = 1.8f;

	public float walkingSpeedJog = 4f;

	public float walkingSpeedRun = 6f;

	public float walkingSpeedScooter = 12f;

	public float animationSpeedWalk = 0.75f;

	public float animationSpeedWalkFast = 1f;

	public float animationSpeedJog = 1.3f;

	public float animationSpeedRun = 0.85f;

	[Header("Other")]
	public Sprite vehiclePOIIcon;

	public Color vehiclePOIBackgroundColor;

	public Color vehicleIllegalParkingPOIBackgroundColor;

	private float[] _vehicleColorsRandomWeights;

	[AutocompleteProvider("VehicleColors")]
	private static List<string> VehicleColors => InstanceBehavior<GlobalReferences>.Instance.vehicleColors.Select((VehicleColor x) => x.name).ToList();

	protected override void Awake()
	{
		base.Awake();
		if (!base.IsMainInstance)
		{
			return;
		}
		InteriorElement.SetShadowOnlyMaterial(shadowOnlyMaterial);
		VideoClipData[] videoClipDatas = _videoClipDatas;
		foreach (VideoClipData videoClipData in videoClipDatas)
		{
			if (!VideoClips.TryGetValue(videoClipData.type, out var value))
			{
				value = new List<VideoClipData>();
				VideoClips.Add(videoClipData.type, value);
			}
			value.Add(videoClipData);
		}
		RebindSaveLoad.LoadBindingOverrides(PlayerInput);
		RebindSaveLoad.LoadBindingOverrides(VehicleInput);
		_signFontAssetsDict.Clear();
		TMP_FontAsset[] array = signFontAssets;
		foreach (TMP_FontAsset tMP_FontAsset in array)
		{
			if (Enum.TryParse<FontFace>(tMP_FontAsset.name, out var result))
			{
				_signFontAssetsDict.TryAdd(result, tMP_FontAsset);
			}
		}
	}

	public TMP_FontAsset GetFontByName(FontFace fontName)
	{
		_signFontAssetsDict.TryGetValue(fontName, out var value);
		return value;
	}

	public static Sprite GetButtonImageByName(string imageName)
	{
		Sprite sprite = InstanceBehavior<GlobalReferences>.Instance.wsButtons.FirstOrDefault((Sprite x) => x.name.ToLower().Contains(imageName.ToLower()));
		if ((object)sprite == null)
		{
			sprite = InstanceBehavior<GlobalReferences>.Instance.wsButtons[0];
		}
		return sprite;
	}

	public float[] VehicleColorsRandomWeights()
	{
		if (_vehicleColorsRandomWeights == null || _vehicleColorsRandomWeights.Length != vehicleColors.Length)
		{
			_vehicleColorsRandomWeights = vehicleColors.Select((VehicleColor x) => x.randomWeight).ToArray();
		}
		return _vehicleColorsRandomWeights;
	}
}
