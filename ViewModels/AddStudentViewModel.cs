using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StageManagementSystem.Models;
using StageManagementSystem.Services;
using System;
using System.Threading.Tasks;

namespace StageManagementSystem.ViewModels
{
    public partial class AddStudentViewModel : ViewModelBase
    {
        private readonly StudentService _studentService;

        [ObservableProperty]
        private string _firstName = "";

        [ObservableProperty]
        private string _lastName = "";
        
        [ObservableProperty]
        private string _studentNumber = "";
        
        [ObservableProperty]
        private string _email = "";
        
        [ObservableProperty]
        private string _phone = "";
        
        [ObservableProperty]
        private string _type = "stage";
        
        [ObservableProperty]
        private string _myRole = "docentbegeleider";
        
        [ObservableProperty]
        private string _company = "";
        
        [ObservableProperty]
        private string _location = "";
        
        [ObservableProperty]
        private DateTime _startDate = DateTime.Today;
        
        [ObservableProperty]
        private DateTime _endDate = DateTime.Today.AddMonths(5);

        public AddStudentViewModel(StudentService studentService)
        {
            _studentService = studentService;
        }

        [RelayCommand]
        public async Task Save()
        {
            if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName) || string.IsNullOrWhiteSpace(Company)) return;

            var student = new Student
            {
                FirstName = FirstName,
                LastName = LastName,
                StudentNumber = StudentNumber,
                Email = Email,
                Phone = Phone,
                Type = Type,
                MyRole = MyRole,
                Company = Company,
                Location = Location,
                StartDate = StartDate,
                EndDate = EndDate,
                Status = "opstart", // New default
                CreatedAt = DateTime.Now
            };

            await _studentService.AddStudentAsync(student);
        }
    }
}
