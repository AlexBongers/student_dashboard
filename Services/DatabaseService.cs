using Microsoft.EntityFrameworkCore;
using StageManagementSystem.Data;
using StageManagementSystem.Models;
using System;
using System.Linq;

namespace StageManagementSystem.Services
{
    public class DatabaseService
    {
        public void Initialize()
        {
            using var context = new AppDbContext();
            context.Database.EnsureCreated();

            // Patch existing database schema to include ProfilePicturePath if it's missing.
            try
            {
                using var command = context.Database.GetDbConnection().CreateCommand();
                command.CommandText = "ALTER TABLE Students ADD COLUMN ProfilePicturePath TEXT;";
                context.Database.OpenConnection();
                command.ExecuteNonQuery();
                context.Database.CloseConnection();
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 1) // 1 = SQLITE_ERROR, typically "duplicate column name"
            {
                // Column already exists, swallow exception.
            }

            if (!context.Students.Any())
            {
                SeedData(context);
            }
            
            // Auto-upgrade legacy lowercase statuses in the database
            UpgradeLegacyStatuses(context);
        }

        private void UpgradeLegacyStatuses(AppDbContext context)
        {
            var lowercaseKeys = new[] { "opstart", "pva", "concept1", "concept2", "eindversie", "definitief", "herkansing", "afgerond" };
            string GetUpgraded(string original) => original switch
            {
                "opstart" => "Opstart",
                "pva" => "PvA",
                "concept1" => "Concept 1",
                "concept2" => "Concept 2",
                "eindversie" => "Eindversie",
                "definitief" => "Definitief",
                "herkansing" => "Herkansing",
                "afgerond" => "Afgerond",
                _ => original
            };

            bool changed = false;
            foreach (var student in context.Students.ToList())
            {
                if (lowercaseKeys.Contains(student.Status))
                {
                    student.Status = GetUpgraded(student.Status);
                    changed = true;
                }
            }

            foreach (var step in context.WorkflowSteps.ToList())
            {
                if (lowercaseKeys.Contains(step.StepKey))
                {
                    step.StepKey = GetUpgraded(step.StepKey);
                    changed = true;
                }
            }

            if (changed)
            {
                context.SaveChanges();
            }
        }

        private void SeedData(AppDbContext context)
        {
            var students = new System.Collections.Generic.List<Student>
            {
                new Student 
                { 
                    FirstName = "Abderrahim", LastName = "Boutahiri", 
                    StudentNumber = "1766880", Company = "Achmea", 
                    Type = "scriptie", MyRole = "1e examinator", 
                    Status = "PvA", StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(5),
                    Notes = "Begeleider: Brechtje Veenstra (Achmea)\nSchoolbegeleider: Pascal Kwanten\nVoortgang: Uploaden Plan van Aanpak (poging 1)"
                },
                new Student 
                { 
                    FirstName = "Ibrahim", LastName = "Errahoui", 
                    StudentNumber = "1809615", Company = "RDW", 
                    Type = "scriptie", MyRole = "1e examinator", 
                    Status = "PvA", StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(5),
                    Notes = "Begeleider: Dielis IJlstra (RDW)\nSchoolbegeleider: Alex Bongers\nVoortgang: Uploaden Plan van Aanpak in progress (ingeleverd op 18-02-2026)"
                },
                new Student 
                { 
                    FirstName = "Ruben", LastName = "van Gend", 
                    StudentNumber = "1814639", Company = "De belastingdienst", 
                    Type = "scriptie", MyRole = "1e examinator", 
                    Status = "Definitief", StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(5), // Using 'definitief' for eindversie/end product
                    Notes = "Begeleider: Linda de Jong (De belastingdienst)\nSchoolbegeleider: Alex Bongers & Marco Gomes\nVoortgang: Bijna klaar, huidige fase: Uploaden & Beoordelen Eindproduct(en)"
                },
                new Student 
                { 
                    FirstName = "Job", LastName = "Huguenin", 
                    StudentNumber = "1848884", Company = "Veiligheidsregio Gelderland Zuid", 
                    Type = "stage", MyRole = "docentbegeleider", 
                    Status = "Opstart", StartDate = DateTime.Parse("2026-02-01"), EndDate = DateTime.Parse("2026-07-31"),
                    Notes = "Begeleider: Patrick van den Elzen (Veiligheidsregio Gelderland-Zuid)\nSchoolbegeleider: Alex Bongers\nVoortgang: Stageovereenkomst goedgekeurd, huidige fase: Uploaden Portfolio"
                },
                new Student 
                { 
                    FirstName = "Ridvan", LastName = "Kapici", 
                    StudentNumber = "1859994", Company = "Monta Services B.V.", 
                    Type = "stage", MyRole = "docentbegeleider", 
                    Status = "Concept 1", StartDate = DateTime.Parse("2026-02-01"), EndDate = DateTime.Parse("2026-07-31"),
                    Notes = "Begeleider: Teus van den Dool (T-Systems Nederland B.V.)\nSchoolbegeleider: Alex Bongers & Marco Gomes\nVoortgang: Plan van Aanpak goedgekeurd"
                },
                new Student 
                { 
                    FirstName = "Eray", LastName = "Karadağ", 
                    StudentNumber = "1814163", Company = "Medux B.V.", 
                    Type = "scriptie", MyRole = "1e examinator", 
                    Status = "Concept 1", StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(5),
                    Notes = "Begeleider: Hanno Bruggert (Rabobank)\nSchoolbegeleider: Alex Bongers & Marco Gomes\nVoortgang: Plan van Aanpak goedgekeurd"
                },
                new Student 
                { 
                    FirstName = "Pharrell", LastName = "van Kasteel", 
                    StudentNumber = "1804343", Company = "De belastingdienst", 
                    Type = "stage", MyRole = "docentbegeleider", 
                    Status = "PvA", StartDate = DateTime.Parse("2026-02-01"), EndDate = DateTime.Parse("2026-07-31"),
                    Notes = "Begeleider: Sander Beelen (Infinigate B.V.)\nSchoolbegeleider: Alex Bongers & Marco Gomes\nVoortgang: Plan van Aanpak ingediend (1 dag geleden)"
                },
                new Student 
                { 
                    FirstName = "Michel", LastName = "Schimpf", 
                    StudentNumber = "1759015", Company = "Lumiad", 
                    Type = "scriptie", MyRole = "1e examinator", 
                    Status = "Concept 2", StartDate = DateTime.Now, EndDate = DateTime.Now.AddMonths(5),
                    Notes = "Begeleider: Tom Bodewes (Lumiad)\nSchoolbegeleider: Alex Bongers\nVoortgang: Plan van Aanpak afgerond, huidige fase: Uploaden concept 2 scriptie/verslag"
                }
            };

            context.Students.AddRange(students);
            context.SaveChanges();

            // Auto-generate workflow steps based on Status
            var workflowKeys = new[] { "Opstart", "PvA", "Concept 1", "Concept 2", "Definitief", "Herkansing", "Afgerond" };
            
            foreach (var student in students)
            {
                // Find index of current status
                int statusIndex = Array.IndexOf(workflowKeys, student.Status);
                if (statusIndex >= 0)
                {
                    // Mark all steps up to and including current status as completed
                    for (int i = 0; i <= statusIndex; i++)
                    {
                         context.WorkflowSteps.Add(new WorkflowStep 
                         { 
                             StudentId = student.Id, 
                             StepKey = workflowKeys[i], 
                             Completed = true, 
                             CompletedDate = DateTime.Now // Approximate
                         });
                    }
                }
            }
            context.SaveChanges();
        }
    }
}
