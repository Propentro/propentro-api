namespace Propentro.Infrastructure.Authorization;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequireSystemAdministratorAttribute : Attribute;
