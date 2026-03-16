using Microsoft.EntityFrameworkCore;
using WebDashboard.Data;
using WebDashboard.Models;

namespace WebDashboard.Services
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
                .Where(s => !s.Archived)
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
                .Where(s => s.Archived)
                .Include(s => s.Contacts)
                .Include(s => s.WorkflowSteps)
                .Include(s => s.Deadlines)
                .Include(s => s.Attachments)
                .OrderByDescending(s => s.ArchivedAt)
                .ToListAsync();
        }

        public async Task<Student?> GetStudentByIdAsync(int id)
        {
            return await _context.Students
                .Include(s => s.Contacts)
                .Include(s => s.WorkflowSteps)
                .Include(s => s.Deadlines)
                .Include(s => s.Attachments)
                .FirstOrDefaultAsync(s => s.Id == id);
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

        public async Task DeleteStudentAsync(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddContactAsync(Contact contact)
        {
            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteContactAsync(int id)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact != null)
            {
                _context.Contacts.Remove(contact);
                await _context.SaveChangesAsync();
            }
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

        public async Task DeleteDeadlineAsync(int id)
        {
            var deadline = await _context.Deadlines.FindAsync(id);
            if (deadline != null)
            {
                _context.Deadlines.Remove(deadline);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ToggleWorkflowStepAsync(int studentId, string stepKey, bool completed)
        {
            var student = await _context.Students
                .Include(s => s.WorkflowSteps)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null) return;

            var step = student.WorkflowSteps.FirstOrDefault(w => w.StepKey == stepKey);
            if (step == null)
            {
                step = new WorkflowStep { StudentId = studentId, StepKey = stepKey };
                student.WorkflowSteps.Add(step);
            }

            step.Completed = completed;
            step.CompletedDate = completed ? DateTime.Now : null;

            await _context.SaveChangesAsync();
        }
    }
}
