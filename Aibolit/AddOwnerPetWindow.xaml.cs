using System;
using System.Windows;
using Npgsql;

namespace Aibolit
{
    public partial class AddOwnerPetWindow : Window
    {
        private DatabaseHelper dbHelper;

        public AddOwnerPetWindow(DatabaseHelper dbHelper)
        {
            InitializeComponent();
            this.dbHelper = dbHelper;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(OwnerSurnameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(OwnerNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(OwnerPhoneTextBox.Text) ||
                    string.IsNullOrWhiteSpace(OwnerAddressTextBox.Text))
                {
                    MessageBox.Show("Заполните все обязательные поля владельца", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(PetNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(PetViewTextBox.Text) ||
                    string.IsNullOrWhiteSpace(PetSpeciesTextBox.Text) ||
                    string.IsNullOrWhiteSpace(PetBirthDateTextBox.Text) ||
                    string.IsNullOrWhiteSpace(PetColorTextBox.Text))
                {
                    MessageBox.Show("Заполните все обязательные поля питомца", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Вызов процедуры
                // Пробуем оба варианта: с кавычками и без (нижний регистр)
                using (var conn = dbHelper.GetConnection())
                {
                    conn.Open();
                    
                    // Подготовка параметров
                    var surname = OwnerSurnameTextBox.Text;
                    var name = OwnerNameTextBox.Text;
                    var middleName = string.IsNullOrWhiteSpace(OwnerMiddleNameTextBox.Text) ? (object)DBNull.Value : OwnerMiddleNameTextBox.Text;
                    var phone = long.Parse(OwnerPhoneTextBox.Text);
                    var address = OwnerAddressTextBox.Text;
                    var email = string.IsNullOrWhiteSpace(OwnerEmailTextBox.Text) ? (object)DBNull.Value : OwnerEmailTextBox.Text;
                    var petName = PetNameTextBox.Text;
                    var petView = PetViewTextBox.Text;
                    var petSpecies = PetSpeciesTextBox.Text;
                    var birthDate = DateTime.Parse(PetBirthDateTextBox.Text).Date;
                    var petColor = PetColorTextBox.Text;
                    
                    // Пробуем сначала с кавычками (как в SQL)
                    try
                    {
                        using (var cmd = new NpgsqlCommand("CALL \"Add_New_Owner_And_Pet\"(@p_Surname, @p_Name, @p_Middle_Name, @p_Phone, @p_Address, @p_Email, @pet_Name, @pet_View, @pet_Species, @pet_Year_Of_Birth, @pet_Color)", conn))
                        {
                            cmd.Parameters.AddWithValue("@p_Surname", surname);
                            cmd.Parameters.AddWithValue("@p_Name", name);
                            cmd.Parameters.AddWithValue("@p_Middle_Name", middleName);
                            cmd.Parameters.AddWithValue("@p_Phone", phone);
                            cmd.Parameters.AddWithValue("@p_Address", address);
                            cmd.Parameters.AddWithValue("@p_Email", email);
                            cmd.Parameters.AddWithValue("@pet_Name", petName);
                            cmd.Parameters.AddWithValue("@pet_View", petView);
                            cmd.Parameters.AddWithValue("@pet_Species", petSpecies);
                            cmd.Parameters.AddWithValue("@pet_Year_Of_Birth", birthDate);
                            cmd.Parameters.AddWithValue("@pet_Color", petColor);
                            
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch (Npgsql.PostgresException ex) when (ex.SqlState == "42883")
                    {
                        // Если процедура не найдена, пробуем без кавычек (нижний регистр)
                        using (var cmd = new NpgsqlCommand("CALL add_new_owner_and_pet(@p_Surname, @p_Name, @p_Middle_Name, @p_Phone, @p_Address, @p_Email, @pet_Name, @pet_View, @pet_Species, @pet_Year_Of_Birth, @pet_Color)", conn))
                        {
                            cmd.Parameters.AddWithValue("@p_Surname", surname);
                            cmd.Parameters.AddWithValue("@p_Name", name);
                            cmd.Parameters.AddWithValue("@p_Middle_Name", middleName);
                            cmd.Parameters.AddWithValue("@p_Phone", phone);
                            cmd.Parameters.AddWithValue("@p_Address", address);
                            cmd.Parameters.AddWithValue("@p_Email", email);
                            cmd.Parameters.AddWithValue("@pet_Name", petName);
                            cmd.Parameters.AddWithValue("@pet_View", petView);
                            cmd.Parameters.AddWithValue("@pet_Species", petSpecies);
                            cmd.Parameters.AddWithValue("@pet_Year_Of_Birth", birthDate);
                            cmd.Parameters.AddWithValue("@pet_Color", petColor);
                            
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show("Владелец и питомец успешно добавлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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

