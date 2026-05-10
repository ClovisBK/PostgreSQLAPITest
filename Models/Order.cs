namespace ProductRepositoryPattern.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
