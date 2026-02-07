namespace Propentro.Infrastructure.Authorization;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class RequirePermissionAttribute(PermissionLogic logic, params string[] permissions) : Attribute
{
    public string[] Permissions { get; } = permissions;
    public PermissionLogic Logic { get; } = logic;

    public RequirePermissionAttribute(string permission) : this(PermissionLogic.All, [permission])
    {
    }
}
