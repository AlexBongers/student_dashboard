using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace StageManagementSystem.Helpers
{
    public class RichTextBoxHelper : DependencyObject
    {
        public static readonly DependencyProperty RtfProperty =
            DependencyProperty.RegisterAttached(
                "Rtf",
                typeof(string),
                typeof(RichTextBoxHelper),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRtfChanged));

        public static string GetRtf(DependencyObject obj)
        {
            return (string)obj.GetValue(RtfProperty);
        }

        public static void SetRtf(DependencyObject obj, string value)
        {
            obj.SetValue(RtfProperty, value);
        }

        private static void OnRtfChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RichTextBox rtb)
            {
                // Prevent infinite loop
                rtb.TextChanged -= Rtb_TextChanged;

                string? rtf = e.NewValue as string;
                if (string.IsNullOrEmpty(rtf))
                {
                    rtb.Document.Blocks.Clear();
                }
                else
                {
                    try
                    {
                        var tr = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
                        using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(rtf)))
                        {
                            tr.Load(ms, DataFormats.Rtf);
                        }
                    }
                    catch
                    {
                        // Fallback to plain text if not valid RTF
                        rtb.Document.Blocks.Clear();
                        rtb.Document.Blocks.Add(new Paragraph(new Run(rtf)));
                    }
                }

                rtb.TextChanged += Rtb_TextChanged;
            }
        }

        private static void Rtb_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is RichTextBox rtb)
            {
                var tr = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
                using (var ms = new MemoryStream())
                {
                    tr.Save(ms, DataFormats.Rtf);
                    string rtf = Encoding.UTF8.GetString(ms.ToArray());
                    SetRtf(rtb, rtf);
                }
            }
        }
    }
}
