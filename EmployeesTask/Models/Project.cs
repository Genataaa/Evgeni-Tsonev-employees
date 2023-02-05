namespace EmployeesTask.Models
{
    public class Project
    {
        public int Id { get; set; }

        public List<Employee> Employees { get; set; } = new List<Employee>();
    }
}
