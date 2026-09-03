using System;
using System.Collections.Generic;
using UI.DraggableWindows;

namespace Helpers;

[Serializable]
public class PlayerSettingsData
{
	public List<string> playerColorHexes = new List<string>();

	public List<DraggableWindowData> draggableWindows = new List<DraggableWindowData>();

	public List<string> idFurnitureFavorites = new List<string>();
}
