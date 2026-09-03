using System;
using System.Collections.Generic;

namespace Player.HUD.ControlHints;

public class ControlsHintController : IDisposable
{
	private readonly ControlsHintRegistry _registry;

	private readonly List<IControlsHintProvider> _registeredProviders = new List<IControlsHintProvider>();

	private readonly List<IControlsHintProvider> _activeProviders = new List<IControlsHintProvider>();

	private bool _disposed;

	public IReadOnlyList<IControlsHintProvider> ActiveProviders => _activeProviders;

	public event Action Changed;

	public ControlsHintController(ControlsHintRegistry registry)
	{
		_registry = registry;
		_registry.ProviderRegistered += OnProviderRegistered;
		_registry.ProviderDeregistered += OnProviderDeregistered;
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, new Action<bool>(OnCityMapToggle));
		foreach (IControlsHintProvider provider in _registry.Providers)
		{
			AddProvider(provider);
		}
		RefreshActiveGroups();
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		_registry.ProviderRegistered -= OnProviderRegistered;
		_registry.ProviderDeregistered -= OnProviderDeregistered;
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Remove(GlobalEvents.onCityMapToggle, new Action<bool>(OnCityMapToggle));
		foreach (IControlsHintProvider registeredProvider in _registeredProviders)
		{
			Unsubscribe(registeredProvider);
		}
		_registeredProviders.Clear();
		_activeProviders.Clear();
		Changed = null;
	}

	private void OnProviderRegistered(IControlsHintProvider provider)
	{
		AddProvider(provider);
		RefreshActiveGroups();
	}

	private void OnProviderDeregistered(IControlsHintProvider provider)
	{
		for (int i = 0; i < _registeredProviders.Count; i++)
		{
			if (_registeredProviders[i] == provider)
			{
				Unsubscribe(provider);
				_registeredProviders.RemoveAt(i);
				break;
			}
		}
		RefreshActiveGroups();
	}

	private void OnCityMapToggle(bool _)
	{
		RefreshActiveGroups();
	}

	private void AddProvider(IControlsHintProvider provider)
	{
		Subscribe(provider);
		_registeredProviders.Add(provider);
		_registeredProviders.Sort(CompareProviders);
		if (_activeProviders.Capacity < _registeredProviders.Count)
		{
			_activeProviders.Capacity = _registeredProviders.Count;
		}
	}

	private void Subscribe(IControlsHintProvider provider)
	{
		provider.Changed += RefreshActiveGroups;
	}

	private void Unsubscribe(IControlsHintProvider provider)
	{
		provider.Changed -= RefreshActiveGroups;
	}

	private void RefreshActiveGroups()
	{
		_activeProviders.Clear();
		if (CityMap.IsOpen)
		{
			Changed?.Invoke();
			return;
		}
		foreach (IControlsHintProvider registeredProvider in _registeredProviders)
		{
			if (registeredProvider.IsActive && registeredProvider.Hints.Count != 0)
			{
				_activeProviders.Add(registeredProvider);
			}
		}
		Changed?.Invoke();
	}

	private static int CompareProviders(IControlsHintProvider left, IControlsHintProvider right)
	{
		int num = right.Priority.CompareTo(left.Priority);
		if (num == 0)
		{
			return string.Compare(left.HeaderKey, right.HeaderKey, StringComparison.Ordinal);
		}
		return num;
	}
}
