using System;

namespace HoRang2Sea.Services
{
    public class DataReceivedEventArgs : EventArgs
    {
        public string Data { get; private set; }
        public DataReceivedEventArgs(string data)
        {
            Data = data;
        }
    }


}
