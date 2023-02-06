namespace EmployeesTask.Controllers
{
    using EmployeesTask.Models;
    using Microsoft.AspNetCore.Mvc;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    public class HomeController : Controller
    {
        private HashSet<Project> projects;
        private ResultViewModel resultViewModel;

        public HomeController()
        {
            projects = new HashSet<Project>();
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file.ContentType != "application/vnd.ms-excel" && file.ContentType != "text/csv")
            {
                TempData["WrongFileFormat"] = "Wrong File Format. Please select file with extension '.csv'!";

                return View("Index");
            }

            await ReadFile(file);
            
            CheckProjectsForEmployeePairs();

            if (resultViewModel != null)
            {
                GetAllProjectsOfEmployeesPair();
            }

            return View("Index", resultViewModel);
        }

        private async Task ReadFile(IFormFile file)
        {
            try
            {
                if (file != null && file.Length > 0)
                {
                    using (var stream = new MemoryStream())
                    {
                        await file.CopyToAsync(stream);

                        var byteArray = stream.ToArray();

                        var stringFromByteArray = Encoding.UTF8.GetString(byteArray);
                        var stringArray = stringFromByteArray.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

                        ProcessesData(stringArray);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["NoFileUploaded"] = "Please Select a File to Proceed";
            }
        }

        private void ProcessesData(string[] stringArray)
        {
            for (int i = 1; i < stringArray.Length; i++)
            {
                var currentLine = stringArray[i].Split(new string[] { ",", ";" }, StringSplitOptions.RemoveEmptyEntries);

                var currentProjectId = int.Parse(currentLine[1]);
                Project project = null;

                if (!projects.Any(p => p.Id == currentProjectId))
                {
                    project = new Project()
                    {
                        Id = currentProjectId,
                    };

                    projects.Add(project);
                }
                else
                {
                    project = projects.First(p => p.Id == currentProjectId);
                }

                var employeeId = int.Parse(currentLine[0]);
                var dateFrom = DateTime.Parse(currentLine[2]);
                var dateTo = DateTime.Now;

                if (currentLine.Length > 3)
                {
                    dateTo = DateTime.Parse(currentLine[3]);
                }

                var currentEmployee = new Employee()
                {
                    Id = employeeId,
                    DateFrom = dateFrom,
                    DateTo = dateTo,
                };

                project.Employees.Add(currentEmployee);
            }
        }

        private void GetAllProjectsOfEmployeesPair()
        {
            var otherProjects = projects
                .Where(p => p.Employees
                    .Any(e => e.Id == resultViewModel.FirstEmployeeId) &&
                    p.Employees
                    .Any(e => e.Id == resultViewModel.SecondEmployeeId))
                .Select(p => new ResultViewModel()
                {
                    FirstEmployeeId = resultViewModel.FirstEmployeeId,
                    SecondEmployeeId = resultViewModel.SecondEmployeeId,
                    DaysWorked = CalculateDays(
                        p.Employees
                            .First(e => e.Id == resultViewModel.FirstEmployeeId), 
                        p.Employees
                            .First(e => e.Id == resultViewModel.SecondEmployeeId)),
                    ProjectId = p.Id,
                })
                .OrderByDescending(p => p.DaysWorked)
                .ToList();

            resultViewModel.OtherProjects = otherProjects.Where(p => p.DaysWorked > 0).ToList();
        }

        private void CheckProjectsForEmployeePairs()
        {
            foreach (var project in projects)
            {
                if (project.Employees.Count < 2)
                {
                    continue;
                }

                var currentProjectEmployees = project
                    .Employees
                    .OrderBy(d => d.DateFrom)
                    .ThenBy(d => d.DateTo)
                    .ToList();

                for (int i = 0; i < currentProjectEmployees.Count - 1; i++)
                {
                    var firstEmployee = currentProjectEmployees[i];

                    for (int j = i + 1; j < currentProjectEmployees.Count; j++)
                    {
                        var secondEmployee = currentProjectEmployees[j];

                        var daysWorked = CalculateDays(firstEmployee, secondEmployee);

                        if (resultViewModel == null)
                        {
                            resultViewModel = new ResultViewModel
                            {
                                FirstEmployeeId = firstEmployee.Id,
                                SecondEmployeeId = secondEmployee.Id,
                                ProjectId = project.Id,
                                DaysWorked = daysWorked,
                            };
                        }
                        else
                        {
                            if (resultViewModel.DaysWorked < daysWorked)
                            {
                                resultViewModel.FirstEmployeeId = firstEmployee.Id;
                                resultViewModel.SecondEmployeeId = secondEmployee.Id;
                                resultViewModel.ProjectId = project.Id;
                                resultViewModel.DaysWorked = daysWorked;
                            }
                        }
                    }
                }
            }
        }

        private int CalculateDays(Employee firstEmployee, Employee secondEmployee)
        {
            var days = 0;

            if (firstEmployee.DateTo > secondEmployee.DateFrom)
            {
                var dateFrom = secondEmployee.DateFrom;
                var dateTo = firstEmployee.DateTo < secondEmployee.DateTo ?
                    firstEmployee.DateTo
                    : secondEmployee.DateTo;

                days = (dateTo - dateFrom).Days;
            }

            return days;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}