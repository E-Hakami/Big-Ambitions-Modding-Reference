using System;
using System.Collections.Generic;

namespace BigAmbitions.Rivals;

[Serializable]
public class RivalState
{
	public string rivalId;

	public List<Tuple<int, float>> weeklyIncomeHistory;

	public List<Tuple<int, int>> numberOfBusinessesHistory;
}
