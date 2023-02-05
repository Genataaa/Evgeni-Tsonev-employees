namespace EmployeesTask.Models
{
    public class ResultViewModel
    {
        public int FirstEmployeeId { get; set; }

        public int SecondEmployeeId { get; set; }

        public int ProjectId { get; set; }

        public int DaysWorked { get; set; }

        public List<ResultViewModel> OtherProjects { get; set; } = new List<ResultViewModel>();
    }
}
