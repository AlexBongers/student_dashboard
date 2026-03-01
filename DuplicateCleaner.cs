using System.Linq;
using Microsoft.EntityFrameworkCore;
using StageManagementSystem.Data;

namespace StageManagementSystem
{
    public static class DuplicateCleaner
    {
        public static void CleanDuplicates()
        {
            using var context = new AppDbContext();
            
            // Group by First Name, Last Name, and Student Number to find duplicates
            var groupedStudents = context.Students
                .GroupBy(s => new { s.FirstName, s.LastName, s.StudentNumber })
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in groupedStudents)
            {
                // We keep the student that was created first (smallest ID)
                // and delete all others in that exact group
                var studentsToKeep = group.OrderBy(s => s.Id).First();
                var studentsToDelete = group.Where(s => s.Id != studentsToKeep.Id).ToList();

                context.Students.RemoveRange(studentsToDelete);
            }

            context.SaveChanges();
        }
    }
}
