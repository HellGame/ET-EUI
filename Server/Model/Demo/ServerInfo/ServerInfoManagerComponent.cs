using System.Collections.Generic;

namespace ET
{
    public class ServerInfoManagerComponent: Entity, IAwake, IDestroy, ILoad
    {
        public List<ServerInfo> ServerInfoList = new List<ServerInfo>();
    }
}