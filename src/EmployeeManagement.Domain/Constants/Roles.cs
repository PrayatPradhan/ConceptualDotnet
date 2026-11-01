namespace EmployeeManagement.Domain.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string HrManager = "HR_Manager";
    public const string Viewer = "Viewer";

    public static readonly IReadOnlyCollection<string> All = [Admin, HrManager, Viewer];
}
