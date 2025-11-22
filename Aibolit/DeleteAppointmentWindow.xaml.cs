using System;
using System.Data;
using System.Windows;
using Npgsql;

namespace Aibolit
{
    public partial class DeleteAppointmentWindow : Window
    {
        private DatabaseHelper dbHelper;

        public DeleteAppointmentWindow(DatabaseHelper dbHelper)
        {
            InitializeComponent();
            this.dbHelper = dbHelper;
            LoadVeterinarians();
        }

        private void LoadVeterinarians()
        {
            try
            {
                // Загрузка фамилий ветеринаров
                var vetSurnames = dbHelper.ExecuteQuery("SELECT DISTINCT Surname FROM Veterinarian ORDER BY Surname");
                foreach (DataRow row in vetSurnames.Rows)
                {
                    VetSurnameComboBox.Items.Add(row["Surname"].ToString());
                }

                // Загрузка имен ветеринаров
                var vetNames = dbHelper.ExecuteQuery("SELECT DISTINCT Name FROM Veterinarian ORDER BY Name");
                foreach (DataRow row in vetNames.Rows)
                {
                    VetNameComboBox.Items.Add(row["Name"].ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(DateTextBox.Text) ||
                    string.IsNullOrWhiteSpace(StartTimeTextBox.Text) ||
                    string.IsNullOrWhiteSpace(VetSurnameComboBox.Text) ||
                    string.IsNullOrWhiteSpace(VetNameComboBox.Text))
                {
                    MessageBox.Show("Заполните все поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show("Вы уверены, что хотите удалить эту запись?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                    return;

                using (var conn = dbHelper.GetConnection())
                {
                    conn.Open();
                    
                    var appDate = DateTime.Parse(DateTextBox.Text).Date;
                    var startTime = TimeSpan.Parse(StartTimeTextBox.Text);
                    var vetSurname = VetSurnameComboBox.Text;
                    var vetName = VetNameComboBox.Text;
                    
                    try
                    {
                        using (var cmd = new NpgsqlCommand("CALL \"DeleteAppointmentTime\"(@app_date, @start_time, @vet_surname, @vet_name)", conn))
                        {
                            cmd.Parameters.AddWithValue("@app_date", appDate);
                            cmd.Parameters.AddWithValue("@start_time", startTime);
                            cmd.Parameters.AddWithValue("@vet_surname", vetSurname);
                            cmd.Parameters.AddWithValue("@vet_name", vetName);
                            
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch (Npgsql.PostgresException ex) when (ex.SqlState == "42883")
                    {
                        using (var cmd = new NpgsqlCommand("CALL deleteappointmenttime(@app_date, @start_time, @vet_surname, @vet_name)", conn))
                        {
                            cmd.Parameters.AddWithValue("@app_date", appDate);
                            cmd.Parameters.AddWithValue("@start_time", startTime);
                            cmd.Parameters.AddWithValue("@vet_surname", vetSurname);
                            cmd.Parameters.AddWithValue("@vet_name", vetName);
                            
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show("Запись успешно удалена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

