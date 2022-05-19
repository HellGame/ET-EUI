namespace ET
{
    public static class LoginPanelSystem
    {
        public static void RegisterUIEvent(this LoginPanel self)
        {
            self.FUILoginPanel.LoginBtn.AddListnerAsync(self.Login);
        }

        public static void OnShow(this LoginPanel self)
        {
            Log.Info("LoginPanel OnShow");
        }

        public static void OnHide(this LoginPanel self)
        {
            Log.Info("LoginPanel OnHide");
        }

        private static async ETTask Login(this LoginPanel self)
        {
            Log.Info("Login!");
            await TimerComponent.Instance.WaitAsync(1000);
            self.ZoneScene().GetComponent<FUIComponent>().ClosePanel(PanelId.LoginPanel);
        }
    }
}