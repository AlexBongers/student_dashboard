using System.Windows;
using System.Windows.Controls;

namespace StageManagementSystem.Views
{
    public partial class InfoBox : UserControl
    {
        public InfoBox()
        {
            InitializeComponent();
        }

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(InfoBox), new PropertyMetadata("", (d, e) => 
            {
                ((InfoBox)d).LabelText.Text = ((string)e.NewValue)?.ToUpper();
            }));

        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(InfoBox), new PropertyMetadata("", (d, e) => 
            {
                ((InfoBox)d).ValueText.Text = (string)e.NewValue;
            }));
    }
}
