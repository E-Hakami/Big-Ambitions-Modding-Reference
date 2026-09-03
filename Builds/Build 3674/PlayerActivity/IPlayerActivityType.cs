namespace PlayerActivity;

public interface IPlayerActivityType
{
	IPlayerActivity CreateActivity(EntityController attachedEntity);
}
