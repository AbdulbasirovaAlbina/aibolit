using System;
using System.Data;
using System.Linq;
using System.Windows;
using Npgsql;

namespace Aibolit
{
    public partial class AddAppointmentWindow : Window
    {
        private DatabaseHelper dbHelper;

        public AddAppointmentWindow(DatabaseHelper dbHelper)
        {
            InitializeComponent();
            this.dbHelper = dbHelper;
            LoadComboBoxes();
        }

        private void LoadComboBoxes()
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

                // Загрузка услуг
                var services = dbHelper.ExecuteQuery("SELECT Name FROM Service ORDER BY Name");
                foreach (DataRow row in services.Rows)
                {
                    ServiceComboBox.Items.Add(row["Name"].ToString());
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
                if (string.IsNullOrWhiteSpace(VetSurnameComboBox.Text) ||
                    string.IsNullOrWhiteSpace(VetNameComboBox.Text) ||
                    string.IsNullOrWhiteSpace(ServiceComboBox.Text) ||
                    string.IsNullOrWhiteSpace(DateTextBox.Text) ||
                    string.IsNullOrWhiteSpace(StartTimeTextBox.Text) ||
                    string.IsNullOrWhiteSpace(EndTimeTextBox.Text))
                {
                    MessageBox.Show("Заполните все поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var conn = dbHelper.GetConnection())
                {
                    conn.Open();
                    
                    var vetSurname = VetSurnameComboBox.Text;
                    var vetName = VetNameComboBox.Text;
                    var serviceName = ServiceComboBox.Text;
                    var appDate = DateTime.Parse(DateTextBox.Text).Date;
                    var startTime = TimeSpan.Parse(StartTimeTextBox.Text);
                    var endTime = TimeSpan.Parse(EndTimeTextBox.Text);
                    
                    try
                    {
                        using (var cmd = new NpgsqlCommand("CALL \"AddAppointment\"(@vet_surname, @vet_name, @service_name, @app_date, @start_time, @end_time)", conn))
                        {
                            cmd.Parameters.AddWithValue("@vet_surname", vetSurname);
                            cmd.Parameters.AddWithValue("@vet_name", vetName);
                            cmd.Parameters.AddWithValue("@service_name", serviceName);
                            cmd.Parameters.AddWithValue("@app_date", appDate);
                            cmd.Parameters.AddWithValue("@start_time", startTime);
                            cmd.Parameters.AddWithValue("@end_time", endTime);
                            
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch (Npgsql.PostgresException ex) when (ex.SqlState == "42883")
                    {
                        using (var cmd = new NpgsqlCommand("CALL addappointment(@vet_surname, @vet_name, @service_name, @app_date, @start_time, @end_time)", conn))
                        {
                            cmd.Parameters.AddWithValue("@vet_surname", vetSurname);
                            cmd.Parameters.AddWithValue("@vet_name", vetName);
                            cmd.Parameters.AddWithValue("@service_name", serviceName);
                            cmd.Parameters.AddWithValue("@app_date", appDate);
                            cmd.Parameters.AddWithValue("@start_time", startTime);
                            cmd.Parameters.AddWithValue("@end_time", endTime);
                            
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show("Запись успешно добавлена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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

