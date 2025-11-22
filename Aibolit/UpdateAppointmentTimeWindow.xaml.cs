using System;
using System.Data;
using System.Windows;
using Npgsql;

namespace Aibolit
{
    public partial class UpdateAppointmentTimeWindow : Window
    {
        private DatabaseHelper dbHelper;

        public UpdateAppointmentTimeWindow(DatabaseHelper dbHelper)
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(DateTextBox.Text) ||
                    string.IsNullOrWhiteSpace(CurrentStartTimeTextBox.Text) ||
                    string.IsNullOrWhiteSpace(VetSurnameComboBox.Text) ||
                    string.IsNullOrWhiteSpace(VetNameComboBox.Text) ||
                    string.IsNullOrWhiteSpace(NewStartTimeTextBox.Text) ||
                    string.IsNullOrWhiteSpace(NewEndTimeTextBox.Text))
                {
                    MessageBox.Show("Заполните все поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var conn = dbHelper.GetConnection())
                {
                    conn.Open();
                    
                    var appDate = DateTime.Parse(DateTextBox.Text).Date;
                    var currentStartTime = TimeSpan.Parse(CurrentStartTimeTextBox.Text);
                    var newStartTime = TimeSpan.Parse(NewStartTimeTextBox.Text);
                    var newEndTime = TimeSpan.Parse(NewEndTimeTextBox.Text);
                    var vetSurname = VetSurnameComboBox.Text;
                    var vetName = VetNameComboBox.Text;
                    
                    try
                    {
                        using (var cmd = new NpgsqlCommand("CALL \"UpdateAppointmentTime\"(@app_date, @current_start_time, @new_start_time, @new_end_time, @vet_surname, @vet_name)", conn))
                        {
                            cmd.Parameters.AddWithValue("@app_date", appDate);
                            cmd.Parameters.AddWithValue("@current_start_time", currentStartTime);
                            cmd.Parameters.AddWithValue("@new_start_time", newStartTime);
                            cmd.Parameters.AddWithValue("@new_end_time", newEndTime);
                            cmd.Parameters.AddWithValue("@vet_surname", vetSurname);
                            cmd.Parameters.AddWithValue("@vet_name", vetName);
                            
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch (Npgsql.PostgresException ex) when (ex.SqlState == "42883")
                    {
                        using (var cmd = new NpgsqlCommand("CALL updateappointmenttime(@app_date, @current_start_time, @new_start_time, @new_end_time, @vet_surname, @vet_name)", conn))
                        {
                            cmd.Parameters.AddWithValue("@app_date", appDate);
                            cmd.Parameters.AddWithValue("@current_start_time", currentStartTime);
                            cmd.Parameters.AddWithValue("@new_start_time", newStartTime);
                            cmd.Parameters.AddWithValue("@new_end_time", newEndTime);
                            cmd.Parameters.AddWithValue("@vet_surname", vetSurname);
                            cmd.Parameters.AddWithValue("@vet_name", vetName);
                            
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show("Время записи успешно обновлено", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

