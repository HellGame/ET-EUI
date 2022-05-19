namespace ET
{
    public class LoginPanel : Entity, IAwake
    {
        public FUI_LoginPanel FUILoginPanel
        {
            get => (FUI_LoginPanel)this.GetParent<FUIEntity>().GComponent;
        } 
    }
}