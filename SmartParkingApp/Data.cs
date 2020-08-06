using System;
using System.Collections.Generic;
using System.Text;

namespace ParkingApp
{
    [Serializable]
    class Data
    {
        
        public Data(List<ParkingSession> activeSessions, List<ParkingSession> completedSessions, List<User> users, int capacity)
        {
            ActiveSessions = activeSessions;
            CompletedSessions = completedSessions;
            Users = users;
            ParkingCapacity = capacity;
        }
        public List<ParkingSession> ActiveSessions { get; set; }
        public List<ParkingSession> CompletedSessions { get; set; }
        public List<User> Users { get; set; }
        public int ParkingCapacity { get; set; }
    }
}
