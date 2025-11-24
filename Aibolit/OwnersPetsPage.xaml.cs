using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace Aibolit
{
    public partial class OwnersPetsPage : Page
    {
        private readonly DatabaseHelper dbHelper;
        private DataTable ownersTable;
        private DataTable petsTable;
        private DataView ownersView;
        private DataView petsView;
        private int? ownerFilterId;
        private int? petOwnerFilterId;

        public OwnersPetsPage()
        {
            InitializeComponent();
            dbHelper = new DatabaseHelper();
            Loaded += OwnersPetsPage_Loaded;
        }

        private void OwnersPetsPage_Loaded(object sender, RoutedEventArgs e) => LoadData();

        private void LoadData()
        {
            try
            {
                ownersTable = dbHelper.ExecuteQuery(@"
                    SELECT 
                        o.ID_Owner,
                        o.Surname AS Фамилия,
                        o.Name AS Имя,
                        COALESCE(o.Middle_Name, '') AS Отчество,
                        o.Phone AS Телефон,
                        o.Address AS Адрес,
                        COALESCE(o.Email, 'не указан') AS Email,
                        COUNT(p.ID_Pet) AS ""Количество питомцев""
                    FROM Owner o
                    LEFT JOIN Patient p ON o.ID_Owner = p.ID_Owner
                    GROUP BY o.ID_Owner, o.Surname, o.Name, o.Middle_Name, o.Phone, o.Address, o.Email
                    ORDER BY o.Surname, o.Name");

                petsTable = dbHelper.ExecuteQuery(@"
                    SELECT 
                        p.ID_Pet,
                        p.Name AS Имя_Питомца,
                        p.View AS Вид,
                        p.Species AS Порода,
                        TO_CHAR(p.Year_Of_Birth, 'YYYY-MM-DD') AS Дата_Рождения,
                        p.Color AS Цвет,
                        p.ID_Owner,
                        o.Surname AS Фамилия_Владельца,
                        o.Name AS Имя_Владельца,
                        COALESCE(o.Middle_Name, '') AS Отчество_Владельца,
                        o.Phone AS Телефон_Владельца,
                        o.Address AS Адрес_Владельца
                    FROM Patient p
                    JOIN Owner o ON p.ID_Owner = o.ID_Owner
                    ORDER BY o.Surname, p.Name");

                ownersView = ownersTable.DefaultView;
                petsView = petsTable.DefaultView;

                OwnersDataGrid.ItemsSource = ownersView;
                PetsDataGrid.ItemsSource = petsView;

                ownerFilterId = null;
                petOwnerFilterId = null;

                ApplyOwnerFilters();
                ApplyPetFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddOwnerPetButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddOwnerPetWindow(dbHelper);
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void OwnerSearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyOwnerFilters();

        private void PetSearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyPetFilters();

        private void ApplyOwnerFilters()
        {
            if (ownersView == null)
            {
                return;
            }

            var filters = new List<string>();
            var search = OwnerSearchTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                var escaped = search.Replace("'", "''");
                filters.Add($"([Фамилия] LIKE '%{escaped}%' OR [Имя] LIKE '%{escaped}%' OR [Телефон] LIKE '%{escaped}%')");
            }

            if (petOwnerFilterId.HasValue)
            {
                filters.Add($"[ID_Owner] = {petOwnerFilterId.Value}");
            }

            ownersView.RowFilter = string.Join(" AND ", filters);
        }

        private void ApplyPetFilters()
        {
            if (petsView == null)
            {
                return;
            }

            var filters = new List<string>();
            var search = PetSearchTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                var escaped = search.Replace("'", "''");
                filters.Add($"([Имя_Питомца] LIKE '%{escaped}%' OR [Вид] LIKE '%{escaped}%' OR [Порода] LIKE '%{escaped}%')");
            }

            if (ownerFilterId.HasValue)
            {
                filters.Add($"[ID_Owner] = {ownerFilterId.Value}");
            }

            petsView.RowFilter = string.Join(" AND ", filters);
        }

        private void OwnersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OwnersDataGrid.SelectedItem is DataRowView rowView)
            {
                ownerFilterId = Convert.ToInt32(rowView["ID_Owner"]);
            }
            else
            {
                ownerFilterId = null;
            }

            ApplyPetFilters();
        }

        private void PetsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PetsDataGrid.SelectedItem is DataRowView rowView)
            {
                petOwnerFilterId = Convert.ToInt32(rowView["ID_Owner"]);
            }
            else
            {
                petOwnerFilterId = null;
            }

            ApplyOwnerFilters();
            HighlightOwnerFromPet();
        }

        private void HighlightOwnerFromPet()
        {
            if (!petOwnerFilterId.HasValue || ownersView == null)
            {
                OwnersDataGrid.SelectedItem = null;
                return;
            }

            foreach (DataRowView row in ownersView)
            {
                if (Convert.ToInt32(row["ID_Owner"]) == petOwnerFilterId.Value)
                {
                    OwnersDataGrid.SelectedItem = row;
                    OwnersDataGrid.ScrollIntoView(row);
                    return;
                }
            }
        }

        private void PetsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PetsDataGrid.SelectedItem is DataRowView rowView)
            {
                try
                {
                    var dialog = new EditOwnerPetWindow(dbHelper, rowView.Row);
                    if (dialog.ShowDialog() == true)
                    {
                        LoadData();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии окна редактирования: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            OwnerSearchTextBox.Text = string.Empty;
            PetSearchTextBox.Text = string.Empty;
            ownerFilterId = null;
            petOwnerFilterId = null;
            OwnersDataGrid.SelectedItem = null;
            PetsDataGrid.SelectedItem = null;
            ApplyOwnerFilters();
            ApplyPetFilters();
        }

        private void OwnersDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e) =>
            DataGridColumnFormatter.Apply(e);

        private void PetsDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e) =>
            DataGridColumnFormatter.Apply(e);
    }
}

