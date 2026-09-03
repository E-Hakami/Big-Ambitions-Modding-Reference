using System;
using Streets;
using UI.Guiders;
using UnityEngine;

namespace Tutorial;

public class QuestEntryTarget : ScriptableObject
{
	public string localizeKey;

	[NonSerialized]
	public DirectionGuiderType guiderType;

	private bool _isInitialized;

	public void Init()
	{
		if (!_isInitialized)
		{
			_isInitialized = true;
			OnInit();
		}
	}

	protected virtual void OnInit()
	{
	}

	public void Dispose()
	{
		_isInitialized = false;
		OnDispose();
	}

	protected virtual void OnDispose()
	{
	}

	public virtual Address GetAddress()
	{
		return null;
	}

	public virtual void SetTarget()
	{
		Address address = GetAddress();
		if (address.IsUndefined())
		{
			GuidersManager.ResetGuider(guiderType);
		}
		else
		{
			GuidersManager.SetGuiderTarget(address, guiderType);
		}
	}
}
