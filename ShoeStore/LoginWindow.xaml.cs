using Microsoft.Data.SqlClient;
using System.Windows;

namespace ShoeStore
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordTextBox.Password;

            if (string.IsNullOrWhiteSpace(login) ||
                string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(
                    "Введите логин и пароль.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                return;
            }

            try
            {
                using SqlConnection connection = new SqlConnection(Database.ConnectionString);

                connection.Open();

                string query = @"
                    SELECT
                        u.FullName,
                        r.RoleName
                    FROM AppUser u
                    INNER JOIN Role r
                        ON u.IdRole = r.IdRole
                    WHERE u.Login = @Login
                      AND u.Password = @Password;
                ";

                using SqlCommand command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@Login", login);
                command.Parameters.AddWithValue("@Password", password);

                using SqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    string fullName =
                        reader["FullName"].ToString() ?? string.Empty;

                    string roleName =
                        reader["RoleName"].ToString() ?? string.Empty;

                    MainWindow window =
                        new MainWindow(fullName, roleName);

                    window.Show();

                    Close();
                }
                else
                {
                    MessageBox.Show(
                        "Неверный логин или пароль.",
                        "Ошибка авторизации",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ошибка базы данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void Guest_Click(object sender, RoutedEventArgs e)
        {
            MainWindow window =
                new MainWindow("Гость", "Guest");

            window.Show();

            Close();
        }
    }
}
