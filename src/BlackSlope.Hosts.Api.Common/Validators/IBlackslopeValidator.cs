namespace BlackSlope.Hosts.Api.Common.Validators
{
    public interface IBlackslopeValidator<in T>
    {
        void Validate(T instance);
        void Validate(T instance, string[] ruleSetsToExecute);
    }
}
