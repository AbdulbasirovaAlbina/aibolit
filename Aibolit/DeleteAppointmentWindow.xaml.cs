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
                var vetSurnames = dbHelper.ExecuteQuery("SELECT DISTINCT Surname FROM Veterinarian ORDER BY Surname");
                foreach (DataRow row in vetSurnames.Rows)
                {
                    VetSurnameComboBox.Items.Add(row["Surname"].ToString());
                }

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
                    
                    int? vetId = null;
                    using (var cmd = new NpgsqlCommand(
                        "SELECT ID_Veterinarian FROM Veterinarian WHERE Surname = @Surname AND Name = @Name", conn))
                    {
                        cmd.Parameters.AddWithValue("@Surname", vetSurname);
                        cmd.Parameters.AddWithValue("@Name", vetName);
                        var vetResult = cmd.ExecuteScalar();
                        if (vetResult != null && vetResult != DBNull.Value)
                        {
                            vetId = Convert.ToInt32(vetResult);
                        }
                    }
                    
                    if (vetId == null)
                    {
                        MessageBox.Show($"Врач с именем {vetName} {vetSurname} не найден",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    int? appointmentId = null;
                    using (var cmd = new NpgsqlCommand(
                        "SELECT ID_Appointment FROM Appointment WHERE Date = @Date " +
                        "AND Start_Time_Appointment = @Start_Time AND ID_Veterinarian = @ID_Veterinarian", conn))
                    {
                        cmd.Parameters.AddWithValue("@Date", appDate);
                        cmd.Parameters.AddWithValue("@Start_Time", startTime);
                        cmd.Parameters.AddWithValue("@ID_Veterinarian", vetId.Value);
                        var appointmentResult = cmd.ExecuteScalar();
                        if (appointmentResult != null && appointmentResult != DBNull.Value)
                        {
                            appointmentId = Convert.ToInt32(appointmentResult);
                        }
                    }
                    
                    if (appointmentId == null)
                    {
                        MessageBox.Show($"Запись на дату {appDate:yyyy-MM-dd}, время {startTime} и врача {vetName} {vetSurname} не найдена",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    using (var cmd = new NpgsqlCommand(
                        "DELETE FROM Appointment WHERE ID_Appointment = @ID_Appointment", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID_Appointment", appointmentId.Value);
                        cmd.ExecuteNonQuery();
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

