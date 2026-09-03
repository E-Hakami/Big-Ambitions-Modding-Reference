using System;
using System.Collections.Generic;
using UnityEngine;

namespace Player.HUD.ControlHints;

[CreateAssetMenu(fileName = "ControlsHintRegistry", menuName = "BigAmbitions/Controls Hints/Registry")]
public class ControlsHintRegistry : ScriptableObject
{
	[NonSerialized]
	private readonly List<IControlsHintProvider> _providers = new List<IControlsHintProvider>();

	public IReadOnlyList<IControlsHintProvider> Providers => _providers;

	public event Action<IControlsHintProvider> ProviderRegistered;

	public event Action<IControlsHintProvider> ProviderDeregistered;

	public void Reset()
	{
		_providers?.Clear();
		ProviderRegistered = null;
		ProviderDeregistered = null;
	}

	public void Register(IControlsHintProvider provider)
	{
		if (provider == null)
		{
			throw new ArgumentNullException("provider");
		}
		if (string.IsNullOrWhiteSpace(provider.HeaderKey))
		{
			throw new ArgumentException("The provider must have a header localization key.", "provider");
		}
		if (provider.Hints == null)
		{
			throw new ArgumentException("The provider must have a hints list.", "provider");
		}
		for (int i = 0; i < provider.Hints.Count; i++)
		{
			if (provider.Hints[i] == null)
			{
				throw new ArgumentException("The provider hints cannot contain null.", "provider");
			}
		}
		foreach (IControlsHintProvider provider2 in _providers)
		{
			if (provider2 == provider)
			{
				throw new InvalidOperationException("The provider is already registered.");
			}
			if (string.Equals(provider2.HeaderKey, provider.HeaderKey, StringComparison.Ordinal))
			{
				throw new InvalidOperationException("A provider with the header key '" + provider.HeaderKey + "' is already registered.");
			}
		}
		_providers.Add(provider);
		ProviderRegistered?.Invoke(provider);
	}

	public void Deregister(IControlsHintProvider provider)
	{
		if (provider != null && _providers.Remove(provider))
		{
			ProviderDeregistered?.Invoke(provider);
		}
	}
}
