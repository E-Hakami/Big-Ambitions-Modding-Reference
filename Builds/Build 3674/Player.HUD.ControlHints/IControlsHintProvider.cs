using System;
using System.Collections.Generic;

namespace Player.HUD.ControlHints;

public interface IControlsHintProvider
{
	string HeaderKey { get; }

	int Priority { get; }

	bool IsActive { get; }

	IReadOnlyList<ControlsHint> Hints { get; }

	event Action Changed;
}
