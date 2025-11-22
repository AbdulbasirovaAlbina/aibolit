using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Aibolit
{
    public partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void OwnersPetsButton_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new OwnersPetsPage());
        }

        private void AppointmentsButton_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new AppointmentsPage());
        }

        private void PatientCardsButton_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new PatientCardsPage());
        }

        private void InvoicesButton_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new InvoicesPage());
        }

        private void StatisticsButton_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new StatisticsPage());
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null)
            {
                NavigationService.Navigate(new LoginPage());
            }
        }
    }
}


