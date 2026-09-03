using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

public class MainMenuMusic : MonoBehaviour
{
	[SerializeField]
	private AudioMixerSnapshot mainMenuSnapshot;

	public static MainMenuMusic instance;

	private AudioSource _audioSource;

	private void Awake()
	{
		_audioSource = GetComponent<AudioSource>();
		if ((bool)instance && instance != this)
		{
			Object.Destroy(base.gameObject);
		}
		else
		{
			instance = this;
			Object.DontDestroyOnLoad(instance.gameObject);
		}
		mainMenuSnapshot.TransitionTo(1f);
	}

	public static void Stop()
	{
		if ((bool)instance)
		{
			instance.StartCoroutine(instance.StopInternal());
		}
	}

	private IEnumerator StopInternal()
	{
		yield return _audioSource.DOFade(0f, 3f).SetLink(_audioSource.gameObject).SetUpdate(isIndependentUpdate: true)
			.WaitForCompletion();
		if ((bool)instance)
		{
			Object.Destroy(instance.gameObject);
		}
	}
}
