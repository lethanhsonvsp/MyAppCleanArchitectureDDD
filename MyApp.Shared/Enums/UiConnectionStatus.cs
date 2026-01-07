using System;
using System.Collections.Generic;
using System.Text;

namespace MyApp.Shared.Enums
{
    public enum UiConnectionStatus
    {
        Initial,    // chưa connect
        Connecting,
        Connected,
        Disconnected,
        Error
    }

}
