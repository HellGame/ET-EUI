namespace ET
{
    public class AfterCreateZoneScene_AddComponent: AEventAsync<EventType.AfterCreateZoneScene>
    {
        protected override async ETTask Run(EventType.AfterCreateZoneScene args)
        {
            Scene zoneScene = args.ZoneScene;
            // zoneScene.AddComponent<UIComponent>();
            // zoneScene.AddComponent<UIPathComponent>();
            // zoneScene.AddComponent<UIEventComponent>();
            // zoneScene.AddComponent<RedDotComponent>();

            zoneScene.AddComponent<ResourcesLoaderComponent>();
            await zoneScene.AddComponent<GameResLoaderComponent>().LoadAsync();
            zoneScene.AddComponent<FUIEventComponent>();
            zoneScene.AddComponent<FUIComponent>();
        
            await zoneScene.GetComponent<FUIComponent>().ShowPanelAsync(PanelId.LoginPanel);
        }
    }
}