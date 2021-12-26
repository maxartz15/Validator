namespace Validator
{
	public interface IValidatable
	{
#if UNITY_EDITOR
		public void Validate(Report report);
#endif
	}
}