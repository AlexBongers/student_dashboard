using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Pages.Students;

public class EditModel : PageModel
{
    private readonly StudentService _studentService;

    public EditModel(StudentService studentService)
    {
        _studentService = studentService;
    }

    [BindProperty]
    public Student? Student { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Student = await _studentService.GetStudentByIdAsync(id);
        if (Student == null)
            return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid || Student == null)
            return Page();

        await _studentService.UpdateStudentAsync(Student);
        return RedirectToPage("/Students/Details", new { id = Student.Id });
    }
}
