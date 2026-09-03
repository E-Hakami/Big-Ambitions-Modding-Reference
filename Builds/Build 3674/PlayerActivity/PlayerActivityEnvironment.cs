using System;
using NaughtyAttributes;
using UnityEngine;

namespace PlayerActivity;

[Serializable]
public abstract class PlayerActivityEnvironment<TConfig> where TConfig : PlayerActivityEnvironmentConfig
{
	[SerializeField]
	[Required(null)]
	private TConfig config;

	protected TConfig Config
	{
		get
		{
			if (!config)
			{
				throw new MissingReferenceException(GetType().Name + " config is not assigned.");
			}
			return config;
		}
	}
}
