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
                        a.ID_Appointment,
                        TO_CHAR(a.Date, 'YYYY-MM-DD') AS Дата,
                        a.Start_Time_Appointment AS Время_Начала,
                        a.End_Time_Appointment AS Время_Окончания,
                        v.Surname || ' ' || v.Name || ' ' || v.Middle_Name AS Ветеринар,
                        v.Surname AS Фамилия_Ветеринара,
                        v.Name AS Имя_Ветеринара,
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

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void AppointmentsDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // Скрываем колонку ID_Appointment
            if (e.Column.Header.ToString() == "ID_Appointment" || 
                e.Column.Header.ToString() == "Id_Appointment")
            {
                e.Column.Visibility = Visibility.Collapsed;
            }
        }

        private void AppointmentsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AppointmentsDataGrid.SelectedItem != null)
            {
                try
                {
                    DataRowView rowView = AppointmentsDataGrid.SelectedItem as DataRowView;
                    if (rowView != null)
                    {
                        var dialog = new EditAppointmentWindow(dbHelper, rowView.Row);
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

