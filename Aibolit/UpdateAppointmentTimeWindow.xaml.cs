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
                    
                    if (newStartTime >= newEndTime)
                    {
                        MessageBox.Show("Время окончания должно быть больше времени начала",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
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
                        "AND ID_Veterinarian = @ID_Veterinarian AND Start_Time_Appointment = @Start_Time", conn))
                    {
                        cmd.Parameters.AddWithValue("@Date", appDate);
                        cmd.Parameters.AddWithValue("@ID_Veterinarian", vetId.Value);
                        cmd.Parameters.AddWithValue("@Start_Time", currentStartTime);
                        var appointmentResult = cmd.ExecuteScalar();
                        if (appointmentResult != null && appointmentResult != DBNull.Value)
                        {
                            appointmentId = Convert.ToInt32(appointmentResult);
                        }
                    }
                    
                    if (appointmentId == null)
                    {
                        MessageBox.Show($"Запись на дату {appDate:yyyy-MM-dd}, врача {vetName} {vetSurname} и текущее время начала {currentStartTime} не найдена",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    bool timeConflict = false;
                    using (var cmd = new NpgsqlCommand(
                        "SELECT EXISTS(SELECT 1 FROM Appointment WHERE ID_Veterinarian = @ID_Veterinarian " +
                        "AND Date = @Date AND ID_Appointment != @ID_Appointment " +
                        "AND ((Start_Time_Appointment < @End_Time AND Start_Time_Appointment >= @Start_Time) " +
                        "OR (End_Time_Appointment > @Start_Time AND End_Time_Appointment <= @End_Time)))", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID_Veterinarian", vetId.Value);
                        cmd.Parameters.AddWithValue("@Date", appDate);
                        cmd.Parameters.AddWithValue("@ID_Appointment", appointmentId.Value);
                        cmd.Parameters.AddWithValue("@Start_Time", newStartTime);
                        cmd.Parameters.AddWithValue("@End_Time", newEndTime);
                        timeConflict = Convert.ToBoolean(cmd.ExecuteScalar());
                    }
                    
                    if (timeConflict)
                    {
                        MessageBox.Show("Время для выбранного ветеринара уже занято, выберите другое время",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    using (var cmd = new NpgsqlCommand(
                        "UPDATE Appointment SET Start_Time_Appointment = @New_Start_Time, " +
                        "End_Time_Appointment = @New_End_Time WHERE ID_Appointment = @ID_Appointment", conn))
                    {
                        cmd.Parameters.AddWithValue("@New_Start_Time", newStartTime);
                        cmd.Parameters.AddWithValue("@New_End_Time", newEndTime);
                        cmd.Parameters.AddWithValue("@ID_Appointment", appointmentId.Value);
                        
                        cmd.ExecuteNonQuery();
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

