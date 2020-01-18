namespace TcpServerBaseLibrary.Core
{
    internal enum TCPConnectionState
    {
        ReceivingHeader,
        ReceivingMessageData,
        ReceiveOperationStarted
    }
}
