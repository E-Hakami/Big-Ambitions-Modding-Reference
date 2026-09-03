using System.Collections;
using Controllers;
using Extensions;
using UI.Load;
using UnityEngine;
using UnityEngine.Networking;

public class RadioStationData
{
	private const int LocalSongsToKeepCached = 5;

	private const float TimeBetweenJinglesInSeconds = 480f;

	private const float NearEndOffsetInSeconds = 5f;

	private float _startedAt;

	private float _lastJingleAt;

	private bool _hasNoPlayableClips;

	private readonly RadioStation _radioStation;

	public RadioClip[] radioClips;

	public int currentClipIndex;

	public int currentJingleIndex = -1;

	public bool IsLoading
	{
		get
		{
			if (radioClips.Length != 0)
			{
				return radioClips[currentClipIndex].clip == null;
			}
			return false;
		}
	}

	public bool HasPlayableClips
	{
		get
		{
			if (radioClips.Length != 0)
			{
				return !_hasNoPlayableClips;
			}
			return false;
		}
	}

	public float CurrentClipProgressedTime => BroadcastTime - _startedAt;

	private static float BroadcastTime => InstanceBehavior<GameManager>.Instance.radioPlayer.BroadcastTime;

	public RadioStationData(RadioStation radioStation, RadioClip[] radioClips)
	{
		_radioStation = radioStation;
		this.radioClips = radioClips;
		StartCurrentClipAt(0f);
		if (radioStation == RadioStation.LocalFiles)
		{
			CacheUpcomingLocalSongs();
		}
	}

	public void UpdateCurrentClip()
	{
		if (radioClips.Length == 0 || _hasNoPlayableClips)
		{
			return;
		}
		if (currentJingleIndex == -1 && radioClips[currentClipIndex].clip == null)
		{
			StartCurrentClipAt(0f);
			if (radioClips[currentClipIndex].HasLoadFailed)
			{
				AdvanceToNextClip();
			}
		}
		else if (!(CurrentClipProgressedTime < GetCurrentClipLength()))
		{
			currentJingleIndex = -1;
			StartCurrentClipAt(0f);
			if (ShouldRunAJingle())
			{
				currentJingleIndex = Random.Range(0, InstanceBehavior<GlobalReferences>.Instance.radioJingles.Length);
				_lastJingleAt = BroadcastTime;
				InstanceBehavior<GameManager>.Instance.radioPlayer.onNextSong.Invoke(_radioStation);
			}
			else
			{
				AdvanceToNextClip();
			}
		}
	}

	public void StartCurrentClipAt(float progressedTime)
	{
		_startedAt = BroadcastTime - progressedTime;
	}

	public void AdvanceToNextClip()
	{
		AdvanceClipIndex();
		InstanceBehavior<GameManager>.Instance.radioPlayer.onNextSong.Invoke(_radioStation);
	}

	public void AdvanceToRandomPositionInNextClip()
	{
		currentJingleIndex = -1;
		AdvanceClipIndex();
		StartCurrentClipAt(Random.Range(0f, radioClips[currentClipIndex].GetLength));
		InstanceBehavior<GameManager>.Instance.radioPlayer.onNextSong.Invoke(_radioStation);
	}

	private void AdvanceClipIndex()
	{
		int num = currentClipIndex;
		for (int i = 1; i <= radioClips.Length; i++)
		{
			int num2 = (num + i) % radioClips.Length;
			if (!radioClips[num2].HasLoadFailed)
			{
				currentClipIndex = num2;
				if (_radioStation == RadioStation.LocalFiles)
				{
					UpdateLocalSongsCache(num);
				}
				return;
			}
		}
		_hasNoPlayableClips = true;
	}

	public void UpdateLocalSongsCache(int previousClipIndex)
	{
		RemoveLocalSongFromCache(previousClipIndex);
		CacheUpcomingLocalSongs();
	}

	private void CacheUpcomingLocalSongs()
	{
		int num = Mathf.Min(5, radioClips.Length);
		for (int i = 0; i < num; i++)
		{
			int num2 = (currentClipIndex + i) % radioClips.Length;
			RadioClip radioClip = radioClips[num2];
			if (!(radioClip.clip != null) && !radioClip.HasLoadFailed && !radioClip.IsCaching)
			{
				InstanceBehavior<GameManager>.Instance.StartCoroutine(AddLocalSongToCache(num2));
			}
		}
	}

