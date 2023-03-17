namespace StockBridge.Dto
{
    public class CarDto
    {
        public string Id { get; set; }
        public string Uri { get; set; }
        public string Title { get; set; }
        public string Mileage { get; set; }
        public string Price { get; set; }
        public string DealBadge { get; set; }
        public bool IsSponsored { get; set; }
        public short? ImageCount { get; set; }
        public string StockType { get; set; }
        public DealerDto Dealer { get; set; }
    }

    public class DealerDto
    {
        public string Name { get; set; }
        public decimal? Rating { get; set; }
    }
}