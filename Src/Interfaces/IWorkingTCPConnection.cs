using System;
using System.Net.Sockets;

namespace TcpServerBaseLibrary
{
    internal interface IWorkingTCPConnection
    {
        Socket WorkSocket { get; set; }

        void ExecuteState(int ms);

        event Action<MessageObject> CompleteDataReceived;
        event Action<IWorkingTCPConnection> ConnectionClosedEvent;
    }
}