	private IEnumerator AddLocalSongToCache(int localSongIndexToAdd)
	{
		RadioClip radioClip = radioClips[localSongIndexToAdd];
		radioClip.IsCaching = true;
		using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(radioClip.path, radioClip.type);
		((DownloadHandlerAudioClip)www.downloadHandler).compressed = true;
		yield return www.SendWebRequest();
		radioClip.IsCaching = false;
		if (www.result != UnityWebRequest.Result.Success)
		{
			radioClip.HasLoadFailed = true;
			Debug.LogError("Couldn't read the local song " + radioClip.Name + ". Error: " + www.error);
			yield break;
		}
		AudioClip content = DownloadHandlerAudioClip.GetContent(www);
		if (content == null || content.samples <= 0)
		{
			radioClip.HasLoadFailed = true;
			AudioFileFormatHelper.MarkUnsupported(radioClip.path);
			Debug.LogError("Couldn't decode the local song " + radioClip.Name + ". It is not a supported audio format.");
			if (content != null)
			{
				Object.Destroy(content);
			}
			yield break;
		}
		AudioFileFormatHelper.MarkSupported(radioClip.path);
		RadioPlayer radioPlayer = InstanceBehavior<GameManager>.Instance.radioPlayer;
		if (radioPlayer.GetRadioStationData(_radioStation) != this)
		{
			Object.Destroy(content);
			yield break;
		}
		content.name = radioClip.Name;
		radioClip.clip = content;
		if (localSongIndexToAdd == currentClipIndex && !LoadScene.isLoading)
		{
			radioPlayer.onSongsLoaded.Invoke();
		}
	}

	private void RemoveLocalSongFromCache(int localSongIndexToRemove)
	{
		if (5 < radioClips.Length)
		{
			if (localSongIndexToRemove < 0)
			{
				localSongIndexToRemove += radioClips.Length;
			}
			if (radioClips[localSongIndexToRemove].clip != null)
			{
				Object.Destroy(radioClips[localSongIndexToRemove].clip);
			}
		}
	}

	public AudioClip GetCurrentSong()
	{
		if (currentJingleIndex != -1)
		{
			return InstanceBehavior<GlobalReferences>.Instance.radioJingles[currentJingleIndex];
		}
		if (radioClips.Length == 0)
		{
			return null;
		}
		return radioClips[currentClipIndex].clip;
	}

	private bool ShouldRunAJingle()
	{
		if (_radioStation == RadioStation.LocalFiles)
		{
			return false;
		}
		if (BroadcastTime - _lastJingleAt < 480f)
		{
			return false;
		}
		if (InstanceBehavior<GameManager>.Instance.radioPlayer.SkipJingles)
		{
			return false;
		}
		if (InstanceBehavior<GameManager>.Instance.playerController.Character.CurrentEntityController is DJBoothController)
		{
			return false;
		}
		if (BuildingManager.IsInsideBuilding && InstanceBehavior<BuildingManager>.Instance.buildingRegistration.businessTypeName == "ba:businesstype_nightclub")
		{
			return false;
		}
		return true;
	}

	public void ForceNextAudioToBeAJingle()
	{
		_lastJingleAt = BroadcastTime - 480f;
	}

	public void SkipCurrentRadioClip()
	{
		if (radioClips.Length != 0 && !(radioClips[currentClipIndex].clip == null))
		{
			StartCurrentClipAt(GetCurrentClipLength());
		}
	}

	public void SkipToNearEndOfCurrentRadioClip()
	{
		if (radioClips.Length != 0 && !(radioClips[currentClipIndex].clip == null))
		{
			StartCurrentClipAt(Mathf.Max(0f, GetCurrentClipLength() - 5f));
			InstanceBehavior<GameManager>.Instance.radioPlayer.onNextSong.Invoke(_radioStation);
		}
	}

	private float GetCurrentClipLength()
	{
		if (currentJingleIndex != -1)
		{
			return InstanceBehavior<GlobalReferences>.Instance.radioJingles[currentJingleIndex].length;
		}
		return radioClips[currentClipIndex].clip.length;
	}
}
