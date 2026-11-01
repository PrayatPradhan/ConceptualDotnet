namespace EmployeeManagement.Application.Common;

public static class Policies
{
    public const string CanReadEmployees = "CanReadEmployees";
    public const string CanManageEmployees = "CanManageEmployees";
    public const string AdminOnly = "AdminOnly";
}
