using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using Npgsql;

namespace Aibolit
{
    public partial class EditAppointmentWindow : Window
    {
        private readonly DatabaseHelper dbHelper;
        private readonly DataRow dataRow;
        private int appointmentId;
        private List<PatientItem> patients = new List<PatientItem>();
        private int? currentPatientId;

        private class PatientItem
        {
            public int Id { get; set; }
            public string Display { get; set; } = string.Empty;
        }

        public EditAppointmentWindow(DatabaseHelper dbHelper, DataRow row)
        {
            InitializeComponent();
            this.dbHelper = dbHelper;
            this.dataRow = row;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                appointmentId = Convert.ToInt32(dataRow["ID_Appointment"]);
                
                // Устанавливаем дату
                if (dataRow["Дата"] != DBNull.Value && DateTime.TryParse(dataRow["Дата"].ToString(), out DateTime date))
                {
                    DatePicker.SelectedDate = date;
                }
                
                // Формируем полное ФИО ветеринара
                string vetSurname = dataRow["Фамилия_Ветеринара"]?.ToString() ?? "";
                string vetName = dataRow["Имя_Ветеринара"]?.ToString() ?? "";
                
                // Получаем отчество из базы
                string vetMiddleName = "";
                using (var conn = dbHelper.GetConnection())
                {
                    conn.Open();
                    using (var petCmd = new NpgsqlCommand(
                        "SELECT ID_Pet FROM Appointment WHERE ID_Appointment = @ID", conn))
                    {
                        petCmd.Parameters.AddWithValue("@ID", appointmentId);
                        var petResult = petCmd.ExecuteScalar();
                        if (petResult != null && petResult != DBNull.Value)
                        {
                            currentPatientId = Convert.ToInt32(petResult);
                        }
                    }

                    using (var cmd = new NpgsqlCommand(
                        "SELECT Middle_Name FROM Veterinarian WHERE Surname = @Surname AND Name = @Name LIMIT 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@Surname", vetSurname);
                        cmd.Parameters.AddWithValue("@Name", vetName);
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            vetMiddleName = result.ToString();
                        }
                    }
                }
                
                string fullName = $"{vetSurname} {vetName} {vetMiddleName}".Trim();
                VeterinarianComboBox.Text = fullName;
                
                ServiceComboBox.Text = dataRow["Услуга"]?.ToString() ?? "";
                
                if (dataRow["Время_Начала"] != DBNull.Value)
                {
                    if (TimeSpan.TryParse(dataRow["Время_Начала"].ToString(), out TimeSpan startTime))
                    {
                        StartTimeTextBox.Text = startTime.ToString(@"hh\:mm");
                    }
                }
                
                if (dataRow["Время_Окончания"] != DBNull.Value)
                {
                    if (TimeSpan.TryParse(dataRow["Время_Окончания"].ToString(), out TimeSpan endTime))
                    {
                        EndTimeTextBox.Text = endTime.ToString(@"hh\:mm");
                    }
                }

                // Загружаем списки для ComboBox
                var veterinarians = dbHelper.ExecuteQuery(
                    "SELECT ID_Veterinarian, Surname || ' ' || Name || ' ' || COALESCE(Middle_Name, '') AS FullName " +
                    "FROM Veterinarian ORDER BY Surname, Name");
                foreach (DataRow row in veterinarians.Rows)
                {
                    VeterinarianComboBox.Items.Add(row["FullName"].ToString().Trim());
                }

                var services = dbHelper.ExecuteQuery("SELECT Name FROM Service ORDER BY Name");
                foreach (DataRow row in services.Rows)
                {
                    ServiceComboBox.Items.Add(row["Name"].ToString());
                }

