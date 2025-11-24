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
            Loaded += InvoicesPage_Loaded;
        }

        private void InvoicesPage_Loaded(object sender, RoutedEventArgs e) => LoadData();

        private void LoadData()
        {
            try
            {
                string query = @"
                    SELECT 
                        'ЧЕК № ' || a.ID_Appointment AS ""Номер чека"",
                        'Дата: ' || TO_CHAR(a.Date, 'YYYY-MM-DD') AS Дата,
                        'Ветеринар: ' || v.Surname || ' ' || v.Name || ' ' || v.Middle_Name AS Ветеринар,
                        'Пациент: ' || p.Name || ' (вид: ' || p.View || ', порода: ' || p.Species || ')' AS ""Информация о питомце"",
                        o.Surname || ' ' || o.Name || ' ' || COALESCE(o.Middle_Name, '') AS Владелец,
                        'Услуга: ' || s.Name AS Услуга,
                        'Описание: ' || s.Description AS Описание,
                        'Стоимость: ' || TO_CHAR(s.Cost, '99999.99') || ' руб.' AS Стоимость
                    FROM Appointment a
                    JOIN Service s ON a.ID_Service = s.ID_Service
                    JOIN Veterinarian v ON a.ID_Veterinarian = v.ID_Veterinarian
                    JOIN Patient p ON a.ID_Pet = p.ID_Pet
                    JOIN Owner o ON p.ID_Owner = o.ID_Owner
                    ORDER BY a.Date DESC, p.Name ASC";
                
                var dataTable = dbHelper.ExecuteQuery(query);
                InvoicesDataGrid.ItemsSource = dataTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InvoicesDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            DataGridColumnFormatter.Apply(e);
        }

        private void PrintReceiptButton_Click(object sender, RoutedEventArgs e)
        {
            if (InvoicesDataGrid.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Выберите чек перед печатью", "Уведомление",
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

