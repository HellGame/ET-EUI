using System.Collections.Generic;

namespace ET
{
    public struct PanelInfo
    {
        public PanelId PanelId;
    
        public string PackageName;
    
        public string ComponentName;
    }
    
    public class FUIEventComponent : Entity, IAwake, IDestroy
    {
        public static FUIEventComponent Instance { get; set; }
        public readonly Dictionary<PanelId, IFUIEventHandler> UIEventHandlers = new Dictionary<PanelId, IFUIEventHandler>();
        public readonly Dictionary<PanelId, PanelInfo> PanelIdInfoDict = new Dictionary<PanelId, PanelInfo>();
        public readonly Dictionary<string, PanelInfo> PanelTypeInfoDict = new Dictionary<string, PanelInfo>();

        public bool isClicked { get; set; }
    }
}