using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Npgsql;

namespace Aibolit
{
    public partial class StatisticsPage : Page
    {
        private DatabaseHelper dbHelper;

        public StatisticsPage()
        {
            InitializeComponent();
            dbHelper = new DatabaseHelper();
            
            // Устанавливаем даты по умолчанию: последние 30 дней
            EndDatePicker.SelectedDate = DateTime.Today;
            StartDatePicker.SelectedDate = DateTime.Today.AddDays(-30);

            Loaded += StatisticsPage_Loaded;
        }

        private void StatisticsPage_Loaded(object sender, RoutedEventArgs e) => LoadData();

        private void LoadData()
        {
            try
            {
                DateTime? startDate = StartDatePicker.SelectedDate;
                DateTime? endDate = EndDatePicker.SelectedDate;

                if (!startDate.HasValue || !endDate.HasValue)
                {
                    MessageBox.Show("Выберите период для статистики", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (startDate.Value > endDate.Value)
                {
                    MessageBox.Show("Дата начала не может быть больше даты окончания", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string query = @"
                    SELECT 
                        v.Surname AS Фамилия,
                        v.Name AS Имя,
                        COALESCE(v.Middle_Name, '') AS Отчество,
                        COUNT(DISTINCT a.ID_Pet) AS ""Количество пациентов"",
                        COUNT(a.ID_Appointment) AS ""Количество приёмов""
                    FROM Veterinarian v
                    LEFT JOIN Appointment a 
                        ON v.ID_Veterinarian = a.ID_Veterinarian
                        AND a.Date >= @StartDate
                        AND a.Date <= @EndDate
                    GROUP BY v.Surname, v.Name, v.Middle_Name
                    ORDER BY ""Количество пациентов"" DESC, v.Surname";
                
                var dataTable = dbHelper.ExecuteQuery(query, 
                    new NpgsqlParameter("@StartDate", startDate.Value.Date),
                    new NpgsqlParameter("@EndDate", endDate.Value.Date));
                StatisticsDataGrid.ItemsSource = dataTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StartDatePicker.SelectedDate.HasValue && EndDatePicker.SelectedDate.HasValue)
            {
                LoadData();
            }
        }

        private void EndDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StartDatePicker.SelectedDate.HasValue && EndDatePicker.SelectedDate.HasValue)
            {
                LoadData();
            }
        }

        private void StatisticsDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            DataGridColumnFormatter.Apply(e);
        }
    }
}

