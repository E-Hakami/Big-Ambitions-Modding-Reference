using System;

namespace Entities;

[Serializable]
public class MarketNews
{
	public MarketEventType newsType;

	public int day;

	public Address address;

	public string businessName;

	public string businessTypeName;

	public string corporationName;

	public MarketNews(MarketEventType newsType, int day, Address address, string businessName, string businessTypeName, string corporationName)
	{
		this.newsType = newsType;
		this.day = day;
		this.address = address;
		this.businessName = businessName;
		this.businessTypeName = businessTypeName;
		this.corporationName = corporationName;
	}
}
