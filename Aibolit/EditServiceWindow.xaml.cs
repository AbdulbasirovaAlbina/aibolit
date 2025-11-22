using System;
using System.Data;
using System.Windows;
using Npgsql;

namespace Aibolit
{
    public partial class EditServiceWindow : Window
    {
        private readonly DatabaseHelper dbHelper;
        private readonly DataRow dataRow;
        private int serviceId;

        public EditServiceWindow(DatabaseHelper dbHelper, DataRow row)
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
                serviceId = Convert.ToInt32(dataRow["ID_Service"]);
                NameTextBox.Text = dataRow["Название"]?.ToString() ?? "";
                DescriptionTextBox.Text = dataRow["Описание"]?.ToString() ?? "";
                
                if (dataRow["Стоимость"] != DBNull.Value)
                {
                    if (decimal.TryParse(dataRow["Стоимость"].ToString(), out decimal cost))
                    {
                        CostTextBox.Text = cost.ToString("F2");
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
                if (string.IsNullOrWhiteSpace(NameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ||
                    string.IsNullOrWhiteSpace(CostTextBox.Text))
                {
                    MessageBox.Show("Заполните все поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(CostTextBox.Text, out decimal cost) || cost < 0)
                {
                    MessageBox.Show("Введите корректную стоимость (положительное число)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var conn = dbHelper.GetConnection())
                {
                    conn.Open();
                    
                    using (var cmd = new NpgsqlCommand(
                        "UPDATE Service SET Name = @Name, Description = @Description, Cost = @Cost " +
                        "WHERE ID_Service = @ID_Service", conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", NameTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@Description", DescriptionTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@Cost", cost);
                        cmd.Parameters.AddWithValue("@ID_Service", serviceId);
                        
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Услуга успешно обновлена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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
                // Проверяем, используется ли услуга в записях
                using (var conn = dbHelper.GetConnection())
                {
                    conn.Open();
                    
                    using (var cmd = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM Appointment WHERE ID_Service = @ID_Service", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID_Service", serviceId);
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        
                        if (count > 0)
                        {
                            MessageBox.Show($"Невозможно удалить услугу: она используется в {count} записях на приём", 
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                }

                var result = MessageBox.Show("Вы уверены, что хотите удалить эту услугу?", "Подтверждение", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                    return;

                using (var conn = dbHelper.GetConnection())
                {
                    conn.Open();
                    
                    using (var cmd = new NpgsqlCommand(
                        "DELETE FROM Service WHERE ID_Service = @ID_Service", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID_Service", serviceId);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Услуга успешно удалена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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

