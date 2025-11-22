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
                    DatePicker.SelectedDate == null ||
                    string.IsNullOrWhiteSpace(StartTimeTextBox.Text) ||
                    string.IsNullOrWhiteSpace(EndTimeTextBox.Text))
                {
                    MessageBox.Show("Заполните все поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            vetId = Convert.ToInt32(result);
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
                        "INSERT INTO Appointment (Date, Start_Time_Appointment, End_Time_Appointment, ID_Veterinarian, ID_Service) " +
                        "VALUES (@Date, @Start_Time, @End_Time, @ID_Veterinarian, @ID_Service)", conn))
                    {
                        cmd.Parameters.AddWithValue("@Date", appDate);
                        cmd.Parameters.AddWithValue("@Start_Time", startTime);
                        cmd.Parameters.AddWithValue("@End_Time", endTime);
                        cmd.Parameters.AddWithValue("@ID_Veterinarian", vetId.Value);
                        cmd.Parameters.AddWithValue("@ID_Service", serviceId.Value);
                        
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

