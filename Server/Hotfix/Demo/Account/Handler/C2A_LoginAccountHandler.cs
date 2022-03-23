using System;
using System.Text.RegularExpressions;

namespace ET
{
    public class C2A_LoginAccountHandler: AMRpcHandler<C2A_LoginAccount, A2C_LoginAccount>
    {
        protected override async ETTask Run(Session session, C2A_LoginAccount request, A2C_LoginAccount response, Action reply)
        {
            if (session.DomainScene().SceneType != SceneType.Account)
            {
                Log.Error($"请求的 Scene 错误，当前 Scene 为: {session.DomainScene().SceneType}");
                session.Dispose();
                return;
            }
            session.RemoveComponent<SessionAcceptTimeoutComponent>();

            if (string.IsNullOrEmpty(request.AccountName) || string.IsNullOrEmpty(request.Password))
            {
                response.Error = ErrorCode.ERR_LoginInfoIsNull;
                reply();
                session.Dispose();
                return;
            }

            if (!Regex.IsMatch(request.AccountName.Trim(), @"^(?=.*[0-9].*)(?=.*[A-Z].*)(?=.*[a-z].*).{6,15}$"))
            {
                response.Error = ErrorCode.ERR_AccountNameFormError;
                reply();
                session.Dispose();
                return;
            }
            
            if (!Regex.IsMatch(request.Password.Trim(), @"^(?=.*[0-9].*)(?=.*[A-Z].*)(?=.*[a-z].*).{6,15}$"))
            {
                response.Error = ErrorCode.ERR_PasswordFormError;
                reply();
                session.Dispose();
                return;
            }

            var accountInfoList = await DBManagerComponent.Instance.GetZoneDB(session.DomainZone()).Query<Account>(d => d.AccountName.Equals(request.AccountName.Trim()));
            Account account = null;
            if (accountInfoList.Count > 0)
            {
                account = accountInfoList[0];
                session.AddChild(account);
                if (account.AccountType == (int)AccountType.BlackList)
                {
                    response.Error = ErrorCode.Err_AccountInBlackListError;
                    reply();
                    session.Dispose();
                    return;
                }

                if (!account.Password.Equals(request.Password))
                {
                    response.Error = ErrorCode.Err_LoginPasswordError;
                    reply();
                    session.Dispose();
                    return;
                }
            }
            else
            {
                account = session.AddChild<Account>();
                account.AccountName = request.AccountName.Trim();
                account.Password = request.Password;
                account.CreateTime = TimeHelper.ServerNow();
                account.AccountType = (int)AccountType.General;
                await DBManagerComponent.Instance.GetZoneDB(session.DomainZone()).Save<Account>(account);
            }

            string token = $"{TimeHelper.ServerNow()}{RandomHelper.RandomNumber(int.MinValue, int.MaxValue)}";
            session.DomainScene().GetComponent<TokenComponent>().Remove(account.Id);
            session.DomainScene().GetComponent<TokenComponent>().Add(account.Id, token);

            response.AccountId = account.Id;
            response.Token = token;

            reply();
        }
    }
}