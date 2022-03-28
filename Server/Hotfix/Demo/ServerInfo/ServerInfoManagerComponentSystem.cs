using System;

namespace ET
{
    public class ServerInfoManagerComponentAwakeSyetem: AwakeSystem<ServerInfoManagerComponent>
    {
        public override void Awake(ServerInfoManagerComponent self)
        {
            self.Awake().Coroutine();
        }
    }
    
    public class ServerInfoManagerComponentDestroySystem: DestroySystem<ServerInfoManagerComponent>
    {
        public override void Destroy(ServerInfoManagerComponent self)
        {
            foreach (var serverInfo in self.ServerInfoList)
            {
                serverInfo?.Dispose();
            }
            self.ServerInfoList.Clear();
        }
    }
    
    public class ServerInfoManagerComponentLoadSystem: LoadSystem<ServerInfoManagerComponent>
    {
        public override void Load(ServerInfoManagerComponent self)
        {
            self.Awake().Coroutine();
        }
    }

    public static class ServerInfoManagerComponentSystem
    {
        public static async ETTask Awake(this ServerInfoManagerComponent self)
        {
            var serverInfoList = await DBManagerComponent.Instance.GetZoneDB(self.DomainZone()).Query<ServerInfo>(d => true);
            if (serverInfoList == null || serverInfoList.Count <= 0)
            {
                Log.Error("ServerInfo count is 0");
                return;
            }
            
            foreach (var serverInfo in serverInfoList)
            {
                self.AddChild(serverInfo);
                self.ServerInfoList.Add(serverInfo);
            }
        }
    }
}