using System;

namespace TcpServerBaseLibrary.DataManagers
{
    [Obsolete("Obsolete. Use IMessageManager instead")]
    public abstract class DataManager<T>
    {

        public abstract T ParseData(byte[] data);
 
    }
}
