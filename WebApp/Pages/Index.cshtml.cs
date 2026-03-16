using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Pages;

public class IndexModel : PageModel
{
    private readonly StudentService _studentService;

    public IndexModel(StudentService studentService)
    {
        _studentService = studentService;
    }

    public List<Student> Students { get; set; } = new();
    public string? SearchQuery { get; set; }

    public async Task OnGetAsync(string? q)
    {
        SearchQuery = q;
        var all = await _studentService.GetActiveStudentsAsync();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var lower = q.ToLower();
            all = all.Where(s =>
                s.FirstName.ToLower().Contains(lower) ||
                s.LastName.ToLower().Contains(lower) ||
                (s.StudentNumber?.ToLower().Contains(lower) ?? false) ||
                (s.Company?.ToLower().Contains(lower) ?? false) ||
                (s.Email?.ToLower().Contains(lower) ?? false)
            ).ToList();
        }

        Students = all;
    }
}
