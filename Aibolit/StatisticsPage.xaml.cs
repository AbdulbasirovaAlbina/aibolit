using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace Aibolit
{
    public partial class StatisticsPage : Page
    {
        private DatabaseHelper dbHelper;

        public StatisticsPage()
        {
            InitializeComponent();
            dbHelper = new DatabaseHelper();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                string query = @"
                    SELECT 
                        Veterinarian_Surname AS Фамилия,
                        Veterinarian_Name AS Имя,
                        Patient_Count AS Количество_Пациентов
                    FROM Veterinarian_Patient_Count
                    ORDER BY Patient_Count DESC, Veterinarian_Surname";
                
                var dataTable = dbHelper.ExecuteQuery(query);
                StatisticsDataGrid.ItemsSource = dataTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }
    }
}


