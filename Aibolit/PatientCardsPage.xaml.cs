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
            Loaded += PatientCardsPage_Loaded;
        }

        private void PatientCardsPage_Loaded(object sender, RoutedEventArgs e) => LoadData();

        private void LoadData()
        {
            try
            {
                string query = @"
                    SELECT 
                        p.Name AS Имя_Питомца,
                        p.View AS Вид,
                        p.Species AS Порода,
                        p.Color AS Цвет,
                        TO_CHAR(p.Year_Of_Birth, 'YYYY-MM-DD') AS Дата_Рождения,
                        o.Surname || ' ' || o.Name || ' ' || COALESCE(o.Middle_Name, '') AS Владелец,
                        o.Phone AS Телефон,
                        o.Address AS Адрес,
                        COALESCE(o.Email, 'не указан') AS Email,
                        COALESCE(STRING_AGG(
                            TO_CHAR(a.Date, 'YYYY-MM-DD') || ': ' || s.Name || ' (' || s.Cost || ' руб.)', 
                            '; ' ORDER BY a.Date DESC
                        ), 'нет услуг') AS Услуги,
                        COALESCE(STRING_AGG(
                            TO_CHAR(a.Date, 'YYYY-MM-DD') || ': ' || q.Symptoms, 
                            '; ' ORDER BY a.Date DESC
                        ), 'нет симптомов') AS Симптомы,
                        COALESCE(STRING_AGG(
                            TO_CHAR(a.Date, 'YYYY-MM-DD') || ': ' || q.Appointment_And_Treatment, 
                            '; ' ORDER BY a.Date DESC
                        ), 'нет лечения') AS Лечение
                    FROM Patient p
                    JOIN Owner o ON p.ID_Owner = o.ID_Owner
                    LEFT JOIN Questionnaire q ON p.ID_Pet = q.ID_Pet
                    LEFT JOIN Appointment a ON q.ID_Appointment = a.ID_Appointment
                    LEFT JOIN Service s ON a.ID_Service = s.ID_Service
                    GROUP BY p.ID_Pet, p.Name, p.View, p.Species, p.Color, p.Year_Of_Birth, 
                             o.Surname, o.Name, o.Middle_Name, o.Phone, o.Address, o.Email
                    ORDER BY p.Name";
                
                var dataTable = dbHelper.ExecuteQuery(query);
                PatientCardsDataGrid.ItemsSource = dataTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PatientCardsDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            DataGridColumnFormatter.Apply(e);
        }
    }
}

