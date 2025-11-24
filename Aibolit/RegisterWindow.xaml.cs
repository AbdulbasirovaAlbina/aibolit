using System.Windows;

namespace Aibolit
{
    public partial class RegisterWindow : Window
    {
        private readonly DatabaseHelper dbHelper;

        public string CreatedUsername { get; private set; } = string.Empty;

        public RegisterWindow(DatabaseHelper databaseHelper)
        {
            InitializeComponent();
            dbHelper = databaseHelper;
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Введите имя пользователя и пароль");
                return;
            }

            if (password.Length < 4)
            {
                ShowError("Пароль должен содержать не менее 4 символов");
                return;
            }

            if (password != confirmPassword)
            {
                ShowError("Пароли не совпадают");
                return;
            }

            if (dbHelper.RegisterUser(username, password, out string errorMessage))
            {
                CreatedUsername = username;
                DialogResult = true;
                Close();
            }
            else
            {
                ShowError(errorMessage);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }
    }
}

