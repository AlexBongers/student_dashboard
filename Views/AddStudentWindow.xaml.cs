using System.Windows;

namespace StageManagementSystem.Views
{
    public partial class AddStudentWindow : Window
    {
        public AddStudentWindow()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.AddStudentViewModel vm)
            {
                await vm.SaveCommand.ExecuteAsync(null);
                DialogResult = true;
                Close();
            }
        }
    }
}
