namespace Chronus.Models.MainModels;
using Microsoft.AspNetCore.Identity;

public class Role: IdentityRole<Guid>
{
    public string? Description { get; set; }

    // Relatie One-to-Many cu Employee
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}