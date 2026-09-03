namespace Entities;

public class NegotiationOffer
{
	public readonly string id;

	public int dayToSendOffer;

	public bool negotiationFinished;

	public bool accepted;

	public float initialOfferPrice;

	public float minOfferPrice;

	protected NegotiationOffer(string id, int dayToSendOffer)
	{
		this.id = id;
		this.dayToSendOffer = dayToSendOffer;
	}
}
