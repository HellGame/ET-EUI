using FairyGUI;
using UnityEngine;

namespace ET
{
    [ObjectSystem]
    public class GlobalComponentAwakeSystem: AwakeSystem<GlobalComponent>
    {
        public override void Awake(GlobalComponent self)
        {
            GlobalComponent.Instance = self;
            
            self.Global = GameObject.Find("/Global").transform;
            self.Unit = GameObject.Find("/Global/UnitRoot").transform;
            self.UI = GameObject.Find("/Global/UIRoot").transform;
            self.NormalRoot = GameObject.Find("Global/UIRoot/NormalRoot").transform;
            self.PopUpRoot = GameObject.Find("Global/UIRoot/PopUpRoot").transform;
            self.FixedRoot = GameObject.Find("Global/UIRoot/FixedRoot").transform;
            self.OtherRoot = GameObject.Find("Global/UIRoot/OtherRoot").transform;
            self.PoolRoot =  GameObject.Find("Global/PoolRoot").transform;

            self.GRoot = GRoot.inst;

            self.NormalGRoot = new GComponent();
            self.NormalGRoot.gameObjectName = "NormalGRoot";
            GRoot.inst.AddChild(self.NormalGRoot);
            
            self.PopUpGRoot = new GComponent();
            self.PopUpGRoot.gameObjectName = "PopUpGRoot";
            GRoot.inst.AddChild(self.PopUpGRoot);
            
            self.FixedGRoot = new GComponent();
            self.FixedGRoot.gameObjectName = "FixedGRoot";
            GRoot.inst.AddChild(self.FixedGRoot);
            
            self.OtherGRoot = new GComponent();
            self.OtherGRoot.gameObjectName = "OtherGRoot";
            GRoot.inst.AddChild(self.OtherGRoot);
        }
    }
}