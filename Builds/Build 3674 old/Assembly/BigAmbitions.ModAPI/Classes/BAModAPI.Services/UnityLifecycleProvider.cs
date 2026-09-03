// BigAmbitions.ModAPI, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BAModAPI.Services.UnityLifecycleProvider
using System;
using BAModAPI;
using BAModAPI.Services;
using UnityEngine;

public sealed class UnityLifecycleProvider : MonoBehaviour
{
	private const string ProviderName = "UnityLifecycleProvider";

	private static Action _onFixedUpdate;

	private static Action _onLateUpdate;

	private static Action _onUpdate;

	private static UnityLifecycleProvider Instance;

	private static bool IsApplicationQuitting;

	private static bool IsCreatingInstance;

	public static event Action OnUpdate
	{
		add
		{
			AddCallback(ref _onUpdate, value);
		}
		remove
		{
			RemoveCallback(ref _onUpdate, value);
		}
	}

	public static event Action OnFixedUpdate
	{
		add
		{
			AddCallback(ref _onFixedUpdate, value);
		}
		remove
		{
			RemoveCallback(ref _onFixedUpdate, value);
		}
	}

	public static event Action OnLateUpdate
	{
		add
		{
			AddCallback(ref _onLateUpdate, value);
		}
		remove
		{
			RemoveCallback(ref _onLateUpdate, value);
		}
	}

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		Instance = this;
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	private void OnApplicationQuit()
	{
		IsApplicationQuitting = true;
	}

	private void Update()
	{
		_onUpdate.InvokeSafely();
	}

	private void FixedUpdate()
	{
		_onFixedUpdate.InvokeSafely();
	}

	private void LateUpdate()
	{
		_onLateUpdate.InvokeSafely();
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Initialize()
	{
		EnsureInstance();
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		_onFixedUpdate = null;
		_onLateUpdate = null;
		_onUpdate = null;
		Instance = null;
		IsApplicationQuitting = false;
		IsCreatingInstance = false;
	}

	private static void AddCallback(ref Action callbacks, Action callback)
	{
		if (callback != null)
		{
			EnsureInstance();
			callbacks = (Action)Delegate.Combine(callbacks, callback);
		}
	}

	private static void RemoveCallback(ref Action callbacks, Action callback)
	{
		if (callback != null)
		{
			callbacks = (Action)Delegate.Remove(callbacks, callback);
		}
	}

	private static void EnsureInstance()
	{
		if (Instance != null || IsApplicationQuitting || IsCreatingInstance)
		{
			return;
		}
		IsCreatingInstance = true;
		try
		{
			GameObject obj = new GameObject("UnityLifecycleProvider");
			UnityEngine.Object.DontDestroyOnLoad(obj);
			obj.AddComponent<UnityLifecycleProvider>();
		}
		finally
		{
			IsCreatingInstance = false;
		}
	}
}
