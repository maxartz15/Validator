namespace Validator.Editor
{
    public interface IValidator
    {
        public string MenuName { get; }
        public Report Validate();
    }
}