using System.Collections.Generic;
using BigAmbitions.Characters;
using BigAmbitions.Characters.Appearance;
using Extensions;
using Helpers;
using UnityEngine;

namespace Entities;

public class StreetPerformer : MonoBehaviour
{
	private static readonly AppearanceTag[] AppearanceTags = new AppearanceTag[1] { AppearanceTag.Casual };

	private static readonly Vector3 DonationBucketRelativePosition = new Vector3(-0.75f, 0f, 0f);

	[SerializeField]
	private BaseHuman baseHuman;

	[SerializeField]
	private AudioSource audioSource;

	private readonly Dictionary<StreetPerformerData, List<GameObject>> _objectsByPerformance = new Dictionary<StreetPerformerData, List<GameObject>>();

	private readonly Dictionary<StreetPerformerData, List<GameObject>> _spawnOnlyOneObjectsByPerformance = new Dictionary<StreetPerformerData, List<GameObject>>();

	private StreetPerformerData _data;

	private GameObject _donationBucket;

	public void SetStreetPerformerData(StreetPerformerData data)
	{
		_data = data;
	}

	public void InitAppearance()
	{
		baseHuman.appearanceSetter.SetRandomAge();
		baseHuman.appearanceSetter.SetRandomAppearance(AppearanceTags);
	}

	public void Enable()
	{
		ToggleStreetPerformerAnimation(toggle: true);
		EnableSfx();
		HandlePerformanceObjects();
		HandleDonationBucket();
	}

	private void ToggleStreetPerformerAnimation(bool toggle)
	{
		baseHuman.animator.SetBool(_data.animation, toggle);
	}

	private void EnableSfx()
	{
		audioSource.outputAudioMixerGroup = InstanceBehavior<GlobalReferences>.Instance.cityMixerGroup;
		AudioClip clip = _data.clip;
		if (baseHuman.appearanceSetter.data.gender == Gender.Female && _data.clipFemaleVariant != null)
		{
			clip = _data.clipFemaleVariant;
		}
		audioSource.clip = clip;
		if (audioSource.clip != null)
		{
			audioSource.Play();
		}
	}

	private void HandlePerformanceObjects()
	{
		bool flag = false;
		if (_objectsByPerformance.TryGetValue(_data, out var value))
		{
			flag = true;
			foreach (GameObject item in value)
			{
				item.SetActive(value: true);
			}
		}
		if (_spawnOnlyOneObjectsByPerformance.TryGetValue(_data, out var value2))
		{
			flag = true;
			value2.GetRandom().SetActive(value: true);
		}
		if (!flag)
		{
			SpawnPerformanceObjects();
		}
	}

	private void HandleDonationBucket()
	{
		if (_donationBucket == null)
		{
			_donationBucket = PrefabHelper.CreatePrefab("DonationBucket");
			_donationBucket.transform.SetParent(baseHuman.transform);
			_donationBucket.transform.localPosition = DonationBucketRelativePosition;
			_donationBucket.transform.localRotation = Quaternion.identity;
		}
		_donationBucket.SetActive(value: true);
	}

	private void SpawnPerformanceObjects()
	{
		PerformerObjectData[] objectsData = _data.objectsData;
		if (objectsData != null && objectsData.Length > 0)
		{
			_objectsByPerformance[_data] = new List<GameObject>();
			objectsData = _data.objectsData;
			foreach (PerformerObjectData performerObjectData in objectsData)
			{
				Transform transform = CreateAndPlaceObject(performerObjectData);
				_objectsByPerformance[_data].Add(transform.gameObject);
			}
		}
		objectsData = _data.spawnOnlyOneObjectsData;
		if (objectsData != null && objectsData.Length > 0)
		{
			_spawnOnlyOneObjectsByPerformance[_data] = new List<GameObject>();
			objectsData = _data.spawnOnlyOneObjectsData;
			foreach (PerformerObjectData performerObjectData2 in objectsData)
			{
				Transform transform2 = CreateAndPlaceObject(performerObjectData2);
				_spawnOnlyOneObjectsByPerformance[_data].Add(transform2.gameObject);
				transform2.gameObject.SetActive(value: false);
			}
			_spawnOnlyOneObjectsByPerformance[_data].GetRandom().gameObject.SetActive(value: true);
		}
		baseHuman.appearanceSetter.visualsUpdated?.Invoke();
	}

	private Transform CreateAndPlaceObject(PerformerObjectData performerObjectData)
	{
		Transform transform = PrefabHelper.CreatePrefab(performerObjectData.objectName).transform;
		if (performerObjectData.isHandObject)
		{
			Renderer component = transform.GetComponent<Renderer>();
			AttachedObjectData attachedObjectData = new AttachedObjectData
			{
				objectTransform = transform,
				renderer = component,
				position = performerObjectData.position,
				rotation = Quaternion.Euler(performerObjectData.rotation)
			};
			baseHuman.AttachObjectToHand(attachedObjectData, performerObjectData.isRightHand);
		}
		else if (performerObjectData.isUpperChestObject)
		{
			Renderer component2 = transform.GetComponent<Renderer>();
			AttachedObjectData attachedObjectData2 = new AttachedObjectData
			{
				objectTransform = transform,
				renderer = component2,
				position = performerObjectData.position,
				rotation = Quaternion.Euler(performerObjectData.rotation)
			};
			baseHuman.AttachObjectToUpperChest(attachedObjectData2);
		}
		else
		{
			transform.SetParent(baseHuman.transform);
			transform.localPosition = performerObjectData.position;
			transform.localRotation = Quaternion.Euler(performerObjectData.rotation);
		}
		return transform;
	}

	public void Disable()
	{
		ToggleStreetPerformerAnimation(toggle: false);
		audioSource.Stop();
		if (_objectsByPerformance.TryGetValue(_data, out var value))
		{
			foreach (GameObject item in value)
			{
				item.SetActive(value: false);
			}
		}
		if (_spawnOnlyOneObjectsByPerformance.TryGetValue(_data, out value))
		{
			foreach (GameObject item2 in value)
			{
				item2.SetActive(value: false);
			}
		}
		_donationBucket.SetActive(value: false);
	}
}
