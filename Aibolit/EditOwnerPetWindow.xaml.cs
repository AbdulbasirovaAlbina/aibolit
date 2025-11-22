using System;
using System.Data;
using System.Linq;
using System.Windows;
using Npgsql;
using NpgsqlTypes;

namespace Aibolit
{
    public partial class EditOwnerPetWindow : Window
    {
        private readonly DatabaseHelper dbHelper;
        private readonly DataRow dataRow;
        private int ownerId;
        private int? petId;

        public EditOwnerPetWindow(DatabaseHelper dbHelper, DataRow row)
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
                // Заполняем данные владельца
                OwnerSurnameTextBox.Text = dataRow["Фамилия"]?.ToString() ?? "";
                OwnerNameTextBox.Text = dataRow["Имя"]?.ToString() ?? "";
                OwnerMiddleNameTextBox.Text = dataRow["Отчество"]?.ToString() ?? "";
                OwnerPhoneTextBox.Text = dataRow["Телефон"]?.ToString() ?? "";
                OwnerAddressTextBox.Text = dataRow["Адрес"]?.ToString() ?? "";
                OwnerEmailTextBox.Text = dataRow["Email"]?.ToString() ?? "";

                // Заполняем данные питомца (если есть)
                if (dataRow["Имя_Питомца"] != DBNull.Value && !string.IsNullOrEmpty(dataRow["Имя_Питомца"]?.ToString()))
                {
                    PetNameTextBox.Text = dataRow["Имя_Питомца"]?.ToString() ?? "";
                    PetViewTextBox.Text = dataRow["Вид"]?.ToString() ?? "";
                    PetSpeciesTextBox.Text = dataRow["Порода"]?.ToString() ?? "";
                    PetColorTextBox.Text = dataRow["Цвет"]?.ToString() ?? "";

                    if (dataRow["Дата_Рождения"] != DBNull.Value && !string.IsNullOrEmpty(dataRow["Дата_Рождения"]?.ToString()))
                    {
                        string dateStr = dataRow["Дата_Рождения"].ToString();
                        if (DateTime.TryParse(dateStr, out DateTime birthDate))
                        {
                            PetBirthDatePicker.SelectedDate = birthDate.Date;
                        }
                    }
                }

                // Получаем ID владельца и питомца
                using (var conn = dbHelper.GetConnection())
                {
                    conn.Open();
                    string surname = OwnerSurnameTextBox.Text.Trim();
                    string name = OwnerNameTextBox.Text.Trim();
                    string phone = OwnerPhoneTextBox.Text.Trim();
                    
                    // Извлекаем только цифры из телефона для поиска
                    string phoneDigits = new string(phone.Where(char.IsDigit).ToArray());
                    if (phoneDigits.StartsWith("8") && phoneDigits.Length > 1)
                        phoneDigits = "7" + phoneDigits.Substring(1);

                    using (var cmd = new NpgsqlCommand(
                        "SELECT ID_Owner FROM Owner WHERE Surname = @Surname AND Name = @Name AND Phone = @Phone", conn))
                    {
                        cmd.Parameters.AddWithValue("@Surname", surname);
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Phone", phoneDigits);
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            ownerId = Convert.ToInt32(result);
                        }
                    }

