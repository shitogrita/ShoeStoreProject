using System;

namespace ShoeStore
{
    public class OrderInfo
    {
        public int IdOrder { get; set; }

        public DateTime OrderDate { get; set; }

        public int IdClient { get; set; }
        public string ClientName { get; set; } = string.Empty;

        public int IdPickup { get; set; }
        public string PickupAddress { get; set; } = string.Empty;

        public int IdStatus { get; set; }
        public string StatusName { get; set; } = string.Empty;
    }
}
