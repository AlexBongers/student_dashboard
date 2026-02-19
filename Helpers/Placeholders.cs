using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StageManagementSystem.Helpers
{
    public static class Placeholders
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.RegisterAttached("Text", typeof(string), typeof(Placeholders), new PropertyMetadata(default(string), OnTextChanged));

        public static void SetText(UIElement element, string value)
        {
            element.SetValue(TextProperty, value);
        }

        public static string GetText(UIElement element)
        {
            return (string)element.GetValue(TextProperty);
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                textBox.Loaded += TextBox_Loaded;
                textBox.TextChanged += TextBox_TextChanged;
            }
        }

        private static void TextBox_Loaded(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            UpdatePlaceholder(textBox);
        }

        private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            UpdatePlaceholder(textBox);
        }

        private static void UpdatePlaceholder(TextBox textBox)
        {
            if (string.IsNullOrEmpty(textBox.Text))
            {
                var placeholderText = GetText(textBox);
                if (!string.IsNullOrEmpty(placeholderText))
                {
                    var visualBrush = new VisualBrush
                    {
                        Stretch = Stretch.None,
                        AlignmentX = AlignmentX.Left,
                        AlignmentY = AlignmentY.Center,
                        Visual = new TextBlock
                        {
                            Text = placeholderText,
                            Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)), // Gray600
                            Padding = new Thickness(4, 0, 0, 0),
                            FontStyle = FontStyles.Normal,
                            FontSize = textBox.FontSize,
                            FontFamily = textBox.FontFamily
                        }
                    };
                    textBox.Background = visualBrush;
                }
            }
            else
            {
                textBox.Background = Brushes.Transparent; 
            }
        }
    }
}
