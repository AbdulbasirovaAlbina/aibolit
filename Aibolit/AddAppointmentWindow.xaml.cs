using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using Npgsql;

namespace Aibolit
{
    public partial class AddAppointmentWindow : Window
    {
        private DatabaseHelper dbHelper;
        private List<PatientItem> patients = new List<PatientItem>();

        public AddAppointmentWindow(DatabaseHelper dbHelper)
        {
            InitializeComponent();
            this.dbHelper = dbHelper;
            LoadComboBoxes();
        }

        private class PatientItem
        {
            public int Id { get; set; }
            public string Display { get; set; } = string.Empty;
        }

        private void LoadComboBoxes()
        {
            try
            {
                // Загружаем ветеринаров с полным ФИО
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
                if (string.IsNullOrWhiteSpace(VeterinarianComboBox.Text) ||
                    string.IsNullOrWhiteSpace(ServiceComboBox.Text) ||
                    PatientComboBox.SelectedValue == null ||
                    DatePicker.SelectedDate == null ||
                    string.IsNullOrWhiteSpace(StartTimeTextBox.Text) ||
                    string.IsNullOrWhiteSpace(EndTimeTextBox.Text))
                {
                    MessageBox.Show("Заполните все поля, включая выбор питомца", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var conn = dbHelper.GetConnection())
                {
                    conn.Open();
                    
                    var vetFullName = VeterinarianComboBox.Text.Trim();
                    var serviceName = ServiceComboBox.Text;
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
                    
                    if (startTime >= endTime)
                    {
                        MessageBox.Show("Время окончания должно быть больше времени начала",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    int petId = Convert.ToInt32(PatientComboBox.SelectedValue);
                    
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
                    
                    int? vetId = null;
                    TimeSpan? vetStart = null;
                    TimeSpan? vetEnd = null;
                    using (var cmd = new NpgsqlCommand(
                        "SELECT ID_Veterinarian, Start_Time_Day, End_Time_Day FROM Veterinarian WHERE Surname = @Surname AND Name = @Name " +
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
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                vetId = reader.GetInt32(0);
                                if (!reader.IsDBNull(1)) vetStart = reader.GetTimeSpan(1);
                                if (!reader.IsDBNull(2)) vetEnd = reader.GetTimeSpan(2);
                            }
                        }
                    }
                    
                    if (vetId == null)
                    {
                        MessageBox.Show("Врач с указанными именем и фамилией не найден",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (vetStart.HasValue && startTime < vetStart.Value)
                    {
                        MessageBox.Show($"Врач начинает работать в {vetStart:hh\\:mm}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (vetEnd.HasValue && endTime > vetEnd.Value)
                    {
                        MessageBox.Show($"Врач заканчивает работу в {vetEnd:hh\\:mm}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    int? serviceId = null;
                    using (var cmd = new NpgsqlCommand(
                        "SELECT ID_Service FROM Service WHERE Name = @Name", conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", serviceName);
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            serviceId = Convert.ToInt32(result);
                        }
                    }
                    
                    if (serviceId == null)
                    {
                        MessageBox.Show("Услуга с указанным названием не найдена",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    bool timeConflict = false;
                    using (var cmd = new NpgsqlCommand(
                        "SELECT EXISTS(SELECT 1 FROM Appointment WHERE ID_Veterinarian = @ID_Veterinarian " +
                        "AND Date = @Date AND ((Start_Time_Appointment < @End_Time AND Start_Time_Appointment >= @Start_Time) " +
                        "OR (End_Time_Appointment > @Start_Time AND End_Time_Appointment <= @End_Time)))", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID_Veterinarian", vetId.Value);
                        cmd.Parameters.AddWithValue("@Date", appDate);
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
                    
                    using (var cmd = new NpgsqlCommand(
                        "INSERT INTO Appointment (Date, Start_Time_Appointment, End_Time_Appointment, ID_Veterinarian, ID_Service, ID_Pet) " +
                        "VALUES (@Date, @Start_Time, @End_Time, @ID_Veterinarian, @ID_Service, @ID_Pet)", conn))
                    {
                        cmd.Parameters.AddWithValue("@Date", appDate);
                        cmd.Parameters.AddWithValue("@Start_Time", startTime);
                        cmd.Parameters.AddWithValue("@End_Time", endTime);
                        cmd.Parameters.AddWithValue("@ID_Veterinarian", vetId.Value);
                        cmd.Parameters.AddWithValue("@ID_Service", serviceId.Value);
                        cmd.Parameters.AddWithValue("@ID_Pet", petId);
                        
                        cmd.ExecuteNonQuery();
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

