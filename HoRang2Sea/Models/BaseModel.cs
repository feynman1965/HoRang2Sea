using HoRang2Sea.Common;
using HoRang2Sea.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading;

namespace HoRang2Sea.Models
{
    public class BaseModel : ReactiveObject
    {
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        protected virtual void OnDataReceived(string data) => DataReceived?.Invoke(this, new DataReceivedEventArgs(data));
    }
}
