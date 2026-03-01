using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using StageManagementSystem.Models;
using StageManagementSystem.Services;

namespace StageManagementSystem.Views
{
    public partial class SyncWindow : Window
    {
        private readonly StudentService _studentService;

        public SyncWindow(StudentService studentService)
        {
            InitializeComponent();
            _studentService = studentService;
            this.Closed += SyncWindow_Closed;
            InitializeAsync();
        }

        private void SyncWindow_Closed(object? sender, EventArgs e)
        {
            try
            {
                Browser.Dispose();
            }
            catch { }
        }

        private async void InitializeAsync()
        {
            try
            {
                var env = await CoreWebView2Environment.CreateAsync(null, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "StageManagementWebView2"));
                await Browser.EnsureCoreWebView2Async(env);
                Browser.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Fout bij laden van browser: {ex.Message}";
                StatusText.Foreground = (System.Windows.Media.Brush)FindResource("DangerColor");
            }
        }

        private void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                StatusText.Text = $"Geladen. Log nu in of klik op Synchroniseer als u al ingelogd bent.";
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void Sync_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Bezig met synchroniseren...";
            StatusText.Foreground = (System.Windows.Media.Brush)FindResource("WarningColor");

            try
            {
                string jsToExecute = @"
                    (() => {
                        function getTables(doc) {
                            let res = [];
                            try {
                                doc.querySelectorAll('tr').forEach(tr => {
                                    let cols = [];
                                    tr.querySelectorAll('td, th').forEach(td => cols.push(td.innerText.trim()));
                                    if (cols.length > 0) res.push(cols);
                                });
                                // Als er geen tabellen zijn, probeer div kaarten te vinden
                                if (res.length === 0) {
                                    doc.querySelectorAll('.card, .list-item').forEach(card => {
                                        res.push([card.innerText.replace(/\n/g, ' | ')]);
                                    });
                                }
                                doc.querySelectorAll('iframe').forEach(ifr => {
                                    try { 
                                        let childRes = getTables(ifr.contentDocument); 
                                        res = res.concat(childRes);
                                    } catch(e) {}
                                });
                            } catch(e){}
                            return res;
                        }
                        return JSON.stringify(getTables(document));
                    })();
                ";

                string resultJson = await Browser.CoreWebView2.ExecuteScriptAsync(jsToExecute);
                var rawResult = JsonSerializer.Deserialize<string>(resultJson);
                var extractedRows = JsonSerializer.Deserialize<List<List<string>>>(rawResult);

                // Check if they are actually on the Mijn Studenten list based on what we expected
                if (extractedRows.Count <= 5 || extractedRows.Any(r => r.Count > 0 && r[0].Contains("Openstaande todo"))) 
                {
                    StatusText.Text = "Ga in het OnStage menu links naar 'Mijn studenten' (school icon) en klik daarna nogmaals op Synchroniseer.";
                    StatusText.Foreground = (System.Windows.Media.Brush)FindResource("WarningColor");
                    return;
                }

                var activeStudents = await _studentService.GetActiveStudentsAsync();
                int matchedCount = 0;
                int updatedCount = 0;

                int GetStatusRank(string status)
                {
                    return status switch
                    {
                        "Opstart" => 1,
                        "PvA" => 2,
                        "Concept 1" => 3,
                        "Concept 2" => 4,
                        "Afgerond" => 5,
                        _ => 0
                    };
                }

                foreach (var row in extractedRows)
                {
                    string joinedRowText = string.Join(" ", row).ToLower();

                    foreach (var student in activeStudents)
                    {
                        if (joinedRowText.Contains(student.LastName.ToLower()) || 
                            joinedRowText.Contains(student.FirstName.ToLower()) ||
                            (!string.IsNullOrEmpty(student.StudentNumber) && joinedRowText.Contains(student.StudentNumber.ToLower())))
                        {
                            matchedCount++;
                            
                            string newStatus = student.Status;

                            if (joinedRowText.Contains("goedkeuring pva") || joinedRowText.Contains("plan van aanpak"))
                                newStatus = "PvA";
                            else if (joinedRowText.Contains("eindbeoordeling") || joinedRowText.Contains("definitief") || joinedRowText.Contains("afgerond") || joinedRowText.Contains("geslaagd"))
                                newStatus = "Afgerond";
                            else if (joinedRowText.Contains("concept"))
                            {
                                if (joinedRowText.Contains("1")) newStatus = "Concept 1";
                                else if (joinedRowText.Contains("2")) newStatus = "Concept 2";
                                else newStatus = "Concept 1";
                            }
                            
                            // Only update if the new status is a progression forward from the current status
                            // This ensures we never overwrite manual updates or downgrade a student
                            if (GetStatusRank(newStatus) > GetStatusRank(student.Status))
                            {
                                student.Status = newStatus;
                                await _studentService.UpdateStudentAsync(student);
                                updatedCount++;
                            }
                            break; 
                        }
                    }
                }

                if (updatedCount > 0)
                {
                    StatusText.Text = $"Succesvol! {matchedCount} gevonden, {updatedCount} statussen opgewaardeerd. Venster sluit zo...";
                    StatusText.Foreground = (System.Windows.Media.Brush)FindResource("SuccessColor");
                    await Task.Delay(2000); // Give user time to read
                    DialogResult = true;
                }
                else if (matchedCount > 0)
                {
                    StatusText.Text = $"{matchedCount} studenten gevonden, maar geen statussen hoefden te worden geüpdatet (lokale data is up-to-date).";
                    StatusText.Foreground = (System.Windows.Media.Brush)FindResource("InfoColor");
                }
                else 
                {
                    StatusText.Text = "Geen overeenkomende actieve studenten gevonden op deze pagina.";
                    StatusText.Foreground = (System.Windows.Media.Brush)FindResource("WarningColor");
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Fout tijdens sync: {ex.Message}";
                StatusText.Foreground = (System.Windows.Media.Brush)FindResource("DangerColor");
            }
        }
    }
}
