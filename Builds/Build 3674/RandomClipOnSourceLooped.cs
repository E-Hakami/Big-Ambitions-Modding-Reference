using System.Collections.Generic;
using Culling;
using Extensions;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RandomClipOnSourceLooped : MonoBehaviour, ICullable
{
	[SerializeField]
	private float boundingSphereRadius = 4f;

	[SerializeField]
	[Range(1f, 5f)]
	private int sourcesAtOnce = 1;

	[SerializeField]
	private AudioClip[] clips;

	[SerializeField]
	private float minSecondsBetweenSounds;

	[SerializeField]
	private float maxSecondsBetweenSounds;

	private List<AudioSource> _sources;

	private AudioListener _listener;

	private bool _isVisible;

	private float _nextSoundTime;

	private void Awake()
	{
		_listener = Object.FindObjectOfType<AudioListener>();
		AudioSource orAddComponent = base.gameObject.GetOrAddComponent<AudioSource>();
		orAddComponent.loop = false;
		_sources = new List<AudioSource> { orAddComponent };
		for (int i = 1; i < sourcesAtOnce; i++)
		{
			AudioSource audioSource = base.gameObject.AddComponent<AudioSource>();
			audioSource.maxDistance = orAddComponent.maxDistance;
			audioSource.minDistance = orAddComponent.minDistance;
			audioSource.spatialBlend = orAddComponent.spatialBlend;
			audioSource.rolloffMode = orAddComponent.rolloffMode;
			audioSource.dopplerLevel = orAddComponent.dopplerLevel;
			audioSource.volume = orAddComponent.volume;
			audioSource.pitch = orAddComponent.pitch;
			audioSource.outputAudioMixerGroup = orAddComponent.outputAudioMixerGroup;
			audioSource.loop = orAddComponent.loop;
			_sources.Add(audioSource);
		}
	}

	private void Start()
	{
		if (boundingSphereRadius > 0f)
		{
			InstanceBehavior<CullingManager>.Instance.generalCullingGroupController.RegisterCullable(this);
		}
	}

	private void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded && boundingSphereRadius > 0f)
		{
			InstanceBehavior<CullingManager>.Instance?.generalCullingGroupController.UnregisterCullable(this);
		}
	}

	private void FixedUpdate()
	{
		if (boundingSphereRadius > 0f && (!_isVisible || Vector2.Distance(AudioListenerPositioner.GetAudioListenerPosition(), base.transform.position) > _sources[0].maxDistance))
		{
			return;
		}
		for (int i = 0; i < _sources.Count; i++)
		{
			AudioSource audioSource = _sources[i];
			if (audioSource.isPlaying)
			{
				continue;
			}
			if (maxSecondsBetweenSounds > 0f)
			{
				if (_nextSoundTime == 0f)
				{
					_nextSoundTime = Time.time + Random.Range(minSecondsBetweenSounds, maxSecondsBetweenSounds);
					continue;
				}
				if (_nextSoundTime > Time.time)
				{
					continue;
				}
				_nextSoundTime = 0f;
			}
			audioSource.clip = clips[Random.Range(0, clips.Length)];
			audioSource.Play();
		}
	}

	public void OnLod0()
	{
		_isVisible = true;
		for (int i = 0; i < _sources.Count; i++)
		{
			_sources[i].enabled = true;
		}
	}

	public void OnLod1()
	{
		_isVisible = false;
		for (int i = 0; i < _sources.Count; i++)
		{
			AudioSource audioSource = _sources[i];
			audioSource.Stop();
			audioSource.enabled = false;
		}
	}

	public void OnLod2()
	{
		OnLod1();
	}

	public BoundingSphere GetCullingBoundingSphere()
	{
		return new BoundingSphere(base.transform.position + Vector3.up * 2f, boundingSphereRadius);
	}
}
