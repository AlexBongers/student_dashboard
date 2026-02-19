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

        public StudentDetailViewModel(StudentService studentService)
        {
            _studentService = studentService;
        }

        partial void OnStudentChanged(Student? value)
        {
            if (value != null)
            {
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
        }
        
        [RelayCommand]
        public async Task SaveNotes() 
        {
            if (Student == null) return;
            await _studentService.UpdateStudentAsync(Student);
            System.Windows.MessageBox.Show("Notities opgeslagen!", "Succes", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        [RelayCommand]
        public async Task ToggleWorkflow(WorkflowItem item)
        {
            if (Student == null) return;
            
            await _studentService.ToggleWorkflowStepAsync(Student.Id, item.Key, item.IsCompleted);
            
            // Logic to sync status
            // If checking a box, we might want to update the status to this step? 
            // Or better: the status should be the "highest" completed step?
            // Let's assume sequential progress.
            
            if (item.IsCompleted)
            {
                // When marking as complete, set status to this step
                // Ideally we'd check if this is "higher" than current, but simple approach is fine for now
                Student.Status = item.Key;
                await _studentService.UpdateStudentAsync(Student);
            }
            
            // Refresh to get updated dates/state
             var updated = (await _studentService.GetActiveStudentsAsync()).FirstOrDefault(s => s.Id == Student.Id);
             if (updated == null) updated = (await _studentService.GetArchivedStudentsAsync()).FirstOrDefault(s => s.Id == Student.Id);
             
             if (updated != null)
             {
                 Student = updated; // Re-trigger OnStudentChanged to refresh UI
             }
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
            // Re-sort
            var sorted = Deadlines.OrderBy(d => d.DueDate).ToList();
            Deadlines = new ObservableCollection<Deadline>(sorted);
            
            NewDeadlineTitle = "";
        }

        [RelayCommand]
        public async Task ToggleDeadline(Deadline deadline)
        {
            await _studentService.UpdateDeadlineAsync(deadline);
        }

        [RelayCommand]
        public async Task DeleteDeadline(Deadline deadline)
        {
            await _studentService.DeleteDeadlineAsync(deadline);
            Deadlines.Remove(deadline);
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
