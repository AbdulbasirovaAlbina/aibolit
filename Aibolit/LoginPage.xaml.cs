using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Aibolit
{
    public partial class LoginPage : Page
    {
        private DatabaseHelper dbHelper;

        public LoginPage()
        {
            InitializeComponent();
            dbHelper = new DatabaseHelper();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Пожалуйста, введите имя пользователя и пароль");
                return;
            }

            try
            {
                if (dbHelper.AuthenticateUser(username, password))
                {
                    NavigationService?.Navigate(new MainPage());
                }
                else
                {
                    ShowError("Неверное имя пользователя или пароль");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка подключения к базе данных: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }
    }
}


