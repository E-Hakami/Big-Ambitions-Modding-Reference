using System;

namespace Entities;

[Serializable]
public enum MarketEventType
{
	BusinessOpened,
	BusinessClosed,
	Hype,
	ProductShortage,
	ProductBackorder,
	LargePlayerPurchase,
	MaxProvidersExceeded
}
