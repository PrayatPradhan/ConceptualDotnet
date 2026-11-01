namespace EmployeeManagement.Domain.Entities;

public class Employee
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string Department { get; private set; }
    public decimal Salary { get; private set; }
    public DateTime HiredDate { get; private set; }
    public string? ProfilePictureUrl { get; private set; }

    // Private parameterless constructor used only by EF Core & static factory
#pragma warning disable CS8618
    private Employee() { }
#pragma warning restore CS8618

    public static Employee Create(
        string firstName,
        string lastName,
        string email,
        string department,
        decimal salary,
        string? profilePictureUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(department);

        if (salary < 0)
            throw new ArgumentOutOfRangeException(nameof(salary), "Salary cannot be negative.");

        return new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Department = department.Trim(),
            Salary = salary,
            HiredDate = DateTime.UtcNow,
            ProfilePictureUrl = profilePictureUrl
        };
    }

    public void Update(
        string firstName,
        string lastName,
        string email,
        string department,
        decimal salary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(department);

        if (salary < 0)
            throw new ArgumentOutOfRangeException(nameof(salary), "Salary cannot be negative.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Department = department.Trim();
        Salary = salary;
    }

    public void UpdateProfilePicture(string? profilePictureUrl)
    {
        ProfilePictureUrl = profilePictureUrl;
    }

    public string FullName => $"{FirstName} {LastName}";
}
