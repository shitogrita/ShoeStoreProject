using Microsoft.Data.SqlClient;
using System;
using System.Windows;

namespace ShoeStore
{
    public partial class OrderEditWindow : Window
    {
        private OrderInfo? editingOrder;

        public OrderEditWindow()
        {
            InitializeComponent();

            try
            {
                LoadClients();
                LoadPickups();
                LoadStatuses();

                OrderDatePicker.SelectedDate = DateTime.Today;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ошибка загрузки данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }


        public OrderEditWindow(OrderInfo order)
        {
            InitializeComponent();

            editingOrder = order;

            try
            {
                LoadClients();
                LoadPickups();
                LoadStatuses();

                TitleTextBlock.Text = "Редактирование заказа";

                OrderIdTextBox.Text = order.IdOrder.ToString();
                OrderDatePicker.SelectedDate = order.OrderDate;

                ClientComboBox.SelectedValue = order.IdClient;
                PickupComboBox.SelectedValue = order.IdPickup;
                StatusComboBox.SelectedValue = order.IdStatus;

                OrderIdTextBox.IsReadOnly = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ошибка загрузки данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }


        private void LoadClients()
        {
            ClientComboBox.Items.Clear();

            using SqlConnection connection = new SqlConnection(Database.ConnectionString);

            connection.Open();

            string query = @"
                SELECT
                    IdClient,
                    ClientName
                FROM Client
                ORDER BY ClientName;
            ";

            using SqlCommand command = new SqlCommand(query, connection);
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                LookupItem item = new LookupItem
                {
                    Id = Convert.ToInt32(reader["IdClient"]),
                    Name = reader["ClientName"].ToString() ?? string.Empty
                };

                ClientComboBox.Items.Add(item);
            }

            if (ClientComboBox.Items.Count > 0)
            {
                ClientComboBox.SelectedIndex = 0;
            }
        }

        private void LoadPickups()
        {
            PickupComboBox.Items.Clear();

            using SqlConnection connection = new SqlConnection(Database.ConnectionString);

            connection.Open();

            string query = @"
                SELECT
                    IdPickup,
                    PickupAddress
                FROM Pickup
                ORDER BY PickupAddress;
            ";

            using SqlCommand command = new SqlCommand(query, connection);
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                LookupItem item = new LookupItem
                {
                    Id = Convert.ToInt32(reader["IdPickup"]),
                    Name = reader["PickupAddress"].ToString() ?? string.Empty
                };

                PickupComboBox.Items.Add(item);
            }

            if (PickupComboBox.Items.Count > 0)
            {
                PickupComboBox.SelectedIndex = 0;
            }
        }

        private void LoadStatuses()
        {
            StatusComboBox.Items.Clear();

            using SqlConnection connection = new SqlConnection(Database.ConnectionString);

            connection.Open();

            string query = @"
                SELECT
                    IdStatus,
                    StatusName
                FROM Status
                ORDER BY StatusName;
            ";

            using SqlCommand command = new SqlCommand(query, connection);
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                LookupItem item = new LookupItem
                {
                    Id = Convert.ToInt32(reader["IdStatus"]),
                    Name = reader["StatusName"].ToString() ?? string.Empty
                };

                StatusComboBox.Items.Add(item);
            }

            if (StatusComboBox.Items.Count > 0)
            {
                StatusComboBox.SelectedIndex = 0;
            }
        }

        private void SaveOrder_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(OrderIdTextBox.Text, out int orderId))
            {
                MessageBox.Show("Некорректный номер заказа.");
                return;
            }

            if (OrderDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату.");
                return;
            }

            if (ClientComboBox.SelectedValue == null ||
                PickupComboBox.SelectedValue == null ||
                StatusComboBox.SelectedValue == null)
            {
                MessageBox.Show("Заполните все поля.");
                return;
            }

            int clientId = Convert.ToInt32(ClientComboBox.SelectedValue);
            int pickupId = Convert.ToInt32(PickupComboBox.SelectedValue);
            int statusId = Convert.ToInt32(StatusComboBox.SelectedValue);

            DateTime orderDate = OrderDatePicker.SelectedDate.Value;

            try
            {
                using SqlConnection connection = new SqlConnection(Database.ConnectionString);

                connection.Open();

                if (editingOrder == null)
                {
                    string query = @"
                        INSERT INTO Orders
                        (
                            IdOrder,
                            OrderDate,
                            IdClient,
                            IdPickup,
                            IdStatus
                        )
                        VALUES
                        (
                            @IdOrder,
                            @OrderDate,
                            @IdClient,
                            @IdPickup,
                            @IdStatus
                        );
                    ";

                    using SqlCommand command = new SqlCommand(query, connection);

                    command.Parameters.AddWithValue("@IdOrder", orderId);
                    command.Parameters.AddWithValue("@OrderDate", orderDate);
                    command.Parameters.AddWithValue("@IdClient", clientId);
                    command.Parameters.AddWithValue("@IdPickup", pickupId);
                    command.Parameters.AddWithValue("@IdStatus", statusId);

                    command.ExecuteNonQuery();
                }
                else
                {
                    string query = @"
                        UPDATE Orders
                        SET
                            OrderDate = @OrderDate,
                            IdClient = @IdClient,
                            IdPickup = @IdPickup,
                            IdStatus = @IdStatus
                        WHERE IdOrder = @IdOrder;
                    ";

                    using SqlCommand command = new SqlCommand(query, connection);

                    command.Parameters.AddWithValue("@IdOrder", editingOrder.IdOrder);
                    command.Parameters.AddWithValue("@OrderDate", orderDate);
                    command.Parameters.AddWithValue("@IdClient", clientId);
                    command.Parameters.AddWithValue("@IdPickup", pickupId);
                    command.Parameters.AddWithValue("@IdStatus", statusId);

                    command.ExecuteNonQuery();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ошибка базы данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}