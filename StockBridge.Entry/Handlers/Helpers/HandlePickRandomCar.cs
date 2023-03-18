using StockBridge.Dto;

namespace StockBridge.Entry.Handlers.Helpers
{
    /// <summary>
    /// Pick a random car from given car list
    /// </summary>
    public static partial class HandlePickRandomCar
    {
        private static Random _rnd;

        /// <summary>
        /// Setter for _rnd
        /// </summary>
        /// <param name="rnd"></param>
        public static void SetRandom(Random rnd)
        {
            _rnd = rnd;
        }

        /// <summary>
        /// Picks a random car from given car list
        /// </summary>
        /// <param name="aboutCars"></param>
        /// <returns></returns>
        public static CarMinimizedDto PickRandomCar(List<CarMinimizedDto> CarList)
        {
            int r = _rnd.Next(CarList.Count);
            CarMinimizedDto selectedCar = CarList[r];
            return selectedCar;
        }
    }
}