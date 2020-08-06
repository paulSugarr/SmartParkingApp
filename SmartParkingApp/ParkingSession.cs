using System;

namespace ParkingApp
{
    [Serializable]
    class ParkingSession
    {

        // Date and time of arriving at the parking
        public DateTime EntryDt { get; set; }
        // Date and time of payment for the parking
        public DateTime? PaymentDt { get; set; }
        // Date and time of exiting the parking
        public DateTime? ExitDt { get; set; }
        // Total cost of parking
        public decimal? TotalPayment { get; set; }
        // Plate number of the visitor's car
        public string CarPlateNumber { get; set; }
        // Issued printed ticket
        public int TicketNumber { get; set; }
        public User User { get; set; }
        public ParkingSession(string carPlateNumber, DateTime entryDt, int ticketNumber)
        {
            CarPlateNumber = carPlateNumber;
            EntryDt = entryDt;
            TicketNumber = ticketNumber;
        }
        public ParkingSession()
        {

        }
        public void Print(int number)
        {
            Console.WriteLine("{0}) Car: {1} | Ticket: {2}| Entry: {3}| Exit: {4} | Payment: {5}  | Total Pay: {6}", number, CarPlateNumber, TicketNumber, EntryDt, ExitDt, PaymentDt, TotalPayment);
        }
        public void Print()
        {
            Console.WriteLine("Car: {0} | Ticket: {1}| Entry: {2} | Exit: {3} | Payment: {4}  | Total Pay: {5}", CarPlateNumber, TicketNumber, EntryDt, ExitDt, PaymentDt, TotalPayment);
        }
    }
}
