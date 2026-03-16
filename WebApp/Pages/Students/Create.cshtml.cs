using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Pages.Students;

public class CreateModel : PageModel
{
    private readonly StudentService _studentService;

    public CreateModel(StudentService studentService)
    {
        _studentService = studentService;
    }

    [BindProperty]
    public Student Student { get; set; } = new Student
    {
        StartDate = DateTime.Today,
        EndDate = DateTime.Today.AddMonths(5)
    };

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        Student.CreatedAt = DateTime.UtcNow;
        await _studentService.AddStudentAsync(Student);
        return RedirectToPage("/Students/Details", new { id = Student.Id });
    }
}