                var patientsTable = dbHelper.ExecuteQuery(@"
                    SELECT 
                        p.ID_Pet,
                        p.Name AS PetName,
                        p.View,
                        p.Species,
                        o.Surname AS OwnerSurname,
                        o.Name AS OwnerName
                    FROM Patient p
                    JOIN Owner o ON p.ID_Owner = o.ID_Owner
                    ORDER BY o.Surname, o.Name, p.Name");

                patients = patientsTable.AsEnumerable()
                    .Select(row => new PatientItem
                    {
                        Id = Convert.ToInt32(row["ID_Pet"]),
                        Display = $"{row["PetName"]} ({row["View"]}, {row["Species"]}) — {row["OwnerSurname"]} {row["OwnerName"]}"
                    })
                    .ToList();

                PatientComboBox.ItemsSource = patients;
                if (currentPatientId.HasValue)
                {
                    PatientComboBox.SelectedValue = currentPatientId.Value;
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
                if (DatePicker.SelectedDate == null ||
                    string.IsNullOrWhiteSpace(VeterinarianComboBox.Text) ||
                    string.IsNullOrWhiteSpace(ServiceComboBox.Text) ||
                    PatientComboBox.SelectedValue == null ||
                    string.IsNullOrWhiteSpace(StartTimeTextBox.Text) ||
                    string.IsNullOrWhiteSpace(EndTimeTextBox.Text))
                {
                    MessageBox.Show("Заполните все поля, включая выбор питомца", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var conn = dbHelper.GetConnection())
                {
                    conn.Open();
                    
                    var appDate = DatePicker.SelectedDate.Value.Date;
                    
                    if (!TimeSpan.TryParse(StartTimeTextBox.Text, out TimeSpan startTime))
                    {
                        MessageBox.Show("Неверный формат времени начала. Используйте формат HH:MM (например, 09:00)",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    if (!TimeSpan.TryParse(EndTimeTextBox.Text, out TimeSpan endTime))
                    {
                        MessageBox.Show("Неверный формат времени окончания. Используйте формат HH:MM (например, 10:30)",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    var vetFullName = VeterinarianComboBox.Text.Trim();
                    var serviceName = ServiceComboBox.Text;
                    
                    // Парсим ФИО ветеринара
                    var nameParts = vetFullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (nameParts.Length < 2)
                    {
                        MessageBox.Show("Неверный формат ФИО ветеринара. Ожидается: Фамилия Имя Отчество",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    var vetSurname = nameParts[0];
                    var vetName = nameParts[1];
                    string vetMiddleName = nameParts.Length > 2 ? nameParts[2] : null;
                    
                    if (startTime >= endTime)
                    {
                        MessageBox.Show("Время окончания должно быть больше времени начала",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    int? vetId = null;
                    using (var cmd = new NpgsqlCommand(
                        "SELECT ID_Veterinarian FROM Veterinarian WHERE Surname = @Surname AND Name = @Name " +
                        "AND (Middle_Name = @Middle_Name OR (Middle_Name IS NULL AND @Middle_Name IS NULL))", conn))
                    {
                        cmd.Parameters.AddWithValue("@Surname", vetSurname);
                        cmd.Parameters.AddWithValue("@Name", vetName);
                        if (vetMiddleName != null)
                        {
                            cmd.Parameters.AddWithValue("@Middle_Name", vetMiddleName);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@Middle_Name", DBNull.Value);
                        }
                        var vetResult = cmd.ExecuteScalar();
                        if (vetResult != null && vetResult != DBNull.Value)
                        {
                            vetId = Convert.ToInt32(vetResult);
                        }
                    }
                    
                    if (vetId == null)
                    {
                        MessageBox.Show("Врач с указанными именем и фамилией не найден",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    int? serviceId = null;
                    using (var cmd = new NpgsqlCommand(
                        "SELECT ID_Service FROM Service WHERE Name = @Name", conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", serviceName);
                        var serviceResult = cmd.ExecuteScalar();
                        if (serviceResult != null && serviceResult != DBNull.Value)
                        {
                            serviceId = Convert.ToInt32(serviceResult);
                        }
                    }
                    
                    if (serviceId == null)
                    {
                        MessageBox.Show("Услуга с указанным названием не найдена",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    // Проверка конфликта времени (кроме текущей записи)
                    bool timeConflict = false;
                    using (var cmd = new NpgsqlCommand(
                        "SELECT EXISTS(SELECT 1 FROM Appointment WHERE ID_Veterinarian = @ID_Veterinarian " +
                        "AND Date = @Date AND ID_Appointment != @ID_Appointment " +
                        "AND ((Start_Time_Appointment < @End_Time AND Start_Time_Appointment >= @Start_Time) " +
                        "OR (End_Time_Appointment > @Start_Time AND End_Time_Appointment <= @End_Time)))", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID_Veterinarian", vetId.Value);
                        cmd.Parameters.AddWithValue("@Date", appDate);
                        cmd.Parameters.AddWithValue("@ID_Appointment", appointmentId);
                        cmd.Parameters.AddWithValue("@Start_Time", startTime);
                        cmd.Parameters.AddWithValue("@End_Time", endTime);
                        timeConflict = Convert.ToBoolean(cmd.ExecuteScalar());
                    }
                    
                    if (timeConflict)
                    {
                        MessageBox.Show("Время для выбранного ветеринара уже занято, выберите другое время",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    // Обновляем запись
                    int petId = Convert.ToInt32(PatientComboBox.SelectedValue);

                    using (var cmd = new NpgsqlCommand(
                        "UPDATE Appointment SET Date = @Date, Start_Time_Appointment = @Start_Time, " +
                        "End_Time_Appointment = @End_Time, ID_Veterinarian = @ID_Veterinarian, ID_Service = @ID_Service, " +
                        "ID_Pet = @ID_Pet WHERE ID_Appointment = @ID_Appointment", conn))
                    {
                        cmd.Parameters.AddWithValue("@Date", appDate);
                        cmd.Parameters.AddWithValue("@Start_Time", startTime);
                        cmd.Parameters.AddWithValue("@End_Time", endTime);
                        cmd.Parameters.AddWithValue("@ID_Veterinarian", vetId.Value);
                        cmd.Parameters.AddWithValue("@ID_Service", serviceId.Value);
                        cmd.Parameters.AddWithValue("@ID_Pet", petId);
                        cmd.Parameters.AddWithValue("@ID_Appointment", appointmentId);
                        
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Запись успешно обновлена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить эту запись?", "Подтверждение", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                    return;

                using (var conn = dbHelper.GetConnection())
                {
                    conn.Open();
                    
                    using (var cmd = new NpgsqlCommand(
                        "DELETE FROM Appointment WHERE ID_Appointment = @ID_Appointment", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID_Appointment", appointmentId);
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

