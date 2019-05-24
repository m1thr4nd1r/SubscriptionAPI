namespace SubscriptionAPI.Models
{
    public class Subscription
    {
        public string Itens { get; set; }
        public string Customer { get; set; }
        public string Interval { get; set; }
        public string Interval_Count { get; set; }
        public string Payment_Method { get; set; }
        public string Payment_Data { get; set; }
        public string Start_At { get; set; }
        public string Billing_Type { get; set; }
        public string Installments { get; set; }
    }
}
