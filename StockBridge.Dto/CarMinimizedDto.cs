namespace StockBridge.Dto
{
    public class CarMinimizedDto : CarBaseDto
    {
        public string Id { get; set; }
        public bool IsSponsored { get; set; }
        public DealerDto Dealer { get; set; }
    }

    public class DealerDto
    {
        public string Name { get; set; }
        public decimal? Rating { get; set; }
    }
}