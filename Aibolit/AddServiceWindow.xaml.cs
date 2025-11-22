using System;
using System.Windows;
using Npgsql;

namespace Aibolit
{
    public partial class AddServiceWindow : Window
    {
        private readonly DatabaseHelper dbHelper;

        public AddServiceWindow(DatabaseHelper dbHelper)
        {
            InitializeComponent();
            this.dbHelper = dbHelper;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ||
                    string.IsNullOrWhiteSpace(CostTextBox.Text))
                {
                    MessageBox.Show("Заполните все поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(CostTextBox.Text, out decimal cost) || cost < 0)
                {
                    MessageBox.Show("Введите корректную стоимость (положительное число)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var conn = dbHelper.GetConnection())
                {
                    conn.Open();
                    
                    using (var cmd = new NpgsqlCommand(
                        "INSERT INTO Service (Name, Description, Cost) " +
                        "VALUES (@Name, @Description, @Cost)", conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", NameTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@Description", DescriptionTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@Cost", cost);
                        
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Услуга успешно добавлена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

