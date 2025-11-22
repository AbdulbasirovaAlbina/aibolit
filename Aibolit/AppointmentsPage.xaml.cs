using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Npgsql;

namespace Aibolit
{
    public partial class AppointmentsPage : Page
    {
        private DatabaseHelper dbHelper;

        public AppointmentsPage()
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
                        a.ID_Appointment AS ID,
                        a.Date AS Дата,
                        a.Start_Time_Appointment AS Время_Начала,
                        a.End_Time_Appointment AS Время_Окончания,
                        v.Surname || ' ' || v.Name || ' ' || v.Middle_Name AS Ветеринар,
                        s.Name AS Услуга,
                        s.Cost AS Стоимость
                    FROM Appointment a
                    JOIN Veterinarian v ON a.ID_Veterinarian = v.ID_Veterinarian
                    JOIN Service s ON a.ID_Service = s.ID_Service
                    ORDER BY a.Date DESC, a.Start_Time_Appointment";
                
                var dataTable = dbHelper.ExecuteQuery(query);
                AppointmentsDataGrid.ItemsSource = dataTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddAppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddAppointmentWindow(dbHelper);
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void UpdateTimeButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new UpdateAppointmentTimeWindow(dbHelper);
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void DeleteAppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new DeleteAppointmentWindow(dbHelper);
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


