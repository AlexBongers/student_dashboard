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
    public class WorkflowItem : ObservableObject
    {
        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
        
        private bool _isCompleted;
        public bool IsCompleted
        {
            get => _isCompleted;
            set => SetProperty(ref _isCompleted, value);
        }

        public DateTime? Date { get; set; }
    }

    public partial class StudentDetailViewModel : ViewModelBase
    {
        private readonly StudentService _studentService;

        [ObservableProperty]
        private Student? _student;
        
        [ObservableProperty]
        private ObservableCollection<WorkflowItem> _workflowSteps = new();

        [ObservableProperty]
        private ObservableCollection<Contact> _contacts = new();
        
        [ObservableProperty]
        private ObservableCollection<Deadline> _deadlines = new();

        [ObservableProperty]
        private ObservableCollection<Attachment> _attachments = new();
        
        [ObservableProperty]
        private string _newContactType = "Email";
        
        [ObservableProperty]
        private string _newContactContent = "";
        
        [ObservableProperty]
        private DateTime _newContactDate = DateTime.Today;

        // New Deadline inputs
        [ObservableProperty] private string _newDeadlineTitle = "";
        [ObservableProperty] private DateTime _newDeadlineDate = DateTime.Today.AddDays(7);
        // Status Sync properties
        [ObservableProperty]
        private ObservableCollection<string> _availableStatuses = new();
        
        private string _selectedStatus = "";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (SetProperty(ref _selectedStatus, value))
                {
                    // Trigger Status -> Workflow sync when selection changes
                    _ = SyncStatusToWorkflowAsync(value);
                }
            }
        }

        public StudentDetailViewModel(StudentService studentService)
        {
            _studentService = studentService;
        }

        partial void OnStudentChanged(Student? value)
        {
            if (value != null)
            {
                var definitions = GetWorkflowDefinitions(value.Type);
                AvailableStatuses = new ObservableCollection<string>(definitions.Select(d => d.Key));
                
                // Prevent cyclic updates during init by setting backing field directly first
                _selectedStatus = value.Status;
                OnPropertyChanged(nameof(SelectedStatus));

                UpdateWorkflowList(value);
                Contacts = new ObservableCollection<Contact>(value.Contacts.OrderByDescending(c => c.Date));
                Deadlines = new ObservableCollection<Deadline>(value.Deadlines.OrderBy(d => d.DueDate));
                Attachments = new ObservableCollection<Attachment>(value.Attachments.OrderByDescending(a => a.UploadDate));
            }
            else
            {
                WorkflowSteps.Clear();
                Contacts.Clear();
                Deadlines.Clear();
                Attachments.Clear();
            }
            OnPropertyChanged(nameof(ArchiveButtonText));
        }
        
        [RelayCommand]
        public async Task SaveNotes() 
        {
            if (Student == null) return;
            await _studentService.UpdateStudentAsync(Student);
            System.Windows.MessageBox.Show("Notities opgeslagen!", "Succes", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private async Task SyncStatusToWorkflowAsync(string newStatus)
        {
            if (Student == null || string.IsNullOrEmpty(newStatus)) return;
            if (Student.Status == newStatus) return; // Prevent loop

            Student.Status = newStatus;

            var definitions = GetWorkflowDefinitions(Student.Type);
            int targetIndex = definitions.FindIndex(d => d.Key == newStatus);
            if (targetIndex != -1)
            {
                // Iterate model steps and check off anything before/including this target
                for (int i = 0; i < definitions.Count; i++)
                {
                    bool shouldBeCompleted = i <= targetIndex;
                    await _studentService.ToggleWorkflowStepAsync(Student.Id, definitions[i].Key, shouldBeCompleted);
                }
            }

            await _studentService.UpdateStudentAsync(Student);

            // Fully refresh UI
            var updated = (await _studentService.GetActiveStudentsAsync()).FirstOrDefault(s => s.Id == Student.Id);
            if (updated == null) updated = (await _studentService.GetArchivedStudentsAsync()).FirstOrDefault(s => s.Id == Student.Id);
            if (updated != null)
            {
                Student = updated;
            }
        }

        [RelayCommand]
        public async Task ToggleWorkflow(WorkflowItem item)
        {
            if (Student == null) return;
            
            await _studentService.ToggleWorkflowStepAsync(Student.Id, item.Key, item.IsCompleted);
            
            // Workflow -> Status Sync logic
            // Find highest completed step to update the status text (dropdown)
            var definitions = GetWorkflowDefinitions(Student.Type);
            
            // Re-fetch the student to get latest workflow state from DB
            var updatedStudent = (await _studentService.GetActiveStudentsAsync()).FirstOrDefault(s => s.Id == Student.Id) 
                                 ?? (await _studentService.GetArchivedStudentsAsync()).FirstOrDefault(s => s.Id == Student.Id);

            if (updatedStudent != null)
            {
                string highestCompletedKey = definitions.First().Key; // default to first

                foreach (var def in definitions)
                {
                    var step = updatedStudent.WorkflowSteps.FirstOrDefault(w => w.StepKey == def.Key);
                    if (step != null && step.Completed)
                    {
                        highestCompletedKey = def.Key; // keeps overwriting until the last completed one is found
                    }
                }

                if (Student.Status != highestCompletedKey)
                {
                    Student.Status = highestCompletedKey;
                    await _studentService.UpdateStudentAsync(Student);
                }

                Student = updatedStudent; // refresh UI completely
            }
        }
        
        public string ArchiveButtonText => Student?.Archived == true ? "Herstellen" : "Archiveren";
        
        [RelayCommand]
        public async Task ArchiveStudent()
        {
            if (Student == null) return;
            
            Student.Archived = !Student.Archived;
            Student.ArchivedAt = Student.Archived ? DateTime.Now : null;
            Student.Status = Student.Archived ? "afgerond" : Student.Status;
            
            await _studentService.UpdateStudentAsync(Student);
            
            // Re-trigger property changed to update UI
            OnPropertyChanged(nameof(Student));
            OnPropertyChanged(nameof(ArchiveButtonText));
            
            System.Windows.MessageBox.Show(
                Student.Archived ? "Student gearchiveerd!" : "Student hersteld!",
                "Succes", 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Information);
                
            // Ideally we'd broadcast an event here so StudentListViewModel can reload
            // For now, the user can just re-select or refresh the list by clicking.
        }
        
        [RelayCommand]
        public async Task AddContact()
        {
            if (Student == null || string.IsNullOrWhiteSpace(NewContactContent)) return;

            var contact = new Contact
            {
                StudentId = Student.Id,
                Type = NewContactType,
                Content = NewContactContent,
                Date = NewContactDate
            };

            await _studentService.AddContactAsync(contact);
            
            Contacts.Insert(0, contact);
            NewContactContent = ""; // Reset
            NewContactDate = DateTime.Today;
        }

        [RelayCommand]
        public async Task AddDeadline()
        {
            if (Student == null || string.IsNullOrWhiteSpace(NewDeadlineTitle)) return;

            var deadline = new Deadline
            {
                StudentId = Student.Id,
                Title = NewDeadlineTitle,
                DueDate = NewDeadlineDate,
                IsCompleted = false
            };

            await _studentService.AddDeadlineAsync(deadline);
            Deadlines.Add(deadline);
            Student?.Deadlines?.Add(deadline); // Keep model in sync for HasUrgentDeadline
            
            // Re-sort
            var sorted = Deadlines.OrderBy(d => d.DueDate).ToList();
            Deadlines = new ObservableCollection<Deadline>(sorted);
            
            NewDeadlineTitle = "";
            OnPropertyChanged(nameof(Student));
        }

        [RelayCommand]
        public async Task ToggleDeadline(Deadline deadline)
        {
            await _studentService.UpdateDeadlineAsync(deadline);
            OnPropertyChanged(nameof(Student));
        }

        [RelayCommand]
        public async Task DeleteDeadline(Deadline deadline)
        {
            await _studentService.DeleteDeadlineAsync(deadline);
            Deadlines.Remove(deadline);
            Student?.Deadlines?.Remove(deadline); // Keep model in sync
            OnPropertyChanged(nameof(Student));
        }
        
        [RelayCommand]
        public async Task AddFile()
        {
            if (Student == null) return;
            
            // Use OpenFileDialog
            var dialog = new Microsoft.Win32.OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                var file = new Attachment
                {
                    StudentId = Student.Id,
                    FileName = System.IO.Path.GetFileName(dialog.FileName),
                    FilePath = dialog.FileName, // In a real app, copy this to local storage
                    UploadDate = DateTime.Now
                };
                
                await _studentService.AddAttachmentAsync(file);
                Attachments.Insert(0, file);
            }
        }

        [RelayCommand]
        public async Task DeleteFile(Attachment attachment)
        {
            await _studentService.DeleteAttachmentAsync(attachment);
            Attachments.Remove(attachment);
        }
        
        [RelayCommand]
        public void OpenFile(Attachment attachment)
        {
            try
            {
                new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo(attachment.FilePath)
                    {
                        UseShellExecute = true
                    }
                }.Start();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Kan bestand niet openen: {ex.Message}", "Fout", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void UpdateWorkflowList(Student student)
        {
            WorkflowSteps.Clear();
            var definitions = GetWorkflowDefinitions(student.Type);
            
            foreach (var def in definitions)
            {
                var existing = student.WorkflowSteps.FirstOrDefault(w => w.StepKey == def.Key);
                WorkflowSteps.Add(new WorkflowItem
                {
                    Key = def.Key,
                    Label = def.Label,
                    IsCompleted = existing?.Completed ?? false,
                    Date = existing?.CompletedDate
                });
            }
        }

        private List<(string Key, string Label)> GetWorkflowDefinitions(string type)
        {
            // Merged definitions from Student Tracker
            return new List<(string, string)>
            {
                ("opstart", "Opstart"),
                ("pva", "PvA / Stageplan"),
                ("concept1", "1e Concept Verslag"),
                ("concept2", "2e Concept Verslag"),
                ("definitief", "Definitief Verslag"),
                ("herkansing", "Herkansing"),
                ("afgerond", "Afgerond")
            };
        }
    }
}
