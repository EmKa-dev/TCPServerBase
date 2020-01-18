namespace TcpServerBaseLibrary.Core
{
    internal enum TCPServerState
    {
        Listening,
        AcceptConnectionRequestOperationStarted,

        ConnectionThresholdReached            
    }
}
