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

        // Alerts Data
        [ObservableProperty] private System.Collections.ObjectModel.ObservableCollection<AlertItem> _alerts = new();

        public record ChartItem(string Label, int Value, double Percentage, string ColorResource);
        public record AlertItem(string Message, string Description, string Icon, string ColorResource, Student? RelatedStudent);

        public MainViewModel(StudentService studentService)
        {
            _studentService = studentService;
            
            // We instantiate child ViewModels here. In a more complex app, these might be injected too.
            StudentListViewModel = new StudentListViewModel(_studentService);
            StudentDetailViewModel = new StudentDetailViewModel(_studentService);

            // Wiring up the close action
            StudentDetailViewModel.OnCloseDetail = () => 
            {
                StudentListViewModel.SelectedStudent = null;
            };

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
        public async Task Import()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "CSV (Comma delimited)|*.csv",
                Title = "Importeer Studenten"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string[] lines = System.IO.File.ReadAllLines(dialog.FileName);
                    if (lines.Length <= 1) return; // Empty or just header

                    int importedCount = 0;
                    for (int i = 1; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var values = line.Split(',');

                        if (values.Length >= 4)
                        {
                            var student = new Student
                            {
                                FirstName = values[0].Trim('"', ' '),
                                LastName = values[1].Trim('"', ' '),
                                StudentNumber = values.Length > 2 ? values[2].Trim('"', ' ') : "",
                                Company = values.Length > 3 ? values[3].Trim('"', ' ') : "",
                                Type = values.Length > 4 ? values[4].Trim('"', ' ') : "stage",
                                MyRole = values.Length > 5 ? values[5].Trim('"', ' ') : "docentbegeleider",
                                Status = values.Length > 6 ? values[6].Trim('"', ' ') : "Opstart",
                                Email = values.Length > 7 ? values[7].Trim('"', ' ') : "",
                                StartDate = values.Length > 8 && DateTime.TryParse(values[8].Trim('"', ' '), out var sd) ? sd : DateTime.Today,
                                EndDate = values.Length > 9 && DateTime.TryParse(values[9].Trim('"', ' '), out var ed) ? ed : DateTime.Today.AddMonths(5)
                            };

                            await _studentService.AddStudentAsync(student);
                            importedCount++;
                        }
                    }

                    if (importedCount > 0)
                    {
                        System.Windows.MessageBox.Show($"{importedCount} student(en) succesvol geïmporteerd!", "Succes", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        await Refresh();
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Fout bij importeren: {ex.Message}", "Fout", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        public async Task Refresh()
        {
            await StudentListViewModel.LoadData();
            await UpdateStats();
        }

        public async Task UpdateStats()
        {
             try
             {
                 var students = await _studentService.GetActiveStudentsAsync();
                 ActiveCount = students.Count;
                 
                 int action = 0;
                 foreach(var s in students) {
                     var lastContact = s.Contacts.OrderByDescending(c => c.Date).FirstOrDefault();
                     if (lastContact == null || (DateTime.Now - lastContact.Date).TotalDays > 14) action++;
                 }
                 NeedsActionCount = action;

                 InReviewCount = students.Count(s => s.Status == "Concept 1" || s.Status == "Concept 2" || s.Status == "Eindversie" || s.Status == "Definitief");
                 
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
                         "Opstart" => "InfoColor",
                         "PvA" => "WarningColor",
                         "Concept 1" => "PrimaryColor",
                         "Concept 2" => "PrimaryColor",
                         "Eindversie" => "SuccessColor",
                         "Definitief" => "SuccessColor",
                         "Afgerond" => "SuccessColor",
                         "Herkansing" => "DangerColor",
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

                 // Generate Alerts for Action Center
                 var newAlerts = new System.Collections.ObjectModel.ObservableCollection<AlertItem>();

                 foreach (var s in students)
                 {
                     // 1. Check for urgent deadlines (within 7 days and not completed)
                     var urgentDeadline = s.Deadlines?.FirstOrDefault(d => !d.IsCompleted && (d.DueDate.Date - DateTime.Today).TotalDays <= 7);
                     if (urgentDeadline != null)
                     {
                         int daysLeft = (int)(urgentDeadline.DueDate.Date - DateTime.Today).TotalDays;
                         string timeText = daysLeft < 0 ? $"({Math.Abs(daysLeft)} dagen te laat)" : $"({daysLeft} dagen)";
                         newAlerts.Add(new AlertItem(
                             $"Dringende Deadline: {s.Name}",
                             $"{urgentDeadline.Title} {timeText}",
                             "\xE783", // Warning icon
                             "DangerColor",
                             s
                         ));
                     }

                     // 2. Check for actionable items (Opstart phase for > 14 days)
                     var lastContact = s.Contacts?.OrderByDescending(c => c.Date).FirstOrDefault();
                     double daysSinceContact = lastContact != null ? (DateTime.Now - lastContact.Date).TotalDays : (DateTime.Now - s.CreatedAt).TotalDays;
                     if (s.Status == "Opstart" && daysSinceContact > 14)
                     {
                         newAlerts.Add(new AlertItem(
                             $"Actie Vereist: {s.Name}",
                             $"Staat al {(int)daysSinceContact} dagen op 'Opstart' zonder contact.",
                             "\xE814", // Clock/History icon
                             "WarningColor",
                             s
                         ));
                     }

                     // 3. Check for students awaiting review (Concept 1, 2, Eindversie)
                     if (s.Status.StartsWith("Concept") || s.Status == "Eindversie")
                     {
                         newAlerts.Add(new AlertItem(
                             $"In Review: {s.Name}",
                             $"Wacht op beoordeling voor '{s.Status}'",
                             "\xE7B3", // Document icon
                             "InfoColor",
                             s
                         ));
                     }
                 }

                 // Sort alerts (Danger -> Warning -> Info) and attach
                 var sortedAlerts = newAlerts.OrderByDescending(a => a.ColorResource == "DangerColor")
                                             .ThenByDescending(a => a.ColorResource == "WarningColor")
                                             .ThenByDescending(a => a.ColorResource == "InfoColor");
                 Alerts = new System.Collections.ObjectModel.ObservableCollection<AlertItem>(sortedAlerts);
             }
             catch (Exception ex)
             {
                 System.Windows.MessageBox.Show($"Fout bij ophalen statistieken: {ex.Message}", "Fout", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
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
        
        [RelayCommand]
        public void OpenStudentFromAlert(AlertItem alert)
        {
            if (alert.RelatedStudent != null)
            {
                StudentListViewModel.SelectedStudent = StudentListViewModel.Students.FirstOrDefault(s => s.Id == alert.RelatedStudent.Id);
                // The parent view/window logic usually handles navigation, but since this is a Single Page App setup, 
                // selecting the student will populate the StudentDetailViewModel automatically.
            }
        }
    }
}
