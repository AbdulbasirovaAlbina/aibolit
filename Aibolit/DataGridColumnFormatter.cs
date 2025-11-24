using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Aibolit
{
    public static class DataGridColumnFormatter
    {
        public static void Apply(DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e == null)
            {
                return;
            }

            var header = e.Column.Header?.ToString() ?? string.Empty;

            if (ShouldHideColumn(header))
            {
                e.Column.Visibility = Visibility.Collapsed;
                return;
            }

            e.Column.Header = FormatHeader(header);
        }

        private static bool ShouldHideColumn(string header)
        {
            return !string.IsNullOrWhiteSpace(header) &&
                   header.IndexOf("id", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FormatHeader(string header)
        {
            if (string.IsNullOrWhiteSpace(header))
            {
                return header;
            }

            header = header.Replace("_", " ");
            var builder = new StringBuilder(header.Length * 2);
            char? previous = null;

            foreach (var symbol in header)
            {
                if (NeedsSpace(previous, symbol, builder))
                {
                    builder.Append(' ');
                }

                builder.Append(symbol);
                previous = symbol;
            }

            return NormalizeSpaces(builder.ToString());
        }

        private static bool NeedsSpace(char? previous, char current, StringBuilder builder)
        {
            if (!previous.HasValue)
            {
                return false;
            }

            if (previous.Value == ' ')
            {
                return false;
            }

            return char.IsLetter(previous.Value) &&
                   char.IsLower(previous.Value) &&
                   char.IsLetter(current) &&
                   char.IsUpper(current);
        }

        private static string NormalizeSpaces(string value)
        {
            var builder = new StringBuilder(value.Length);
            bool previousSpace = false;

            foreach (var symbol in value)
            {
                if (char.IsWhiteSpace(symbol))
                {
                    if (!previousSpace)
                    {
                        builder.Append(' ');
                        previousSpace = true;
                    }
                }
                else
                {
                    builder.Append(symbol);
                    previousSpace = false;
                }
            }

            return builder.ToString().Trim();
        }
    }
}

