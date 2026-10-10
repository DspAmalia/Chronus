namespace Chronus.Models.MainModels;

public class Department
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Relatie One-to-Many cu Employee
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}