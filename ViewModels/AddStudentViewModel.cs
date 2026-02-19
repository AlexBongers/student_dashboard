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

        [ObservableProperty]
        private string? _profilePicturePath;

        public AddStudentViewModel(StudentService studentService)
        {
            _studentService = studentService;
        }

        [RelayCommand]
        public void SelectProfilePicture()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Selecteer Profielfoto"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string appData = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StageManagementSystem", "ProfilePictures");
                    System.IO.Directory.CreateDirectory(appData);

                    string ext = System.IO.Path.GetExtension(dialog.FileName);
                    string newFileName = Guid.NewGuid().ToString() + ext;
                    string destPath = System.IO.Path.Combine(appData, newFileName);

                    System.IO.File.Copy(dialog.FileName, destPath, true);
                    ProfilePicturePath = destPath;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Fout bij kopiëren van afbeelding: {ex.Message}", "Fout", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        public async Task Save()
        {
            if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName) || string.IsNullOrWhiteSpace(Company))
            {
                System.Windows.MessageBox.Show(
                    "Vul a.u.b. alle verplichte velden in (Voornaam, Achternaam, Bedrijf).",
                    "Validatie Fout",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

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
                Status = "Opstart", // New default
                CreatedAt = DateTime.Now,
                ProfilePicturePath = ProfilePicturePath
            };

            try 
            {
                await _studentService.AddStudentAsync(student);
            } 
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Fout bij aanmaken nieuwe student: {ex.Message}", "Fout", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
