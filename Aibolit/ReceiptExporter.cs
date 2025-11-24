using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace Aibolit
{
    public static class ReceiptExporter
    {
        public static string GenerateReceipt(DataRow row, DateTime printedAt)
        {
            if (row == null)
            {
                throw new ArgumentNullException(nameof(row));
            }

            string appointmentId = ExtractNumber(SafeGet(row, "ID_Appointment") ?? SafeGet(row, "Номер чека"));
            string date = CleanLabel(SafeGet(row, "Дата"));
            string vet = CleanLabel(SafeGet(row, "Ветеринар"));
            string service = CleanLabel(SafeGet(row, "Услуга"));
            string cost = CleanLabel(SafeGet(row, "Стоимость"));
            string pet = CleanLabel(SafeGet(row, "Питомец") ?? SafeGet(row, "Информация о питомце"));
            string owner = CleanLabel(SafeGet(row, "Владелец"));
            if (owner == "—" && pet.Contains("вид:"))
            {
                owner = ExtractOwnerFromPet(pet);
            }

            var sb = new StringBuilder();
            sb.AppendLine("================================");
            sb.AppendLine("        ВЕТКЛИНИКА «АЙБОЛИТ»");
            sb.AppendLine("================================");
            sb.AppendLine($"ЧЕК № {appointmentId}");
            sb.AppendLine($"Дата: {date} {printedAt:HH\\:mm}");
            sb.AppendLine("--------------------------------");
            sb.AppendLine($"Ветеринар : {vet}");
            sb.AppendLine($"Питомец   : {pet}");
            sb.AppendLine($"Владелец  : {owner}");
            sb.AppendLine("--------------------------------");
            sb.AppendLine($"Услуга    : {service}");
            sb.AppendLine($"Стоимость : {cost}");
            sb.AppendLine("--------------------------------");
            sb.AppendLine("Спасибо за визит!");
            sb.AppendLine("================================");

            var savePath = PromptSavePath(appointmentId);
            if (string.IsNullOrEmpty(savePath))
            {
                throw new InvalidOperationException("Сохранение отменено пользователем");
            }

            File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
            return savePath;
        }

        private static string SafeGet(DataRow row, string columnName)
        {
            if (row.Table.Columns.Contains(columnName) && row[columnName] != DBNull.Value)
            {
                return Convert.ToString(row[columnName], CultureInfo.InvariantCulture);
            }
            return null;
        }

        private static string CleanLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "—";
            }

            value = value.Trim();

            if (value.StartsWith("ЧЕК №", StringComparison.OrdinalIgnoreCase))
            {
                return value.Substring("ЧЕК №".Length).Trim();
            }

            int colonIndex = value.IndexOf(':');
            if (colonIndex >= 0 && colonIndex + 1 < value.Length)
            {
                return value.Substring(colonIndex + 1).Trim();
            }

            return value;
        }

        private static string ExtractNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "—";
            }

            value = value.Trim();
            if (value.StartsWith("ЧЕК №", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring("ЧЕК №".Length).Trim();
            }

            return value;
        }

        private static string ExtractOwnerFromPet(string pet)
        {
            const string marker = ") — ";
            var idx = pet.IndexOf(marker, StringComparison.Ordinal);
            if (idx >= 0 && idx + marker.Length < pet.Length)
            {
                return pet.Substring(idx + marker.Length).Trim();
            }
            return "—";
        }

        private static string PromptSavePath(string appointmentId)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Сохранение чека",
                Filter = "Текстовый файл (*.txt)|*.txt",
                FileName = $"receipt_{appointmentId}_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                AddExtension = true,
                OverwritePrompt = true
            };

            bool? result = dialog.ShowDialog();
            return result == true ? dialog.FileName : null;
        }
    }
}

