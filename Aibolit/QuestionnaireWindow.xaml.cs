using System;
using System.Data;
using System.Text;
using System.Windows;
using Npgsql;

namespace Aibolit
{
    public partial class QuestionnaireWindow : Window
    {
        private readonly DatabaseHelper dbHelper;
        private readonly int appointmentId;
        private readonly int petId;
        private int? questionnaireId;

        public QuestionnaireWindow(DatabaseHelper dbHelper, int appointmentId, int petId)
        {
            InitializeComponent();
            this.dbHelper = dbHelper;
            this.appointmentId = appointmentId;
            this.petId = petId;

            LoadAppointmentInfo();
            LoadQuestionnaire();
        }

        private void LoadAppointmentInfo()
        {
            try
            {
                using (var conn = dbHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(@"
                        SELECT 
                            p.Name AS PetName,
                            p.View,
                            p.Species,
                            o.Surname || ' ' || o.Name || ' ' || COALESCE(o.Middle_Name, '') AS OwnerName,
                            v.Surname || ' ' || v.Name || ' ' || COALESCE(v.Middle_Name, '') AS VetName,
                            s.Name AS ServiceName,
                            TO_CHAR(a.Date, 'YYYY-MM-DD') AS VisitDate,
                            a.Start_Time_Appointment AS StartTime,
                            a.End_Time_Appointment AS EndTime
                        FROM Appointment a
                        JOIN Patient p ON a.ID_Pet = p.ID_Pet
                        JOIN Owner o ON p.ID_Owner = o.ID_Owner
                        JOIN Veterinarian v ON a.ID_Veterinarian = v.ID_Veterinarian
                        JOIN Service s ON a.ID_Service = s.ID_Service
                        WHERE a.ID_Appointment = @ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", appointmentId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var info = new StringBuilder();
                                info.AppendLine($"Пациент: {reader["PetName"]} ({reader["View"]}, {reader["Species"]})");
                                info.AppendLine($"Владелец: {reader["OwnerName"]}");
                                info.AppendLine($"Ветеринар: {reader["VetName"]}");
                                info.AppendLine($"Услуга: {reader["ServiceName"]}");
                                info.AppendLine($"Дата: {reader["VisitDate"]}, {reader["StartTime"]} - {reader["EndTime"]}");
                                AppointmentInfoTextBlock.Text = info.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить данные приёма: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadQuestionnaire()
        {
            try
            {
                using (var conn = dbHelper.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT ID_Questionnaire, Symptoms, Appointment_And_Treatment FROM Questionnaire WHERE ID_Appointment = @ID LIMIT 1",
                        conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", appointmentId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                questionnaireId = reader.GetInt32(0);
                                SymptomsTextBox.Text = reader["Symptoms"]?.ToString() ?? string.Empty;
                                TreatmentTextBox.Text = reader["Appointment_And_Treatment"]?.ToString() ?? string.Empty;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить карту: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string symptoms = SymptomsTextBox.Text.Trim();
            string treatment = TreatmentTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(symptoms) || string.IsNullOrWhiteSpace(treatment))
            {
                MessageBox.Show("Заполните симптомы и назначение", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conn = dbHelper.GetConnection())
                {
                    conn.Open();
                    if (questionnaireId.HasValue)
                    {
                        using (var cmd = new NpgsqlCommand(
                            "UPDATE Questionnaire SET Symptoms = @Symptoms, Appointment_And_Treatment = @Treatment " +
                            "WHERE ID_Questionnaire = @ID", conn))
                        {
                            cmd.Parameters.AddWithValue("@Symptoms", symptoms);
                            cmd.Parameters.AddWithValue("@Treatment", treatment);
                            cmd.Parameters.AddWithValue("@ID", questionnaireId.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (var cmd = new NpgsqlCommand(
                            "INSERT INTO Questionnaire (Symptoms, Appointment_And_Treatment, ID_Pet, ID_Appointment) " +
                            "VALUES (@Symptoms, @Treatment, @PetId, @AppointmentId)", conn))
                        {
                            cmd.Parameters.AddWithValue("@Symptoms", symptoms);
                            cmd.Parameters.AddWithValue("@Treatment", treatment);
                            cmd.Parameters.AddWithValue("@PetId", petId);
                            cmd.Parameters.AddWithValue("@AppointmentId", appointmentId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show("Карта сохранена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось сохранить карту: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

