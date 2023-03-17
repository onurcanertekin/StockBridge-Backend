namespace StockBridge.Dto
{
    public class CarDetailedDto : CarBaseDto
    {
        public string EstimatedMonthlyPayment { get; set; }
        public string SellerContactPhone { get; set; }
        public CarBasicsDto CarBasics { get; set; }
        public CarFeaturesDto CarFeatures { get; set; }
        public CarHistoryDto CarHistory { get; set; }
    }

    public class CarBasicsDto
    {
        public string ExteriorColor { get; set; }
        public string InteriorColor { get; set; }
        public string DriveTrain { get; set; }
        public string FuelType { get; set; }
        public string Transmission { get; set; }
        public string Engine { get; set; }
        public string VIN { get; set; }
        public string Stock { get; set; }
        public string Mileage { get; set; }
    }

    public class CarFeaturesDto
    {
        public List<string> Convenience { get; set; }
        public List<string> Entertainment { get; set; }
        public List<string> Exterior { get; set; }
        public List<string> Safety { get; set; }
        public List<string> Seating { get; set; }
        public List<string> AdditionalPopularFeatures { get; set; }
    }

    public class CarHistoryDto
    {
        public string AccidentsOrDamage { get; set; }
        public string FirstOwnerVehicle { get; set; }
        public string PersonelUseOnly { get; set; }
    }
}