using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace Aibolit
{
    public partial class PatientCardsPage : Page
    {
        private DatabaseHelper dbHelper;

        public PatientCardsPage()
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
                        Pet_Name AS Имя_Питомца,
                        Pet_View AS Вид,
                        Pet_Species AS Порода,
                        Pet_Color AS Цвет,
                        Pet_Year_Of_Birth AS Дата_Рождения,
                        Owner_Name AS Владелец,
                        Owner_Phone AS Телефон,
                        Owner_Address AS Адрес,
                        Owner_Email AS Email,
                        Services AS Услуги,
                        Symptoms AS Симптомы,
                        Treatment_Details AS Лечение
                    FROM Patient_Card
                    ORDER BY Pet_Name";
                
                var dataTable = dbHelper.ExecuteQuery(query);
                PatientCardsDataGrid.ItemsSource = dataTable.DefaultView;
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


