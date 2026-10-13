using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Windows;

namespace ShoeStore
{
    public partial class OrdersWindow : Window
    {
        private string roleName;

        public OrdersWindow(string role)
        {
            InitializeComponent();

            roleName = role;

            // Менеджер может смотреть заказы,
            // но CRUD доступен только Admin.
            if (roleName != "Admin")
            {
                AdminButtonsPanel.Visibility = Visibility.Collapsed;
            }

            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                List<OrderInfo> orders = new List<OrderInfo>();

                using SqlConnection connection = new SqlConnection(Database.ConnectionString);

                connection.Open();

                string query = @"
                    SELECT
                        o.IdOrder,
                        o.OrderDate,
                        o.IdClient,
                        c.ClientName,
                        o.IdPickup,
                        p.PickupAddress,
                        o.IdStatus,
                        s.StatusName
                    FROM Orders o
                    INNER JOIN Client c
                        ON o.IdClient = c.IdClient
                    INNER JOIN Pickup p
                        ON o.IdPickup = p.IdPickup
                    INNER JOIN Status s
                        ON o.IdStatus = s.IdStatus
                    ORDER BY o.IdOrder;
                ";

                using SqlCommand command = new SqlCommand(query, connection);
                using SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    OrderInfo order = new OrderInfo
                    {
                        IdOrder = Convert.ToInt32(reader["IdOrder"]),
                        OrderDate = Convert.ToDateTime(reader["OrderDate"]),

                        IdClient = Convert.ToInt32(reader["IdClient"]),
                        ClientName = reader["ClientName"].ToString() ?? string.Empty,

                        IdPickup = Convert.ToInt32(reader["IdPickup"]),
                        PickupAddress = reader["PickupAddress"].ToString() ?? string.Empty,

                        IdStatus = Convert.ToInt32(reader["IdStatus"]),
                        StatusName = reader["StatusName"].ToString() ?? string.Empty
                    };

                    orders.Add(order);
                }

                OrdersGrid.ItemsSource = orders;
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ошибка загрузки заказов",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void AddOrder_Click(object sender, RoutedEventArgs e)
        {
            OrderEditWindow window = new OrderEditWindow
            {
                Owner = this
            };

            if (window.ShowDialog() == true)
            {
                LoadOrders();
            }
        }

        private void EditOrder_Click(object sender, RoutedEventArgs e)
        {
            OrderInfo? selectedOrder = OrdersGrid.SelectedItem as OrderInfo;

            if (selectedOrder == null)
            {
                MessageBox.Show("Выберите заказ.");
                return;
            }

            OrderEditWindow window = new OrderEditWindow(selectedOrder)
            {
                Owner = this
            };

            if (window.ShowDialog() == true)
            {
                LoadOrders();
            }
        }
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
 

        private void DeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            OrderInfo? selectedOrder = OrdersGrid.SelectedItem as OrderInfo;

            if (selectedOrder == null)
            {
                MessageBox.Show("Выберите заказ.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Удалить выбранный заказ?",
                "Удаление",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                using SqlConnection connection = new SqlConnection(Database.ConnectionString);

                connection.Open();

                // Сначала удаляем позиции заказа,
                // затем сам заказ из-за внешнего ключа.
                string deleteItemsQuery = @"
                    DELETE FROM OrderItem
                    WHERE IdOrder = @IdOrder;
                ";

                using SqlCommand deleteItemsCommand = new SqlCommand(deleteItemsQuery, connection);

                deleteItemsCommand.Parameters.AddWithValue(
                    "@IdOrder",
                    selectedOrder.IdOrder
                );

                deleteItemsCommand.ExecuteNonQuery();

                string deleteOrderQuery = @"
                    DELETE FROM Orders
                    WHERE IdOrder = @IdOrder;
                ";

                using SqlCommand deleteOrderCommand = new SqlCommand(deleteOrderQuery, connection);

                deleteOrderCommand.Parameters.AddWithValue(
                    "@IdOrder",
                    selectedOrder.IdOrder
                );

                deleteOrderCommand.ExecuteNonQuery();

                LoadOrders();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ошибка удаления",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}