using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace Aibolit
{
    public partial class InvoicesPage : Page
    {
        private DatabaseHelper dbHelper;

        public InvoicesPage()
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
                        Check_Number AS Номер_Чека,
                        Check_Date AS Дата,
                        Veterinarian_Name AS Ветеринар,
                        Pet_Info AS Информация_О_Питомце,
                        Service_Name AS Услуга,
                        Service_Description AS Описание,
                        Service_Cost AS Стоимость
                    FROM Invoice_Check
                    ORDER BY Check_Date DESC";
                
                var dataTable = dbHelper.ExecuteQuery(query);
                InvoicesDataGrid.ItemsSource = dataTable.DefaultView;
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


