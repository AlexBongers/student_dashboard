using Microsoft.AspNetCore.Mvc;
using WebDashboard.Models;
using WebDashboard.Services;

namespace WebDashboard.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentsController : ControllerBase
    {
        private readonly StudentService _studentService;

        public StudentsController(StudentService studentService)
        {
            _studentService = studentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool archived = false)
        {
            var students = archived
                ? await _studentService.GetArchivedStudentsAsync()
                : await _studentService.GetActiveStudentsAsync();

            return Ok(students.Select(s => MapToDto(s)));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null) return NotFound();
            return Ok(MapToDto(student));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StudentCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var student = new Student
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Phone = dto.Phone,
                StudentNumber = dto.StudentNumber,
                Type = dto.Type,
                MyRole = dto.MyRole,
                Company = dto.Company,
                Location = dto.Location,
                Address = dto.Address,
                StudyProgram = dto.StudyProgram,
                Cohort = dto.Cohort,
                CompanyAddress = dto.CompanyAddress,
                CompanySupervisorName = dto.CompanySupervisorName,
                CompanySupervisorEmail = dto.CompanySupervisorEmail,
                CompanySupervisorPhone = dto.CompanySupervisorPhone,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = dto.Status ?? "Opstart",
                Notes = dto.Notes,
                CreatedAt = DateTime.Now
            };

            await _studentService.AddStudentAsync(student);
            return CreatedAtAction(nameof(GetById), new { id = student.Id }, MapToDto(student));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] StudentCreateDto dto)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null) return NotFound();

            student.FirstName = dto.FirstName;
            student.LastName = dto.LastName;
            student.Email = dto.Email;
            student.Phone = dto.Phone;
            student.StudentNumber = dto.StudentNumber;
            student.Type = dto.Type;
            student.MyRole = dto.MyRole;
            student.Company = dto.Company;
            student.Location = dto.Location;
            student.Address = dto.Address;
            student.StudyProgram = dto.StudyProgram;
            student.Cohort = dto.Cohort;
            student.CompanyAddress = dto.CompanyAddress;
            student.CompanySupervisorName = dto.CompanySupervisorName;
            student.CompanySupervisorEmail = dto.CompanySupervisorEmail;
            student.CompanySupervisorPhone = dto.CompanySupervisorPhone;
            student.StartDate = dto.StartDate;
            student.EndDate = dto.EndDate;
            student.Status = dto.Status ?? student.Status;
            student.Notes = dto.Notes;

            await _studentService.UpdateStudentAsync(student);
            return Ok(MapToDto(student));
        }

        [HttpPost("{id}/archive")]
        public async Task<IActionResult> Archive(int id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null) return NotFound();

            student.Archived = !student.Archived;
            student.ArchivedAt = student.Archived ? DateTime.Now : null;
            if (student.Archived) student.Status = "Afgerond";

            await _studentService.UpdateStudentAsync(student);
            return Ok(MapToDto(student));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null) return NotFound();
            await _studentService.DeleteStudentAsync(id);
            return NoContent();
        }

        [HttpPost("{id}/contacts")]
        public async Task<IActionResult> AddContact(int id, [FromBody] ContactCreateDto dto)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null) return NotFound();

            var contact = new Contact
            {
                StudentId = id,
                Type = dto.Type,
                Content = dto.Content,
                Date = dto.Date
            };

            await _studentService.AddContactAsync(contact);
            return Ok(new { contact.Id, contact.StudentId, contact.Type, contact.Content, Date = contact.Date });
        }

        [HttpDelete("{studentId}/contacts/{contactId}")]
        public async Task<IActionResult> DeleteContact(int studentId, int contactId)
        {
            await _studentService.DeleteContactAsync(contactId);
            return NoContent();
        }

        [HttpPost("{id}/deadlines")]
        public async Task<IActionResult> AddDeadline(int id, [FromBody] DeadlineCreateDto dto)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null) return NotFound();

            var deadline = new Deadline
            {
                StudentId = id,
                Title = dto.Title,
                DueDate = dto.DueDate,
                IsCompleted = false
            };

            await _studentService.AddDeadlineAsync(deadline);
            return Ok(new { deadline.Id, deadline.StudentId, deadline.Title, deadline.DueDate, deadline.IsCompleted });
        }

        [HttpPut("{studentId}/deadlines/{deadlineId}")]
        public async Task<IActionResult> UpdateDeadline(int studentId, int deadlineId, [FromBody] DeadlineUpdateDto dto)
        {
            var student = await _studentService.GetStudentByIdAsync(studentId);
            if (student == null) return NotFound();

            var deadline = student.Deadlines.FirstOrDefault(d => d.Id == deadlineId);
            if (deadline == null) return NotFound();

            deadline.IsCompleted = dto.IsCompleted;
            await _studentService.UpdateDeadlineAsync(deadline);
            return Ok(new { deadline.Id, deadline.StudentId, deadline.Title, deadline.DueDate, deadline.IsCompleted });
        }

        [HttpDelete("{studentId}/deadlines/{deadlineId}")]
        public async Task<IActionResult> DeleteDeadline(int studentId, int deadlineId)
        {
            await _studentService.DeleteDeadlineAsync(deadlineId);
            return NoContent();
        }

        [HttpPost("{id}/workflow")]
        public async Task<IActionResult> UpdateWorkflow(int id, [FromBody] WorkflowUpdateDto dto)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null) return NotFound();

            var workflowKeys = new[] { "Opstart", "PvA", "Concept 1", "Concept 2", "Definitief", "Herkansing", "Afgerond" };
            int targetIndex = Array.IndexOf(workflowKeys, dto.Status);

            if (targetIndex >= 0)
            {
                for (int i = 0; i < workflowKeys.Length; i++)
                {
                    await _studentService.ToggleWorkflowStepAsync(id, workflowKeys[i], i <= targetIndex);
                }
                student.Status = dto.Status;
                await _studentService.UpdateStudentAsync(student);
            }

            var updated = await _studentService.GetStudentByIdAsync(id);
            return Ok(MapToDto(updated!));
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var students = await _studentService.GetActiveStudentsAsync();
            var archived = await _studentService.GetArchivedStudentsAsync();

            int needsAction = 0;
            foreach (var s in students)
            {
                var lastContact = s.Contacts.OrderByDescending(c => c.Date).FirstOrDefault();
                if (lastContact == null || (DateTime.Now - lastContact.Date).TotalDays > 14) needsAction++;
            }

            int inReview = students.Count(s =>
                s.Status == "Concept 1" || s.Status == "Concept 2" ||
                s.Status == "Definitief");

            var now = DateTime.Now;
            int completedMonth = archived.Count(s =>
                s.ArchivedAt.HasValue &&
                s.ArchivedAt.Value.Month == now.Month &&
                s.ArchivedAt.Value.Year == now.Year);

            var allStudents = students.Concat(archived).ToList();
            var statusDistribution = allStudents
                .GroupBy(s => s.Status)
                .Select(g => new { label = g.Key, count = g.Count(), percentage = allStudents.Count > 0 ? (double)g.Count() / allStudents.Count : 0 })
                .OrderByDescending(x => x.count)
                .ToList();

            var alerts = new List<object>();
            foreach (var s in students)
            {
                var urgentDeadline = s.Deadlines?.FirstOrDefault(d => !d.IsCompleted && (d.DueDate.Date - DateTime.Today).TotalDays <= 7);
                if (urgentDeadline != null)
                {
                    int daysLeft = (int)(urgentDeadline.DueDate.Date - DateTime.Today).TotalDays;
                    string timeText = daysLeft < 0 ? $"({Math.Abs(daysLeft)} dagen te laat)" : $"({daysLeft} dagen)";
                    alerts.Add(new { message = $"Dringende Deadline: {s.Name}", description = $"{urgentDeadline.Title} {timeText}", color = "danger", studentId = s.Id });
                }

                var lastContact = s.Contacts?.OrderByDescending(c => c.Date).FirstOrDefault();
                double daysSinceContact = lastContact != null ? (DateTime.Now - lastContact.Date).TotalDays : (DateTime.Now - s.CreatedAt).TotalDays;
                if (s.Status == "Opstart" && daysSinceContact > 14)
                {
                    alerts.Add(new { message = $"Actie Vereist: {s.Name}", description = $"Staat al {(int)daysSinceContact} dagen op 'Opstart' zonder contact.", color = "warning", studentId = s.Id });
                }

                if (s.Status.StartsWith("Concept") || s.Status == "Definitief")
                {
                    alerts.Add(new { message = $"In Review: {s.Name}", description = $"Wacht op beoordeling voor '{s.Status}'", color = "info", studentId = s.Id });
                }
            }

            return Ok(new
            {
                activeCount = students.Count,
                needsActionCount = needsAction,
                inReviewCount = inReview,
                completedMonthCount = completedMonth,
                statusDistribution,
                alerts
            });
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export()
        {
            var students = await _studentService.GetActiveStudentsAsync();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Voornaam,Achternaam,Studentnummer,Bedrijf,Type,Rol,Status,Email,StartDatum,EindDatum");

            string SafeCsv(string? input)
            {
                if (string.IsNullOrEmpty(input)) return "";
                if (input.Contains(",") || input.Contains("\"") || input.Contains("\n"))
                    return $"\"{input.Replace("\"", "\"\"")}\"";
                return input;
            }

            foreach (var s in students)
            {
                sb.AppendLine($"{SafeCsv(s.FirstName)},{SafeCsv(s.LastName)},{SafeCsv(s.StudentNumber)},{SafeCsv(s.Company)},{SafeCsv(s.Type)},{SafeCsv(s.MyRole)},{SafeCsv(s.Status)},{SafeCsv(s.Email)},{s.StartDate:yyyy-MM-dd},{s.EndDate:yyyy-MM-dd}");
            }

            return File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"StudentenExport_{DateTime.Now:yyyyMMdd}.csv");
        }

        private static object MapToDto(Student s) => new
        {
            s.Id,
            s.FirstName,
            s.LastName,
            s.Name,
            s.Email,
            s.Phone,
            s.StudentNumber,
            s.Type,
            s.MyRole,
            s.Company,
            s.Location,
            s.Address,
            s.StudyProgram,
            s.Cohort,
            s.CompanyAddress,
            s.CompanySupervisorName,
            s.CompanySupervisorEmail,
            s.CompanySupervisorPhone,
            s.StartDate,
            s.EndDate,
            s.Status,
            s.Notes,
            s.Archived,
            s.ArchivedAt,
            s.HasUrgentDeadline,
            s.CreatedAt,
            Contacts = s.Contacts.OrderByDescending(c => c.Date).Select(c => new
            {
                c.Id, c.StudentId, c.Type, c.Content, c.Date
            }),
            WorkflowSteps = s.WorkflowSteps.Select(w => new
            {
                w.Id, w.StudentId, w.StepKey, w.Completed, w.CompletedDate
            }),
            Deadlines = s.Deadlines.OrderBy(d => d.DueDate).Select(d => new
            {
                d.Id, d.StudentId, d.Title, d.DueDate, d.IsCompleted
            }),
            Attachments = s.Attachments.OrderByDescending(a => a.UploadDate).Select(a => new
            {
                a.Id, a.StudentId, a.FileName, a.UploadDate
            })
        };
    }

    public record StudentCreateDto(
        string FirstName,
        string LastName,
        string? Email,
        string? Phone,
        string? StudentNumber,
        string Type,
        string MyRole,
        string Company,
        string? Location,
        string? Address,
        string? StudyProgram,
        string? Cohort,
        string? CompanyAddress,
        string? CompanySupervisorName,
        string? CompanySupervisorEmail,
        string? CompanySupervisorPhone,
        DateTime StartDate,
        DateTime EndDate,
        string? Status,
        string? Notes
    );

    public record ContactCreateDto(string Type, string Content, DateTime Date);
    public record DeadlineCreateDto(string Title, DateTime DueDate);
    public record DeadlineUpdateDto(bool IsCompleted);
    public record WorkflowUpdateDto(string Status);
}
