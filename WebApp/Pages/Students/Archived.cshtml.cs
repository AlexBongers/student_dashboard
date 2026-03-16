using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Pages.Students;

public class ArchivedModel : PageModel
{
    private readonly StudentService _studentService;

    public ArchivedModel(StudentService studentService)
    {
        _studentService = studentService;
    }

    public List<Student> Students { get; set; } = new();

    public async Task OnGetAsync()
    {
        Students = await _studentService.GetArchivedStudentsAsync();
    }

    public async Task<IActionResult> OnPostUnarchiveAsync(int id)
    {
        await _studentService.UnarchiveStudentAsync(id);
        return RedirectToPage();
    }
}
