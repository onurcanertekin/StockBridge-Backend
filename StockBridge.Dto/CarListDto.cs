namespace StockBridge.Dto
{
    internal class CarDto
    {
        public CarDto(string id,
            string uri,
            string title,
            string mileage,
            string price,
            string dealBadge,
            bool isSponsored,
            short imageCount,
            string stockType,
            DealerDto dealer)
        {
            Id = id;
            Uri = uri;
            Title = title;
            Mileage = mileage;
            Price = price;
            DealBadge = dealBadge;
            IsSponsored = isSponsored;
            ImageCount = imageCount;
            StockType = stockType;
            Dealer = dealer;
        }

        public string Id { get; set; }
        public string Uri { get; set; }
        public string Title { get; set; }
        public string Mileage { get; set; }
        public string Price { get; set; }
        public string DealBadge { get; set; }
        public bool IsSponsored { get; set; }
        public short ImageCount { get; set; }
        public string StockType { get; set; }
        public DealerDto Dealer { get; set; }
    }

    internal class DealerDto
    {
        public DealerDto(decimal rating, string reviewCount, string mileAwayFromUser)
        {
            Rating = rating;
            ReviewCount = reviewCount;
            MileAwayFromUser = mileAwayFromUser;
        }

        public string Name { get; set; }
        public decimal Rating { get; set; }
        public string ReviewCount { get; set; }
        public string MileAwayFromUser { get; set; }
    }
}