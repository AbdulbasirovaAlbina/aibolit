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
            Loaded += AppointmentsPage_Loaded;
        }

        private void AppointmentsPage_Loaded(object sender, RoutedEventArgs e) => LoadData();

        private void LoadData()
        {
            try
            {
                string query = @"
                    SELECT 
                        a.ID_Appointment,
                        a.ID_Pet,
                        TO_CHAR(a.Date, 'YYYY-MM-DD') AS Дата,
                        a.Start_Time_Appointment AS Время_Начала,
                        a.End_Time_Appointment AS Время_Окончания,
                        v.Surname || ' ' || v.Name || ' ' || v.Middle_Name AS Ветеринар,
                        v.Surname AS Фамилия_Ветеринара,
                        v.Name AS Имя_Ветеринара,
                        s.Name AS Услуга,
                        s.Cost AS Стоимость,
                        p.Name AS Питомец,
                        o.Surname || ' ' || o.Name AS Владелец,
                        COALESCE(q.Symptoms, '') AS Симптомы,
                        COALESCE(q.Appointment_And_Treatment, '') AS ""Назначение и лечение""
                    FROM Appointment a
                    JOIN Veterinarian v ON a.ID_Veterinarian = v.ID_Veterinarian
                    JOIN Service s ON a.ID_Service = s.ID_Service
                    JOIN Patient p ON a.ID_Pet = p.ID_Pet
                    JOIN Owner o ON p.ID_Owner = o.ID_Owner
                    LEFT JOIN Questionnaire q ON q.ID_Appointment = a.ID_Appointment
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

        private void AppointmentsDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var header = e.Column.Header?.ToString();
            if (!string.IsNullOrWhiteSpace(header))
            {
                if (header.Equals("Фамилия ветеринара", StringComparison.OrdinalIgnoreCase) ||
                    header.Equals("Имя ветеринара", StringComparison.OrdinalIgnoreCase))
                {
                    e.Column.Visibility = Visibility.Collapsed;
                    return;
                }
            }

            DataGridColumnFormatter.Apply(e);
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

        private void FillCardButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppointmentsDataGrid.SelectedItem is not DataRowView rowView)
            {
                MessageBox.Show("Выберите запись перед заполнением карты", "Уведомление",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (rowView["ID_Appointment"] == DBNull.Value || rowView["ID_Pet"] == DBNull.Value)
            {
                MessageBox.Show("Не удалось определить запись или питомца", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int appointmentId = Convert.ToInt32(rowView["ID_Appointment"]);
            int petId = Convert.ToInt32(rowView["ID_Pet"]);

            var dialog = new QuestionnaireWindow(dbHelper, appointmentId, petId);
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void PrintReceiptButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppointmentsDataGrid.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Выберите запись, чтобы распечатать чек", "Уведомление",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string filePath = ReceiptExporter.GenerateReceipt(row.Row, DateTime.Now);
                MessageBox.Show($"Чек сохранён в файле:\n{filePath}", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось сохранить чек: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}


