using System;
using Memphis.Client.Helper;

namespace Memphis.Client.Station
{
    public class MemphisStation
    {
        private readonly MemphisClient _memphisClient;
        private readonly string _name;
        private readonly string _internalName;


        public MemphisStation(MemphisClient memphisClient, string stationName)
        {
            this._memphisClient = memphisClient ?? throw new ArgumentNullException(nameof(memphisClient));
            this._name = stationName ?? throw new ArgumentNullException(nameof(stationName));

            this._internalName = MemphisUtil.GetInternalName(stationName);
        }

        public string Name
        {
            get { return _name; }
        }
        
        
        internal string InternalName
        {
            get { return _internalName; }
        }
    }
}