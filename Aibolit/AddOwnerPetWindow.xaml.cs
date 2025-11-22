using System;
using System.Linq;
using System.Windows;
using Npgsql;
using NpgsqlTypes;

namespace Aibolit
{
    public partial class AddOwnerPetWindow : Window
    {
        private readonly DatabaseHelper dbHelper;

        public AddOwnerPetWindow(DatabaseHelper dbHelper)
        {
            InitializeComponent();
            this.dbHelper = dbHelper;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // === Обязательные поля ===
                if (string.IsNullOrWhiteSpace(OwnerSurnameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(OwnerNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(OwnerAddressTextBox.Text) ||
                    string.IsNullOrWhiteSpace(PetNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(PetViewTextBox.Text) ||
                    string.IsNullOrWhiteSpace(PetSpeciesTextBox.Text) ||
                    string.IsNullOrWhiteSpace(PetColorTextBox.Text) ||
                    !PetBirthDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Заполните все поля со звёздочкой и выберите дату рождения",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // === Телефон — чистые 11 цифр, начинается с 7 ===
                string digits = new string(OwnerPhoneTextBox.Text.Where(char.IsDigit).ToArray());

                // Коррекция: если ввели 8 → заменяем на 7
                if (digits.StartsWith("8") && digits.Length > 1)
                    digits = "7" + digits.Substring(1);

                // Убеждаемся, что начинается с 7 и ровно 11 цифр
                if (digits.Length != 11 || !digits.StartsWith("7"))
                {
                    MessageBox.Show("Введите полный номер телефона (11 цифр, начиная с 7)",
                        "Ошибка телефона", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Передаём как строку — VARCHAR в PostgreSQL
                string phoneNumber = digits;

                DateTime birthDate = PetBirthDatePicker.SelectedDate.Value.Date;

                using (var conn = dbHelper.GetConnection())
                {
                    conn.Open();
                    
                    string surname = OwnerSurnameTextBox.Text.Trim();
                    string name = OwnerNameTextBox.Text.Trim();
                    string middleName = string.IsNullOrWhiteSpace(OwnerMiddleNameTextBox.Text) ? null : OwnerMiddleNameTextBox.Text.Trim();
                    string address = OwnerAddressTextBox.Text.Trim();
                    string email = string.IsNullOrWhiteSpace(OwnerEmailTextBox.Text) ? null : OwnerEmailTextBox.Text.Trim();
                    
                    string petName = PetNameTextBox.Text.Trim();
                    string petView = PetViewTextBox.Text.Trim();
                    string petSpecies = PetSpeciesTextBox.Text.Trim();
                    string petColor = PetColorTextBox.Text.Trim();
                    
                    // === Шаг 1: Проверка наличия владельца ===
                    int? ownerId = null;
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
                    
                    // === Шаг 2: Если владелец не найден, добавляем нового ===
                    if (ownerId == null)
                    {
                        using (var cmd = new NpgsqlCommand(
                            "INSERT INTO Owner (Surname, Name, Middle_Name, Phone, Address, Email) " +
                            "VALUES (@Surname, @Name, @Middle_Name, @Phone, @Address, @Email) " +
                            "RETURNING ID_Owner", conn))
                        {
                            cmd.Parameters.AddWithValue("@Surname", surname);
                            cmd.Parameters.AddWithValue("@Name", name);
                            if (middleName == null)
                                cmd.Parameters.Add("@Middle_Name", NpgsqlDbType.Varchar).Value = DBNull.Value;
                            else
                                cmd.Parameters.AddWithValue("@Middle_Name", middleName);
                            cmd.Parameters.AddWithValue("@Phone", phoneNumber);
                            cmd.Parameters.AddWithValue("@Address", address);
                            if (email == null)
                                cmd.Parameters.Add("@Email", NpgsqlDbType.Varchar).Value = DBNull.Value;
                            else
                                cmd.Parameters.AddWithValue("@Email", email);
                            
                            var result = cmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                ownerId = Convert.ToInt32(result);
                            }
                            else
                            {
                                throw new Exception("Не удалось получить ID созданного владельца");
                            }
                        }
                    }
                    
                    // Проверяем, что ownerId установлен перед добавлением питомца
                    if (!ownerId.HasValue || ownerId.Value == 0)
                    {
                        throw new Exception("Ошибка: не удалось определить ID владельца");
                    }
                    
                    // === Шаг 3: Проверка наличия питомца с таким же именем, видом и породой у этого владельца ===
                    bool petExists = false;
                    using (var cmd = new NpgsqlCommand(
                        "SELECT EXISTS(SELECT 1 FROM Patient WHERE Name = @Name AND View = @View AND Species = @Species AND ID_Owner = @ID_Owner)", conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", petName);
                        cmd.Parameters.AddWithValue("@View", petView);
                        cmd.Parameters.AddWithValue("@Species", petSpecies);
                        cmd.Parameters.AddWithValue("@ID_Owner", ownerId.Value);
                        petExists = Convert.ToBoolean(cmd.ExecuteScalar());
                    }
                    
                    // === Шаг 4: Если питомец не найден, добавляем нового ===
                    if (!petExists)
                    {
                        using (var cmd = new NpgsqlCommand(
                            "INSERT INTO Patient (Name, View, Species, Year_Of_Birth, Color, ID_Owner) " +
                            "VALUES (@Name, @View, @Species, @Year_Of_Birth, @Color, @ID_Owner)", conn))
                        {
                            cmd.Parameters.AddWithValue("@Name", petName);
                            cmd.Parameters.AddWithValue("@View", petView);
                            cmd.Parameters.AddWithValue("@Species", petSpecies);
                            cmd.Parameters.AddWithValue("@Year_Of_Birth", birthDate);
                            cmd.Parameters.AddWithValue("@Color", petColor);
                            cmd.Parameters.AddWithValue("@ID_Owner", ownerId.Value);
                            
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Питомец с таким именем, видом и породой уже существует у данного владельца.",
                            "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                MessageBox.Show("Владелец и питомец успешно добавлены!", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Npgsql.PostgresException pgEx)
            {
                MessageBox.Show($"Ошибка БД:\n{pgEx.Message}\nКод: {pgEx.SqlState}",
                    "Ошибка PostgreSQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === Маска телефона: +7 (___) ___-__-__ ===
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

