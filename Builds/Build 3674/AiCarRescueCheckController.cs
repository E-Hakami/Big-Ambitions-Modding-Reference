using System.Collections.Generic;
using UnityEngine;

public class AiCarRescueCheckController : InstanceBehavior<AiCarRescueCheckController>
{
	[SerializeField]
	private float updateTime = 0.1f;

	private readonly List<AiCarRescueCheck> _aiCarRescueChecks = new List<AiCarRescueCheck>();

	private float _deltaTimeAccumulated;

	public void Register(AiCarRescueCheck aiCarRescueCheck)
	{
		_aiCarRescueChecks.Add(aiCarRescueCheck);
	}

	public void UnRegister(AiCarRescueCheck aiCarRescueCheck)
	{
		_aiCarRescueChecks.Remove(aiCarRescueCheck);
	}

	private void Update()
	{
		if (_deltaTimeAccumulated >= updateTime)
		{
			for (int i = 0; i < _aiCarRescueChecks.Count; i++)
			{
				_aiCarRescueChecks[i].DoUpdate(_deltaTimeAccumulated);
			}
			_deltaTimeAccumulated = 0f;
		}
		else
		{
			_deltaTimeAccumulated += Time.deltaTime;
		}
	}
}
