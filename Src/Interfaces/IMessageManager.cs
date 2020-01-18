using TcpServerBaseLibrary.Core;

namespace TcpServerBaseLibrary.Interface
{
    public interface IMessageManager
    {
        void HandleMessage(MessageObject msgobj);
    }
}
