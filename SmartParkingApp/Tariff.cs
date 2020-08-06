using System;
namespace ParkingApp
{
    [Serializable]
    class Tariff
    {
        public int Minutes { get; set; }
        public decimal Rate { get; set; }
        public Tariff(int minutes, decimal rate)
        {
            Minutes = minutes;
            Rate = rate;
        }
    }
}
