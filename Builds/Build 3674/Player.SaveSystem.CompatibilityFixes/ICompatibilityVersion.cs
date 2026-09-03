using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes;

public interface ICompatibilityVersion
{
	IEnumerable<(ICompatibilityFix fix, int buildNumber)> CompatibilityFixesBeforeLoading => null;

	IEnumerable<(ICompatibilityFix fix, int buildNumber)> CompatibilityFixesAfterLoading => null;

	void RunPriority(GameInstance gameInstance)
	{
		RunBeforeGameLoaded(gameInstance, priority: true);
	}

	void Run(GameInstance gameInstance)
	{
		RunBeforeGameLoaded(gameInstance, priority: false);
		GlobalEvents.RegisterOnGameLoadedCallback(delegate
		{
			RunAfterGameLoaded(gameInstance, priority: true);
			RunAfterGameLoaded(gameInstance, priority: false);
		});
	}

	private void RunBeforeGameLoaded(GameInstance gameInstance, bool priority)
	{
		if (CompatibilityFixesBeforeLoading == null)
		{
			return;
		}
		foreach (var item in priority ? CompatibilityFixesBeforeLoading.Where(((ICompatibilityFix fix, int buildNumber) fix, int _) => fix.fix.Priority) : CompatibilityFixesBeforeLoading.Where(((ICompatibilityFix fix, int buildNumber) fix, int _) => !fix.fix.Priority))
		{
			if (gameInstance.buildNumberAtLastSave <= item.buildNumber)
			{
				try
				{
					item.fix.Apply(gameInstance);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}
		}
	}

	private void RunAfterGameLoaded(GameInstance gameInstance, bool priority)
	{
		if (CompatibilityFixesAfterLoading == null)
		{
			return;
		}
		foreach (var item in priority ? CompatibilityFixesAfterLoading.Where(((ICompatibilityFix fix, int buildNumber) fix, int _) => fix.fix.Priority) : CompatibilityFixesAfterLoading.Where(((ICompatibilityFix fix, int buildNumber) fix, int _) => !fix.fix.Priority))
		{
			if (gameInstance.buildNumberAtLastSave <= item.buildNumber)
			{
				try
				{
					item.fix.Apply(gameInstance);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}
		}
	}
}
