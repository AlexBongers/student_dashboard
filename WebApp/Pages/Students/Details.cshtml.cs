using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Pages.Students;

public class DetailsModel : PageModel
{
    private readonly StudentService _studentService;

    public DetailsModel(StudentService studentService)
    {
        _studentService = studentService;
    }

    public Student? Student { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Student = await _studentService.GetStudentByIdAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostArchiveAsync(int id)
    {
        await _studentService.ArchiveStudentAsync(id);
        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await _studentService.DeleteStudentAsync(id);
        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostToggleWorkflowAsync(int studentId, string stepKey, bool completed)
    {
        await _studentService.ToggleWorkflowStepAsync(studentId, stepKey, completed);
        return RedirectToPage(new { id = studentId });
    }

    public async Task<IActionResult> OnPostSaveNotesAsync(int studentId, string? notes)
    {
        await _studentService.UpdateNotesAsync(studentId, notes);
        return RedirectToPage(new { id = studentId });
    }

    public async Task<IActionResult> OnPostAddDeadlineAsync(int studentId, string title, DateTime dueDate)
    {
        var deadline = new Deadline
        {
            StudentId = studentId,
            Title = title,
            DueDate = dueDate,
            IsCompleted = false
        };
        await _studentService.AddDeadlineAsync(deadline);
        return RedirectToPage(new { id = studentId });
    }

    public async Task<IActionResult> OnPostToggleDeadlineAsync(int deadlineId, int studentId, bool completed)
    {
        await _studentService.ToggleDeadlineAsync(deadlineId, completed);
        return RedirectToPage(new { id = studentId });
    }

    public async Task<IActionResult> OnPostDeleteDeadlineAsync(int deadlineId, int studentId)
    {
        await _studentService.DeleteDeadlineAsync(deadlineId);
        return RedirectToPage(new { id = studentId });
    }

    public async Task<IActionResult> OnPostAddContactAsync(int studentId, DateTime date, string type, string content)
    {
        var contact = new Contact
        {
            StudentId = studentId,
            Date = date,
            Type = type,
            Content = content
        };
        await _studentService.AddContactAsync(contact);
        return RedirectToPage(new { id = studentId });
    }

    public async Task<IActionResult> OnPostDeleteContactAsync(int contactId, int studentId)
    {
        await _studentService.DeleteContactAsync(contactId);
        return RedirectToPage(new { id = studentId });
    }
}
