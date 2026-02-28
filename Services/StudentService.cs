using Microsoft.EntityFrameworkCore;
using StageManagementSystem.Data;
using StageManagementSystem.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StageManagementSystem.Services
{
    public class StudentService
    {
        private readonly AppDbContext _context;

        public StudentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Student>> GetAllStudentsAsync()
        {
            return await _context.Students
                .IgnoreQueryFilters()
                .Include(s => s.Contacts)
                .Include(s => s.WorkflowSteps)
                .Include(s => s.Deadlines)
                .Include(s => s.Attachments)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }
        
        public async Task<List<Student>> GetActiveStudentsAsync()
        {
            return await _context.Students
                .Where(s => !s.Archived && (!string.IsNullOrEmpty(s.FirstName) || !string.IsNullOrEmpty(s.LastName)))
                .Include(s => s.Contacts)
                .Include(s => s.WorkflowSteps)
                .Include(s => s.Deadlines)
                .Include(s => s.Attachments)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Student>> GetArchivedStudentsAsync()
        {
            return await _context.Students
                .Where(s => s.Archived && (!string.IsNullOrEmpty(s.FirstName) || !string.IsNullOrEmpty(s.LastName)))
                .Include(s => s.Contacts)
                .Include(s => s.WorkflowSteps)
                .Include(s => s.Deadlines)
                .Include(s => s.Attachments)
                .OrderByDescending(s => s.ArchivedAt)
                .ToListAsync();
        }

        public async Task AddStudentAsync(Student student)
        {
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateStudentAsync(Student student)
        {
            _context.Students.Update(student);
            await _context.SaveChangesAsync();
        }

        public async Task AddContactAsync(Contact contact)
        {
            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();
        }

        public async Task AddDeadlineAsync(Deadline deadline)
        {
            _context.Deadlines.Add(deadline);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateDeadlineAsync(Deadline deadline)
        {
            _context.Deadlines.Update(deadline);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteDeadlineAsync(Deadline deadline)
        {
            _context.Deadlines.Remove(deadline);
            await _context.SaveChangesAsync();
        }

        public async Task AddAttachmentAsync(Attachment attachment)
        {
            _context.Attachments.Add(attachment);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAttachmentAsync(Attachment attachment)
        {
            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync();
        }
        
        public async Task ToggleWorkflowStepAsync(int studentId, string stepKey, bool completed)
        {
            var student = await _context.Students.Include(s => s.WorkflowSteps).FirstOrDefaultAsync(s => s.Id == studentId);
            if (student == null) return;

            var step = student.WorkflowSteps.FirstOrDefault(w => w.StepKey == stepKey);
            if (step == null)
            {
                step = new WorkflowStep { StudentId = studentId, StepKey = stepKey };
                student.WorkflowSteps.Add(step);
            }

            step.Completed = completed;
            step.CompletedDate = completed ? DateTime.Now : null;

            if (completed)
            {
                student.Status = stepKey; // auto-update status to latest completed step? Or logical?
            }

            await _context.SaveChangesAsync();
        }
    }
}
