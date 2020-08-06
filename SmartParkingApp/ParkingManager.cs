using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ParkingApp
{
    class ParkingManager
    {
        #region <Fields>
        private int _numberOfSessions = 0;
        private List<Tariff> _tariffList = new List<Tariff>();
        #endregion

        #region <Properties>
        public int ParkingCapacity { get; set; }
        public string AllDataPath { get; set; }
        public int FreeExit { get; private set; }
        public List<Tariff> TariffList
        {
            get
            {
                return _tariffList;
            }
            set
            {
                _tariffList = value;
                FreeExit = _tariffList.Count > 0 ? _tariffList[0].Minutes : FreeExit = Int32.MaxValue; ;
                Saving();

            }
        }
        public List<ParkingSession> ActiveSessions { get; private set; } = new List<ParkingSession>();
        public List<ParkingSession> CompletedSessions { get; private set; } = new List<ParkingSession>();
        public List<User> RegisteredUsers { get; private set; } = new List<User>();
        #endregion


        /*Конструктор этого класса, загружает данные из бинарника, если данных нет, то генерирует стандартные значения*/
        public ParkingManager(string pathAllData)
        {
            ParkingCapacity = Int32.MaxValue;
            AllDataPath = pathAllData;
            Loading();
            if (_tariffList.Count == 0)
            {
                for (int i = 0; i < 10; i++)
                {
                    _tariffList.Add(new Tariff(15 * (i + 1), 50 * i));
                }
                FreeExit = _tariffList[0].Minutes;
            }
            if (_numberOfSessions == 0)
            {
                _numberOfSessions = ActiveSessions.Count + CompletedSessions.Count;
            }
        }


        #region <Methods from template>
        /* BASIC PART */
        public ParkingSession EnterParking(string carPlateNumber)
        {
            /*Тут вернем null, если такой автомобиль уже есть на парковке*/
            if (FindSession(carPlateNumber, out _) != null)
            {
                Console.WriteLine("<Error> The car is already on the parking.");
                return null;
            }

            /*Тут верну null, если парковка заполнена*/
            if (ActiveSessions.Count >= ParkingCapacity)
            {
                Console.WriteLine("<Error> Parking is full");
                return null;
            }
            /*Тут сама логика входа на парковку - вызываю конструктор для ParkingSession,
             увеличиваю количество сессий на парковке*/

            ParkingSession session = new ParkingSession(carPlateNumber, DateTime.Now, _numberOfSessions);
            _numberOfSessions++;
            /*Если пользователь зарегистрирован, в соотв. поле текущего объекта
            класса ParkingSession запишу найденную информацию о пользователе*/

            for (int i = 0; i < RegisteredUsers.Count; i++)
            {
                if (RegisteredUsers[i].CarPlateNumber == carPlateNumber)
                {
                    session.User = RegisteredUsers[i];
                }
            }
            /*Добавим авто в список активных сессий, пересохраним данные в файлы и вернем эту сессию*/
            ActiveSessions.Add(session);
            Saving();
            return session;
        }
        public bool TryLeaveParkingWithTicket(int ticketNumber, out ParkingSession session)
        {
            /*Инициализируем нужные переменные, с помощью метода FindSessionByTicket
            найдем данную сессию в списке активных парковок*/
            int numberInList;
            session = FindSession(ticketNumber, out numberInList);
            /*Если не найдем ничего, то не дадим покинуть парковку и занулим возвращаемую сессию, согласно заданию*/
            if (session == null)
            {
                Console.WriteLine("<Error> Session not found");
                session = null;
                return false;
            }

            /*Тут для простоты кода объявляю переменную payDt
             В нее запишу время последнего платежа
             Если платежа не было, то запишем время входа на парковку*/
            DateTime payDt = session.PaymentDt == null ? session.EntryDt : (DateTime)session.PaymentDt;
            Console.WriteLine(payDt.AddMinutes(FreeExit));
            /*Если укладываемся во время свободного выезда, то разрешаем выезд,
            возвращаем true, переместим данную сессию из активных в прошедшие
            и пересохраним данные в файлах (данная логика реализована в методе Exit)*/
            if (payDt.AddMinutes(FreeExit) > DateTime.Now)
            {
                Exit(session, numberInList);
                return true;
            }
            /*В остальных случая выехать не даем*/
            session = null;
            return false;
        }
        public decimal GetRemainingCost(int ticketNumber)
        {
            /*Найдем нужную сессию. Если ничего не найдем, то плата - 0*/
            ParkingSession currentSession = FindSession(ticketNumber, out _);
            if (currentSession == null)
            {
                return 0m;
            }

            /*Объявим time, куда запишем интервал времени, прошедший с последнего платежа.
            Если платежа не было, то считаем от времени входа на парковку*/
            TimeSpan time = currentSession.PaymentDt != null ? DateTime.Now - (DateTime)currentSession.PaymentDt : DateTime.Now - currentSession.EntryDt;

            /*Если время свободного выезда, возвращаем 0*/
            if (time.TotalMinutes <= FreeExit)
            {
                return 0m;
            }
            /*Перебираем таблицу тарифов, чтобы понять итоговую цену*/
            for (int i = 1; i < TariffList.Count; i++)
            {
                if (time.TotalMinutes < TariffList[i].Minutes & time.TotalMinutes >= TariffList[i - 1].Minutes)
                {
                    return TariffList[i].Rate;
                }
            }
            return TariffList[TariffList.Count - 1].Rate;
        }
        public void PayForParking(int ticketNumber, decimal amount)
        {
            /*Если не найдем сессию по номеру, вернем null*/
            int numberInList;
            var currentSession = FindSession(ticketNumber, out numberInList);
            if (currentSession == null)
            {
                return;
            }

            if (amount == 0)
            {
                currentSession.PaymentDt = currentSession.EntryDt;
            }

            /*Тут запишем в поле TotalPayment текущей сессии
             общую сумму и отметим время платежа*/
            currentSession.PaymentDt = DateTime.Now;
            currentSession.TotalPayment = currentSession.TotalPayment != null ? currentSession.TotalPayment + amount : amount;
            ActiveSessions[numberInList] = currentSession;
            Saving();
        }
        /* ADDITIONAL TASK 2 */
        public bool TryLeaveParkingByCarPlateNumber(string carPlateNumber, out ParkingSession session)
        {
            /*Проверка на то, что сессия не найдена*/
            session = FindSession(carPlateNumber, out int numberInList);
            if (session == null)
            {
                return false;
            }
            /*Если сессия не оплачена, проверим, входит ли она в бесплатный период
             Если да, то из активных перемещаем в прошедшие, возвращаем true и пересохраняем
             все в файлы*/
            if (session.PaymentDt == null)
            {
                if (session.EntryDt.AddMinutes(FreeExit) > DateTime.Now)
                {
                    Exit(session, numberInList);
                    return true;
                }
            }
            else
            {
                /*Если оплата была, сделаем аналогичную проверку
                Если проверку на бесплатный выезд не пройдет, отказываем в отъезде
                возвращаем false и null сессию записываем в out*/
                DateTime paymentDt = (DateTime)session.PaymentDt;
                if (paymentDt.AddMinutes(FreeExit) > DateTime.Now)
                {
                    Exit(session, numberInList);
                    return true;
                }
                else
                {
                    session = null;
                    return false;
                }
            }
            /*Если пользователь не зарегистрирован, то отказываем в выезде*/
            bool found = false;
            for (int i = 0; i < RegisteredUsers.Count; i++)
            {
                if (RegisteredUsers[i].CarPlateNumber == carPlateNumber)
                {
                    found = true;
                }
            }

            if (!found)
            {
                session = null;
                return false;
            }
            else
            {
                /*иначе - автоматически выполняем оплату выезда, перетаскиваем из активных сессий в прошедшие
                и пересохраняем*/
                DateTime entryDt = session.EntryDt;
                session.EntryDt = session.EntryDt.AddMinutes(FreeExit);
                session.TotalPayment = GetRemainingCost(session.TicketNumber);
                session.EntryDt = entryDt;
                session.ExitDt = session.PaymentDt = DateTime.Now;
                ActiveSessions.RemoveAt(numberInList);
                CompletedSessions.Add(session);
                Saving();
                return true;
                /*В принципе, логика подробно описана в шаблоне проекта*/
            }
        }
        #endregion

        #region <Save/Load system>
        /*Просто метод для сохранения информации в файл*/
        public void Saving()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fileStream = new FileStream(AllDataPath, FileMode.OpenOrCreate))
            {
                Data allData = new Data(ActiveSessions, CompletedSessions, RegisteredUsers, ParkingCapacity);
                formatter.Serialize(fileStream, allData);
            }

        }

        /*Просто метод для загрузкт информации из файла*/
        public void Loading()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream(AllDataPath, FileMode.OpenOrCreate))
            {
                if (stream.Length != 0)
                {
                    Data loadData = (Data)formatter.Deserialize(stream);
                    ActiveSessions = loadData.ActiveSessions;
                    CompletedSessions = loadData.CompletedSessions;
                    ParkingCapacity = loadData.ParkingCapacity;
                }
            }
        }
        #endregion

        #region <Console writing>
        /*Из названия должно быть все ясно)*/
        public void PrintRegistered()
        {
            Console.WriteLine("Registered Users:");
            for (int i = 0; i < RegisteredUsers.Count; i++)
            {
                Console.WriteLine("{0}) {1} {2} {3}", i + 1, RegisteredUsers[i].Name, RegisteredUsers[i].CarPlateNumber, RegisteredUsers[i].Phone);
            }
        }
        public void PrintTariffs()
        {
            Console.WriteLine("Tariff List:");
            foreach (var element in TariffList)
            {
                Console.WriteLine("Minutes: {0} - Rate: {1:C} ", element.Minutes, element.Rate);
            }
        }
        #endregion

        #region <Service methods>
        /*Тут метод поиска сесси по номеру билета и его перегрузка с поиском по номеру авто*/
        public ParkingSession FindSession(int ticketNumber, out int numberInList)
        {
            for (int i = 0; i < ActiveSessions.Count; i++)
            {
                if (ActiveSessions[i].TicketNumber == ticketNumber)
                {
                    numberInList = i;
                    return ActiveSessions[i];
                }
            }
            numberInList = -1;
            return null;
        }

        public ParkingSession FindSession(string carPlateNumber, out int numberInList)
        {
            for (int i = 0; i < ActiveSessions.Count; i++)
            {
                if (ActiveSessions[i].CarPlateNumber == carPlateNumber)
                {
                    numberInList = i;
                    return ActiveSessions[i];
                }
            }
            numberInList = -1;
            return null;
        }

        /*Простая регистрация присваиванием. Можно было сделать это в свойстве, понял это только сейчас. Ну ничего, так тоже хорошо*/
        public void Register(List<User> users)
        {
            if (users != null)
            {
                RegisteredUsers = users;
                Saving();
                return;
            }
            else
            {
                Console.WriteLine("List is empty");
            }
        }
        /*Логика выхода с парковки. Вынес в отдельный метод, т.к. код много где повторялся*/
        private void Exit(ParkingSession session, int numberInList)
        {
            session.ExitDt = DateTime.Now;
            CompletedSessions.Add(session);
            ActiveSessions.RemoveAt(numberInList);
            Saving();
        }
        #endregion







    }
}
