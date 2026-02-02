namespace Propentro.Infrastructure.Authorization;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : Attribute
{
    public string[] Permissions { get; }
    public PermissionLogic Logic { get; }

    public RequirePermissionAttribute(string permission)
    {
        Permissions = [permission];
        Logic = PermissionLogic.All;
    }

    public RequirePermissionAttribute(PermissionLogic logic, params string[] permissions)
    {
        Permissions = permissions;
        Logic = logic;
    }
}
