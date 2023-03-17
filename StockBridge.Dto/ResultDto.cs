namespace StockBridge.Dto
{
    public class ResultDto
    {
        public ResultDto()
        {
            AllTesla = new();
            TeslaModelX = new();
        }

        public CarListAndCarDetailWithNotableHighlights AllTesla { get; set; }
        public CarListAndCarDetailWithNotableHighlights TeslaModelX { get; set; }
    }

    public class CarListAndCarDetailWithNotableHighlights
    {
        public CarListAndCarDetailWithNotableHighlights()
        {
            CarDetail = new();
            CarList = new();
            NotableHighlights = new();
        }

        public CarDetailedDto CarDetail { get; set; }
        public List<CarMinimizedDto> CarList { get; set; }
        public NotableHighlightsDto NotableHighlights { get; set; }
    }
}