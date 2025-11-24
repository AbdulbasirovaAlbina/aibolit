using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Npgsql;

namespace Aibolit
{
    public partial class ServicesPage : Page
    {
        private DatabaseHelper dbHelper;

        public ServicesPage()
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
                        ID_Service,
                        Name AS Название,
                        Description AS Описание,
                        Cost AS Стоимость
                    FROM Service
                    ORDER BY Name";
                
                var dataTable = dbHelper.ExecuteQuery(query);
                ServicesDataGrid.ItemsSource = dataTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddServiceButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddServiceWindow(dbHelper);
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void ServicesDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            DataGridColumnFormatter.Apply(e);
        }

        private void ServicesDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ServicesDataGrid.SelectedItem != null)
            {
                try
                {
                    DataRowView rowView = ServicesDataGrid.SelectedItem as DataRowView;
                    if (rowView != null)
                    {
                        var dialog = new EditServiceWindow(dbHelper, rowView.Row);
                        if (dialog.ShowDialog() == true)
                        {
                            LoadData();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии окна редактирования: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

