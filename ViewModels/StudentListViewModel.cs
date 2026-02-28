using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StageManagementSystem.Models;
using StageManagementSystem.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace StageManagementSystem.ViewModels
{
    public enum StatFilter { None, NeedsAction, InReview, Active, Completed }

    public partial class StudentListViewModel : ViewModelBase
    {
        private readonly StudentService _studentService;
        private List<Student> _allStudents = new();

        [ObservableProperty]
        private ObservableCollection<Student> _students = new();

        [ObservableProperty]
        private string _searchQuery = "";

        [ObservableProperty]
        private string _filterType = "all"; // all, stage, scriptie

        [ObservableProperty]
        private StatFilter _statFilter = StatFilter.None;

        [ObservableProperty]
        private bool _showArchived = false;

        [ObservableProperty]
        private Student? _selectedStudent;

        public StudentListViewModel(StudentService studentService)
        {
            _studentService = studentService;
            // LoadData needs to be called explicitly or via OnInitialized if using a sophisticated framework,
            // or called from MainViewModel. We can call it here but it's async void equivalent (fire and forget).
            _ = LoadData();
        }
        
        [RelayCommand]
        public async Task LoadData()
        {
            // If filter is "Completed", we might need archived students even if ShowArchived is false? 
            // Actually, let's just stick to the current ShowArchived flag. 
            // If StatFilter is Completed, we should probably force load archived or ALL.
            // For now, let's load ACTIVE by default, unless ShowArchived is true.
            
            try
            {
                if (ShowArchived || StatFilter == StatFilter.Completed) 
                     _allStudents = await _studentService.GetArchivedStudentsAsync();
                else
                     _allStudents = await _studentService.GetActiveStudentsAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Fout bij laden van studenten: {ex.Message}", "Fout", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                _allStudents = new List<Student>();
            }

            ApplyFilter();
        }

        partial void OnSearchQueryChanged(string value) => ApplyFilter();
        partial void OnFilterTypeChanged(string value) => ApplyFilter();
        partial void OnStatFilterChanged(StatFilter value) => _ = LoadData(); // Reload data because Completed needs different source
        
        partial void OnShowArchivedChanged(bool value)
        {
             _ = LoadData();
             if (value) StatFilter = StatFilter.None; // Reset stat filter when manually toggling archive
        }

        private void ApplyFilter()
        {
            var filtered = _allStudents.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                filtered = filtered.Where(s => s.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) || 
                                             s.Company.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
            }

            if (FilterType != "all")
            {
                // Map filter type "stage"/"afstudeer" correctly
                // In AddStudentViewModel we use "stage" and "scriptie"
                // In list view we used "all", "stage", "afstudeer" (which was scriptie)
                string typeKey = FilterType == "afstudeer" ? "scriptie" : FilterType;
                filtered = filtered.Where(s => s.Type == typeKey);
            }

            switch (StatFilter)
            {
                case StatFilter.NeedsAction:
                    filtered = filtered.Where(s => {
                        var lastContact = s.Contacts.OrderByDescending(c => c.Date).FirstOrDefault();
                        return lastContact == null || (DateTime.Now - lastContact.Date).TotalDays > 14;
                    });
                    break;
                case StatFilter.InReview:
                    filtered = filtered.Where(s => s.Status == "Concept 1" || s.Status == "Concept 2" || s.Status == "Eindversie" || s.Status == "Definitief");
                    break;
                case StatFilter.Active:
                    // Just basic active list, potentially already filtered by LoadData
                    break;
                case StatFilter.Completed:
                    var now = DateTime.Now;
                    filtered = filtered.Where(s => s.ArchivedAt.HasValue && s.ArchivedAt.Value.Month == now.Month && s.ArchivedAt.Value.Year == now.Year);
                    break;
            }

            Students = new ObservableCollection<Student>(filtered);
        }
        
        [RelayCommand]
        public async Task ToggleArchive(Student student)
        {
             student.Archived = !student.Archived;
             student.ArchivedAt = student.Archived ? DateTime.Now : null;
             student.Status = student.Archived ? "Afgerond" : student.Status;
             
             try
             {
                 await _studentService.UpdateStudentAsync(student);
             }
             catch (Exception ex)
             {
                 System.Windows.MessageBox.Show($"Fout bij archiveren bewerken: {ex.Message}", "Fout", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
             }
             await LoadData(); // Refresh list to remove/add it based on current view
        }

        [RelayCommand]
        public async Task BulkArchive()
        {
            var selectedStudents = Students.Where(s => s.IsSelected).ToList();
            if (!selectedStudents.Any()) 
            {
                System.Windows.MessageBox.Show("Selecteer eerst één of meerdere studenten.", "Bulk Archiveren", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            var result = System.Windows.MessageBox.Show($"Weet je zeker dat je {selectedStudents.Count} student(en) wilt archiveren?", "Bevestiging", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                foreach (var student in selectedStudents)
                {
                    student.Archived = true;
                    student.ArchivedAt = DateTime.Now;
                    student.Status = "Afgerond";
                    await _studentService.UpdateStudentAsync(student);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Fout tijdens bulk archiveren: {ex.Message}", "Fout", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                // We'll still try to deselect and reload below so the UI is somewhat fresh.
            }

            // Deselect all
            foreach (var s in _allStudents) s.IsSelected = false;

            await LoadData();
        }
    }
}
