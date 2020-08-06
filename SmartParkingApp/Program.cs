using System;
using System.Collections.Generic;
using System.Text;

namespace ParkingApp
{
    class Program
    {
        static void Main(string[] args)
        {
            /*Просто чтобы красивый символ рубля отображался в консоли*/
            Console.OutputEncoding = Encoding.UTF8;

            List<User> users = new List<User>()
            {
                new User("Peter", "a123", "1234567890"),
                new User("John", "b123", "0987654321"),
                new User("May", "c123", "6789012345"),
                new User("Kate", "d123", "4567812390"),
            };
            /*Инициализирую менеджер, передаю ему параметрами пути к файлам данных. Если их нет, то менеджер их создаст
            по указанным путям*/
            ParkingManager parkingManager = new ParkingManager("AllData.dat");
            parkingManager.Register(users);
            /*Тут реализована логика управления парковкой через консоль. В принципе, по названиям команд понятно, что они делают*/
            string command = "";
            while (command != "end")
            {
                Console.WriteLine("Main Menu (start, end, show)");
                command = Console.ReadLine();
                switch(command)
                {
                    case "start":
                        {
                            Console.WriteLine("Write car plate number:");
                            var carPlateNumber = Console.ReadLine();
                            if (carPlateNumber == "")
                            {
                                Console.WriteLine("Incorrect Input!");
                                break;
                            }
                            Start(parkingManager, carPlateNumber);

                            break;
                        }
                    case "end":
                        {
                            break;
                        }
                    case "show":
                        {
                            Console.WriteLine("Which information do you need (active, completed, tariff, registered)?");
                            string showCommand = Console.ReadLine();
                            
                            switch(showCommand)
                            {
                                case "active":
                                    {
                                        for (int i = 0; i < parkingManager.ActiveSessions.Count; i++)
                                        {
                                            parkingManager.ActiveSessions[i].Print(i);
                                        }
                                        break;
                                    }
                                case "completed":
                                    {
                                        for (int i = 0; i < parkingManager.CompletedSessions.Count; i++)
                                        {
                                            parkingManager.CompletedSessions[i].Print(i);
                                        }
                                        break;
                                    }
                                case "tariff":
                                    {
                                        parkingManager.PrintTariffs();
                                        break;
                                    }
                                case "registered":
                                    {
                                        parkingManager.PrintRegistered();
                                        break;
                                    }
                            }
                            break;
                        }
                    default:
                        Console.WriteLine("Incorrect command!");
                        break;
                }
            }
        }

        /*Это метод начала работы с сессией через консоль. В принципе, названия команд говорят за себя. Но есть один неочевидный момент
        Во многих местах есть всякие изменения времени. Это нужно, чтобы вход на парковку, выезд и прочее
        происходило не в текущее время системы, а в заданное с клавиатуры. В реальной парковке это, разумеется, не нужно*/
        static private void Start(ParkingManager manager, string carPlateNumber)
        {
            var currentSession = manager.FindSession(carPlateNumber, out _);
            if (currentSession == null)
            {
                currentSession = manager.EnterParking(carPlateNumber);
                if (currentSession == null)
                {
                    Console.WriteLine("Error");
                    return;
                }
                Console.WriteLine("When did the car enter?");
                currentSession.EntryDt = OverwriteTime();
            }

            Console.WriteLine("Car entried at {0}", currentSession.EntryDt);
            var ticket = currentSession.TicketNumber;
            var command = "";
            while (command != "complete")
            {
                Console.WriteLine("Session Menu\nCommands: exit ticket, exit number, pay, return");
                command = Console.ReadLine();
                switch (command)
                {
                    case "exit ticket":
                        {
                            Console.WriteLine("When did the car try exit?");
                            DateTime exitDt = OverwriteTime();
                            TimeSpan span = DateTime.Now - exitDt;
                            if (currentSession.PaymentDt == null)
                            {
                                currentSession.EntryDt += span;
                                if (manager.TryLeaveParkingWithTicket(ticket, out var session))
                                {
                                    currentSession.ExitDt = exitDt;
                                    Console.WriteLine("Car exits parking. Date: {0}", currentSession.ExitDt);
                                    command = "complete";
                                }
                                else
                                {
                                    Console.WriteLine("Car can not exit parking. Date: {0}", exitDt);
                                }
                                currentSession.EntryDt -= span;
                            }
                            else
                            {
                                currentSession.PaymentDt += span;
                                if (manager.TryLeaveParkingWithTicket(ticket, out var session))
                                {
                                    currentSession.ExitDt = exitDt;
                                    Console.WriteLine("Car exits parking. Date: {0}", currentSession.ExitDt);
                                    command = "complete";
                                }
                                else
                                {
                                    Console.WriteLine("Car can not exit parking. Date: {0}", exitDt);
                                }
                                currentSession.PaymentDt -= span;
                            }
                            

                            break;
                        }
                    case "exit number":
                        {
                            if (currentSession.User != null)
                            {
                                Console.WriteLine("As registered user, you shouldn't pay at kiosk. You can exit by ncar plate number (exit number)");
                                break;
                            }
                            Console.WriteLine("When did the car try exit?");
                            DateTime exitDt = OverwriteTime();
                            TimeSpan span = DateTime.Now - exitDt;
                            currentSession.EntryDt += span;
                            if (manager.TryLeaveParkingByCarPlateNumber(carPlateNumber, out _))
                            {
                                currentSession.ExitDt = exitDt;
                                Console.WriteLine("Car exits parking. Date: {0}", currentSession.ExitDt);
                                command = "complete";
                            }
                            else
                            {
                                Console.WriteLine("Car can not exit parking. Date: {0}", exitDt);
                            }
                            currentSession.EntryDt -= span;

                            break;
                        }
                    case "pay":
                        {
                            decimal payment = 0m;
                            Console.WriteLine("When driver paid for parking?");
                            DateTime payingDt = OverwriteTime();
                            TimeSpan span = DateTime.Now - payingDt;
                            if (currentSession.PaymentDt == null)
                            {
                                currentSession.EntryDt += span;
                                payment = manager.GetRemainingCost(ticket);
                                manager.PayForParking(ticket, payment);
                                currentSession.EntryDt -= span;
                                currentSession.PaymentDt = payingDt;
                            }
                            else
                            {
                                currentSession.PaymentDt += span;
                                payment = manager.GetRemainingCost(ticket);
                                manager.PayForParking(ticket, payment);
                                currentSession.PaymentDt = payingDt;
                            }
                            Console.WriteLine("Driver paid for session Date: {0}. Cost: {1:C}", currentSession.PaymentDt, payment);
                            break;
                        }
                    case "return":
                        {
                            Console.WriteLine("Return to main menu");
                            command = "complete";
                            break;
                        }
                    default:
                        Console.WriteLine("Incorrect command!");
                        break;

                }
                manager.Saving();
            }
            

        }
        /*Метод изменения значения переменной DateTime на ввод с клавиатуры. Код много где повторялся бы, если бы я
        не вынес это в отдельный метод*/
        static private DateTime OverwriteTime()
        {
            DateTime inputDt;
            if (DateTime.TryParse(Console.ReadLine(), out inputDt))
            {
                return inputDt;
            }
            else
            {
                Console.WriteLine("Incorrect input");
                return DateTime.Now;
            }
        }
    }
}