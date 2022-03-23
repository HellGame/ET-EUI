namespace ET
{
    public static class DisconnectHelper
    {
        public static async ETTask Disconnect(this Session self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            // 下面的等待过程中 session 有可能会被释放，instanceId 有可能会被改变，所以这里存一下，之后比较。
            long instanceId = self.InstanceId;
            
            await TimerComponent.Instance.WaitAsync(1000);

            if (self.InstanceId != instanceId)
            {
                return;
            }
            
            self.Dispose();
        }
    }
}