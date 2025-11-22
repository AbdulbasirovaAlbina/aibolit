using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Npgsql;

namespace Aibolit
{
    public partial class OwnersPetsPage : Page
    {
        private DatabaseHelper dbHelper;

        public OwnersPetsPage()
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
                        o.ID_Owner,
                        o.Surname AS Фамилия,
                        o.Name AS Имя,
                        o.Middle_Name AS Отчество,
                        o.Phone AS Телефон,
                        o.Address AS Адрес,
                        o.Email AS Email,
                        p.ID_Pet,
                        p.Name AS Имя_Питомца,
                        p.View AS Вид,
                        p.Species AS Порода,
                        p.Year_Of_Birth AS Дата_Рождения,
                        p.Color AS Цвет
                    FROM Owner o
                    LEFT JOIN Patient p ON o.ID_Owner = p.ID_Owner
                    ORDER BY o.Surname, o.Name, p.Name";
                
                var dataTable = dbHelper.ExecuteQuery(query);
                OwnersPetsDataGrid.ItemsSource = dataTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddOwnerPetButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddOwnerPetWindow(dbHelper);
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }
    }
}


