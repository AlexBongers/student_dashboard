using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StageManagementSystem.Models;
using StageManagementSystem.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StageManagementSystem.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly StudentService _studentService;

        [ObservableProperty]
        private StudentListViewModel _studentListViewModel;

        [ObservableProperty]
        private StudentDetailViewModel _studentDetailViewModel;
        
        // Stats
        [ObservableProperty] private int _needsActionCount;
        [ObservableProperty] private int _inReviewCount;
        [ObservableProperty] private int _activeCount;
        [ObservableProperty] private int _completedMonthCount;
        
        // Chart Data
        [ObservableProperty] private System.Collections.ObjectModel.ObservableCollection<ChartItem>? _statusDistribution;

        public record ChartItem(string Label, int Value, double Percentage, string ColorResource);

        public MainViewModel(StudentService studentService)
        {
            _studentService = studentService;
            
            // We instantiate child ViewModels here. In a more complex app, these might be injected too.
            StudentListViewModel = new StudentListViewModel(_studentService);
            StudentDetailViewModel = new StudentDetailViewModel(_studentService);

            // Sync selection: List -> Detail
            StudentListViewModel.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(StudentListViewModel.SelectedStudent))
                {
                    StudentDetailViewModel.Student = StudentListViewModel.SelectedStudent;
                }
            };
            
            // Refresh stats when data changes in list
            StudentListViewModel.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(StudentListViewModel.Students)) // When list reloads
                {
                     _ = UpdateStats();
                }
            };
            
            _ = UpdateStats();
        }
        
        [RelayCommand]
        public async Task AddStudent()
        {
            var vm = new AddStudentViewModel(_studentService);
            var window = new Views.AddStudentWindow
            {
                DataContext = vm,
                Owner = System.Windows.Application.Current.MainWindow
            };
            
            if (window.ShowDialog() == true)
            {
                await Refresh();
            }
        }

        [RelayCommand]
        public async Task Export()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV (Comma delimited)|*.csv",
                Title = "Exporteer Studenten",
                FileName = $"StudentenExport_{DateTime.Now:yyyyMMdd}.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var students = await _studentService.GetActiveStudentsAsync();
                    var sb = new System.Text.StringBuilder();
                    
                    // Header
                    sb.AppendLine("Voornaam,Achternaam,Studentnummer,Bedrijf,Type,Rol,Status,Email,StartDatum,EindDatum");
                    
                    foreach (var s in students)
                    {
                        // Escape quotes and commas by wrapping in quotes
                        string SafeCsv(string? input)
                        {
                            if (string.IsNullOrEmpty(input)) return "";
                            if (input.Contains(",") || input.Contains("\"") || input.Contains("\n"))
                            {
                                return $"\"{input.Replace("\"", "\"\"")}\"";
                            }
                            return input;
                        }

                        sb.AppendLine($"{SafeCsv(s.FirstName)},{SafeCsv(s.LastName)},{SafeCsv(s.StudentNumber)},{SafeCsv(s.Company)},{SafeCsv(s.Type)},{SafeCsv(s.MyRole)},{SafeCsv(s.Status)},{SafeCsv(s.Email)},{s.StartDate:yyyy-MM-dd},{s.EndDate:yyyy-MM-dd}");
                    }

                    System.IO.File.WriteAllText(dialog.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                    System.Windows.MessageBox.Show("Export succesvol!", "Succes", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Fout bij exporteren: {ex.Message}", "Fout", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        public void Import()
        {
             System.Windows.MessageBox.Show("Import feature coming soon!", "Import", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        [RelayCommand]
        public async Task Refresh()
        {
            await StudentListViewModel.LoadData();
            await UpdateStats();
        }

        public async Task UpdateStats()
        {
             var students = await _studentService.GetActiveStudentsAsync();
             ActiveCount = students.Count;
             
             int action = 0;
             foreach(var s in students) {
                 var lastContact = s.Contacts.OrderByDescending(c => c.Date).FirstOrDefault();
                 if (lastContact == null || (DateTime.Now - lastContact.Date).TotalDays > 14) action++;
             }
             NeedsActionCount = action;

             InReviewCount = students.Count(s => s.Status == "concept1" || s.Status == "concept2" || s.Status == "eindversie");
             
             var archived = await _studentService.GetArchivedStudentsAsync();
             // For simplicity, defining "Completed this month" as archived this month or end date this month
             var now = DateTime.Now;
             CompletedMonthCount = archived.Count(s => (s.ArchivedAt.HasValue && s.ArchivedAt.Value.Month == now.Month && s.ArchivedAt.Value.Year == now.Year));

             // Populate Status Distribution Chart
             var allStudents = students.Concat(archived).ToList();
             if (allStudents.Any())
             {
                 var groups = allStudents.GroupBy(s => s.Status)
                                         .OrderByDescending(g => g.Count())
                                         .ToList();

                 var chartData = new System.Collections.ObjectModel.ObservableCollection<ChartItem>();
                 
                 // Map statuses to theme colors
                 string GetColorForStatus(string status) => status switch
                 {
                     "opstart" => "InfoColor",
                     "pva" => "WarningColor",
                     "concept1" => "PrimaryColor",
                     "concept2" => "PrimaryColor",
                     "eindversie" => "SuccessColor",
                     "definitief" => "SuccessColor",
                     "afgerond" => "SuccessColor",
                     "herkansing" => "DangerColor",
                     _ => "Gray400"
                 };

                 foreach (var group in groups)
                 {
                     double pct = (double)group.Count() / allStudents.Count;
                     chartData.Add(new ChartItem(
                         group.Key.ToUpper(),
                         group.Count(),
                         pct, // 0.0 to 1.0
                         GetColorForStatus(group.Key)
                     ));
                 }
                 
                 StatusDistribution = chartData;
             }
             else
             {
                 StatusDistribution = new System.Collections.ObjectModel.ObservableCollection<ChartItem>();
             }
        }

        [RelayCommand]
        public void FilterNeedsAction()
        {
            StudentListViewModel.StatFilter = StatFilter.NeedsAction;
            StudentListViewModel.ShowArchived = false;
        }

        [RelayCommand]
        public void FilterInReview()
        {
             StudentListViewModel.StatFilter = StatFilter.InReview;
             StudentListViewModel.ShowArchived = false;
        }

        [RelayCommand]
        public void FilterActive()
        {
             StudentListViewModel.StatFilter = StatFilter.Active;
             StudentListViewModel.ShowArchived = false;
        }

        [RelayCommand]
        public void FilterCompleted()
        {
             StudentListViewModel.StatFilter = StatFilter.Completed;
             // This might auto-trigger load of archived due to logic in VM
        }
    }
}