                    if (ownerId > 0 && !string.IsNullOrEmpty(PetNameTextBox.Text))
                    {
                        using (var cmd = new NpgsqlCommand(
                            "SELECT ID_Pet FROM Patient WHERE Name = @Name AND View = @View AND Species = @Species AND ID_Owner = @ID_Owner LIMIT 1", conn))
                        {
                            cmd.Parameters.AddWithValue("@Name", PetNameTextBox.Text.Trim());
                            cmd.Parameters.AddWithValue("@View", PetViewTextBox.Text.Trim());
                            cmd.Parameters.AddWithValue("@Species", PetSpeciesTextBox.Text.Trim());
                            cmd.Parameters.AddWithValue("@ID_Owner", ownerId);
                            var result = cmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                petId = Convert.ToInt32(result);
                            }
                        }
                    }
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
                // Валидация
                if (string.IsNullOrWhiteSpace(OwnerSurnameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(OwnerNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(OwnerAddressTextBox.Text) ||
                    string.IsNullOrWhiteSpace(OwnerPhoneTextBox.Text))
                {
                    MessageBox.Show("Заполните все обязательные поля владельца", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Валидация телефона
                string digits = new string(OwnerPhoneTextBox.Text.Where(char.IsDigit).ToArray());
                if (digits.StartsWith("8") && digits.Length > 1)
                    digits = "7" + digits.Substring(1);

                if (digits.Length != 11 || !digits.StartsWith("7"))
                {
                    MessageBox.Show("Введите полный номер телефона (11 цифр, начиная с 7)", "Ошибка телефона", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string phoneNumber = digits;

                using (var conn = dbHelper.GetConnection())
                {
                    conn.Open();

                    // Если ownerId не установлен, пытаемся найти владельца по новым данным
                    if (ownerId == 0)
                    {
                        string surname = OwnerSurnameTextBox.Text.Trim();
                        string name = OwnerNameTextBox.Text.Trim();
                        
                        using (var cmd = new NpgsqlCommand(
                            "SELECT ID_Owner FROM Owner WHERE Surname = @Surname AND Name = @Name AND Phone = @Phone", conn))
                        {
                            cmd.Parameters.AddWithValue("@Surname", surname);
                            cmd.Parameters.AddWithValue("@Name", name);
                            cmd.Parameters.AddWithValue("@Phone", phoneNumber);
                            var result = cmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                ownerId = Convert.ToInt32(result);
                            }
                        }
                    }

                    // Если владелец все еще не найден, создаем нового
                    if (ownerId == 0)
                    {
                        using (var cmd = new NpgsqlCommand(
                            "INSERT INTO Owner (Surname, Name, Middle_Name, Phone, Address, Email) " +
                            "VALUES (@Surname, @Name, @Middle_Name, @Phone, @Address, @Email) " +
                            "RETURNING ID_Owner", conn))
                        {
                            cmd.Parameters.AddWithValue("@Surname", OwnerSurnameTextBox.Text.Trim());
                            cmd.Parameters.AddWithValue("@Name", OwnerNameTextBox.Text.Trim());
                            if (string.IsNullOrWhiteSpace(OwnerMiddleNameTextBox.Text))
                                cmd.Parameters.Add("@Middle_Name", NpgsqlDbType.Varchar).Value = DBNull.Value;
                            else
                                cmd.Parameters.AddWithValue("@Middle_Name", OwnerMiddleNameTextBox.Text.Trim());
                            cmd.Parameters.AddWithValue("@Phone", phoneNumber);
                            cmd.Parameters.AddWithValue("@Address", OwnerAddressTextBox.Text.Trim());
                            if (string.IsNullOrWhiteSpace(OwnerEmailTextBox.Text))
                                cmd.Parameters.Add("@Email", NpgsqlDbType.Varchar).Value = DBNull.Value;
                            else
                                cmd.Parameters.AddWithValue("@Email", OwnerEmailTextBox.Text.Trim());
                            
                            ownerId = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                    }
                    else
                    {
                        // Обновляем данные существующего владельца
                        using (var cmd = new NpgsqlCommand(
                            "UPDATE Owner SET Surname = @Surname, Name = @Name, Middle_Name = @Middle_Name, " +
                            "Phone = @Phone, Address = @Address, Email = @Email WHERE ID_Owner = @ID_Owner", conn))
                        {
                            cmd.Parameters.AddWithValue("@Surname", OwnerSurnameTextBox.Text.Trim());
                            cmd.Parameters.AddWithValue("@Name", OwnerNameTextBox.Text.Trim());
                            if (string.IsNullOrWhiteSpace(OwnerMiddleNameTextBox.Text))
                                cmd.Parameters.Add("@Middle_Name", NpgsqlDbType.Varchar).Value = DBNull.Value;
                            else
                                cmd.Parameters.AddWithValue("@Middle_Name", OwnerMiddleNameTextBox.Text.Trim());
                            cmd.Parameters.AddWithValue("@Phone", phoneNumber);
                            cmd.Parameters.AddWithValue("@Address", OwnerAddressTextBox.Text.Trim());
                            if (string.IsNullOrWhiteSpace(OwnerEmailTextBox.Text))
                                cmd.Parameters.Add("@Email", NpgsqlDbType.Varchar).Value = DBNull.Value;
                            else
                                cmd.Parameters.AddWithValue("@Email", OwnerEmailTextBox.Text.Trim());
                            cmd.Parameters.AddWithValue("@ID_Owner", ownerId);

                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Обновляем или добавляем питомца
                    if (!string.IsNullOrWhiteSpace(PetNameTextBox.Text))
                    {
                        if (string.IsNullOrWhiteSpace(PetViewTextBox.Text) ||
                            string.IsNullOrWhiteSpace(PetSpeciesTextBox.Text) ||
                            string.IsNullOrWhiteSpace(PetColorTextBox.Text) ||
                            !PetBirthDatePicker.SelectedDate.HasValue)
                        {
                            MessageBox.Show("Заполните все поля питомца", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // Проверяем, существует ли питомец с такими данными
                        if (!petId.HasValue)
                        {
                            using (var cmd = new NpgsqlCommand(
                                "SELECT ID_Pet FROM Patient WHERE Name = @Name AND View = @View AND Species = @Species AND ID_Owner = @ID_Owner LIMIT 1", conn))
                            {
                                cmd.Parameters.AddWithValue("@Name", PetNameTextBox.Text.Trim());
                                cmd.Parameters.AddWithValue("@View", PetViewTextBox.Text.Trim());
                                cmd.Parameters.AddWithValue("@Species", PetSpeciesTextBox.Text.Trim());
                                cmd.Parameters.AddWithValue("@ID_Owner", ownerId);
                                var result = cmd.ExecuteScalar();
                                if (result != null && result != DBNull.Value)
                                {
                                    petId = Convert.ToInt32(result);
                                }
                            }
                        }

                        if (petId.HasValue)
                        {
                            // Обновляем существующего питомца
                            using (var cmd = new NpgsqlCommand(
                                "UPDATE Patient SET Name = @Name, View = @View, Species = @Species, " +
                                "Year_Of_Birth = @Year_Of_Birth, Color = @Color WHERE ID_Pet = @ID_Pet", conn))
                            {
                                cmd.Parameters.AddWithValue("@Name", PetNameTextBox.Text.Trim());
                                cmd.Parameters.AddWithValue("@View", PetViewTextBox.Text.Trim());
                                cmd.Parameters.AddWithValue("@Species", PetSpeciesTextBox.Text.Trim());
                                cmd.Parameters.AddWithValue("@Year_Of_Birth", PetBirthDatePicker.SelectedDate.Value.Date);
                                cmd.Parameters.AddWithValue("@Color", PetColorTextBox.Text.Trim());
                                cmd.Parameters.AddWithValue("@ID_Pet", petId.Value);

                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // Добавляем нового питомца - проверяем, что ownerId установлен
                            if (ownerId == 0)
                            {
                                MessageBox.Show("Ошибка: не удалось определить владельца", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            using (var cmd = new NpgsqlCommand(
                                "INSERT INTO Patient (Name, View, Species, Year_Of_Birth, Color, ID_Owner) " +
                                "VALUES (@Name, @View, @Species, @Year_Of_Birth, @Color, @ID_Owner)", conn))
                            {
                                cmd.Parameters.AddWithValue("@Name", PetNameTextBox.Text.Trim());
                                cmd.Parameters.AddWithValue("@View", PetViewTextBox.Text.Trim());
                                cmd.Parameters.AddWithValue("@Species", PetSpeciesTextBox.Text.Trim());
                                cmd.Parameters.AddWithValue("@Year_Of_Birth", PetBirthDatePicker.SelectedDate.Value.Date);
                                cmd.Parameters.AddWithValue("@Color", PetColorTextBox.Text.Trim());
                                cmd.Parameters.AddWithValue("@ID_Owner", ownerId);

                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                MessageBox.Show("Данные успешно сохранены!", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Npgsql.PostgresException pgEx)
            {
                MessageBox.Show($"Ошибка БД:\n{pgEx.Message}\nКод: {pgEx.SqlState}", "Ошибка PostgreSQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OwnerPhoneTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var tb = sender as System.Windows.Controls.TextBox;
            if (tb == null) return;

            var digits = new string(tb.Text.Where(char.IsDigit).ToArray());
            if (digits.StartsWith("8") && digits.Length > 1)
                digits = "7" + digits.Substring(1);
            if (digits.Length > 11)
                digits = digits.Substring(0, 11);

            string formatted = "";
            if (digits.Length >= 1) formatted += $"+{digits[0]}";
            if (digits.Length >= 2) formatted += $" ({digits.Substring(1, Math.Min(3, digits.Length - 1))}";
            if (digits.Length >= 5) formatted += $") {digits.Substring(4, Math.Min(3, digits.Length - 4))}";
            if (digits.Length >= 8) formatted += $"-{digits.Substring(7, Math.Min(2, digits.Length - 7))}";
            if (digits.Length >= 10) formatted += $"-{digits.Substring(9, Math.Min(2, digits.Length - 9))}";

            tb.Text = formatted;
            tb.CaretIndex = tb.Text.Length;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

