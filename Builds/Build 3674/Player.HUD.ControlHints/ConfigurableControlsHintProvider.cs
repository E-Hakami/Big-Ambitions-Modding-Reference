using System;
using System.Collections.Generic;
using UnityEngine;

namespace Player.HUD.ControlHints;

public class ConfigurableControlsHintProvider : MonoBehaviour, IControlsHintProvider
{
	[SerializeField]
	private ControlsHintRegistry registry;

	[SerializeField]
	private string headerKey;

	[SerializeField]
	private int priority = 1;

	[SerializeField]
	private bool activateOnEnable;

	[SerializeField]
	private List<ControlsHintConfiguration> hints = new List<ControlsHintConfiguration>();

	private ControlsHint[] _runtimeHints;

	private readonly List<ControlsHint> _activeHints = new List<ControlsHint>();

	public string HeaderKey => headerKey;

	public int Priority => priority;

	public bool IsActive { get; private set; }

	public IReadOnlyList<ControlsHint> Hints => _activeHints;

	public event Action Changed;

	protected virtual void Awake()
	{
		RebuildActiveHints();
	}

	private void EnsureRuntimeHints()
	{
		if (_runtimeHints == null)
		{
			_runtimeHints = new ControlsHint[hints.Count];
			if (_activeHints.Capacity < hints.Count)
			{
				_activeHints.Capacity = hints.Count;
			}
			for (int i = 0; i < hints.Count; i++)
			{
				_runtimeHints[i] = hints[i].Initialize();
			}
		}
	}

	protected virtual void OnEnable()
	{
		registry.Register(this);
		if (activateOnEnable)
		{
			SetActive(active: true);
		}
	}

	protected virtual void OnDisable()
	{
		registry.Deregister(this);
		if (activateOnEnable)
		{
			SetActive(active: false);
		}
	}

	public void SetActive(bool active)
	{
		if (IsActive != active)
		{
			IsActive = active;
			Changed?.Invoke();
		}
	}

	public void SetHintsEnabled(IReadOnlyList<string> textKeys, bool enabledState)
	{
		bool flag = false;
		for (int i = 0; i < hints.Count; i++)
		{
			for (int j = 0; j < textKeys.Count; j++)
			{
				if (!(hints[i].TextKey != textKeys[j]))
				{
					flag |= hints[i].SetEnabled(enabledState);
					break;
				}
			}
		}
		if (flag)
		{
			RefreshHints();
		}
	}

	private void RefreshHints()
	{
		RebuildActiveHints();
		Changed?.Invoke();
	}

	private void RebuildActiveHints()
	{
		_activeHints.Clear();
		EnsureRuntimeHints();
		for (int i = 0; i < hints.Count; i++)
		{
			if (hints[i].IsEnabled)
			{
				_activeHints.Add(_runtimeHints[i]);
			}
		}
	}
}
