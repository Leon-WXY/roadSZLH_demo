using zFrame.Networks;
using UI.Login;
using UnityEngine;
using UnityFramework.MVC;
using UnityFramework.Notification;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Asyncoroutine;
using System;
using LitJson;
using System.Runtime.InteropServices;

public class LoginSystem : SystemBase
{
    public bool IsLogoutReturn
    {
        get
        {
            return isLogoutReturn;
        }
    }
    private bool isLogoutReturn = false;
    public override void OnShown()
    {
        Debug.Log("Login show");
        //StartConnect();
        //NetworkClient.OnDisconnected.AddListener(OnDisConnet);
        //NetworkClient.AddListener(ResponseCode.Login, LoginCallBack);
        ShowLoginPanel();
        NetworkClient.AddListener(ResponseCode.Login, LoginCallBack);
        NetworkClient.Connect();
        base.OnShown();
    }

    #region Old

    private void StartConnect()
    {
#if CONNET_NETWORK
        if (!NetworkClient.isConnected)
        {
            ConnectNetwork();
        }
#endif
    }

    private void ConnectNetwork()
    {
        ShowLoading();
        ////连接网络
        NetworkClient.Connect();
        //每隔0.1秒執行一次定時器
        InvokeRepeating("isConnected", 1, 0.1f);
    }

    private void isConnected()
    {
        if (NetworkClient.isConnected)
        {
            HideLoading();
            Debug.Log("连接成功");
            this.CancelInvoke();//取消定时器的执行
            if (isConnectLogin)
            {
                isConnectLogin = false;
                SendLoginMsg();
            }
        }
        if (NetworkClient.hasReconnected)
        {
            isConnectLogin = false;
            HideLoading();
            Debug.Log("重连仍没有成功");
            this.CancelInvoke();//取消定时器的执行
            //ShowTips("连接网络失败");
        }
    }
    private bool isConnectLogin = false;
    private Request request;
    //    private void OnLogin(string username, string password)
    //    {
    //        ShowLoading();
    //        AccountVo loginVo = new AccountVo();
    //        loginVo.account = username;
    //        loginVo.password = password;
    //        loginVo.isfaceLogin = "0";
    //        request = new Request(loginVo, RequestCode.Login);

    //#if CONNET_NETWORK
    //        if (!NetworkClient.isConnected)
    //        {
    //            isConnectLogin = true;
    //            ConnectNetwork();
    //        }
    //        else
    //        {
    //            isConnectLogin = false;
    //            SendLoginMsg();
    //        }
    //#elif NO_NETWORK
    //        OnLoginSuccess();
    //#endif
    //    }

    private void SendLoginMsg()
    {
        NetworkClient.SendMessage(request);
    }

    private void LoginCallBack(Response response)
    {
        //Debug.LogError("response.message: " + response.message);
        ResponseLogin responseMsg = LitJson.JsonMapper.ToObject<ResponseLogin>(response.message);
        if (responseMsg.code == 200 && responseMsg.data.isLogin)
        {
            Debug.Log($"<color=aqua>TCP Login Success</color>");
            //Debug.Log("TCP Login Success");
        }
        else
        {
            Debug.LogError("TCP Login Failed");
        }
        return;
        //成功 跳转到首页
        //ResponseLoginVo responseMsg = LitJson.JsonMapper.ToObject<ResponseLoginVo>(response.message);
        //if (responseMsg == null)
        //{
        //    Debug.LogError("null!");
        //    return;
        //}
        //Debug.LogError($"code: {responseMsg.code}");

        //if (responseMsg.code == 200 && responseMsg.data.isLogin)
        //{
        //    CommonModel.Instance.AccountInfo = responseMsg.data;
        //    OnLoginSuccess();
        //}
        //else if (responseMsg.code == 200 && responseMsg.data.msg == 3)
        //{
        //    HideLoading();
        //    //用户名错误
        //    loginPanel.TextTypeErrorTip.text = "用户名不存在";
        //    loginPanel.TypeErrorTip.SetActive(true);
        //}
        //else if (responseMsg.code == 200 && responseMsg.data.msg == 2)
        //{
        //    HideLoading();
        //    //密码错误
        //    loginPanel.TextTypeErrorTip.text = "密码错误";
        //    loginPanel.TypeErrorTip.SetActive(true);
        //}
        //else if (responseMsg.code == 200 && responseMsg.data.msg == 4)
        //{
        //    HideLoading();
        //    //密码错误
        //    ShowTips("账户已停用");
        //}
        //else
        //{
        //    HideLoading();
        //    ShowTips("网络错误");
        //}
    }
    #endregion

    private ToastTips crtToastTipView;
    public void ShowToastTip(string content)
    {
        if (crtToastTipView == null)
        {
            //CommonTip
            Transform parent = UICanvas.transform.Find("CommonTip");
            ShowView<ToastTips>((p) =>
            {
                crtToastTipView = p;
            }, UITipsParent, content);
        }
        else
        {
            crtToastTipView.Data = content as object;
            crtToastTipView.OnShown();
        }
    }

    //Todo:点击自动登录toggle后的自动登录逻辑
    private LoginPanel loginPanel;
    private void ShowLoginPanel()
    {
        if (null == loginPanel)
        {
            ShowView<LoginPanel>((LoginPanel loginPanel) =>
            {
                this.loginPanel = loginPanel;
                loginPanel.OnQuit = () =>
                {
                    ShowConfirmDialog(new ConfirmDialogArg("确认关闭运维管理平台？"), () => { OnQuit(); });
                };
                loginPanel.OnMinimize = () =>
                {
                    ShowWindow(GetForegroundWindow(), SW_SHOWMINIMIZED);
                };
                loginPanel.OnLogin = OnLogin;
                loginPanel.InitUserPass();
            });
        }
        else
        {
            loginPanel.InitUserPass();
        }
    }

    private void OnLogin(string username, string password)
    {
        loginPanel?.LoginError(string.Empty);
        ShowLoading();
        this.loginPanel?.SetBtnLoginEnabel(false);
        if (!NetworkClient.isConnected)
        {
            TCPLogin(username, password);
        }
        HttpRequestClient.Instance.Login(username, password, OnLoginCallBack);
    }

    private void OnLoginCallBack(string error)
    {
        if (string.IsNullOrEmpty(error))
        {
            OnLoginSuccess();
            //Debug.LogError($"登录成功");
        }
        else
        {
            loginPanel.LoginError(error);
            this.loginPanel?.SetBtnLoginEnabel(true);
            HideLoading();
        }
    }

    private async void OnLoginSuccess()
    {
        SystemNotifyArg arg = new SystemNotifyArg();
        arg.System = typeof(HomeSystem);
        arg.SystemName = "HomeSystem";
#if !UNITY_EDITOR
        if (null != SceneAsyncLoader.AsyncOperation && !isLogoutReturn)
        {
            SceneAsyncLoader.AllowSceneActivation = true;
            await SceneAsyncLoader.AsyncOperation;
        }
#endif
        if (isLogoutReturn)
        {
            await SceneLoader.LoadAsyncOnLogin("Ground");
        }
        await new WaitForSeconds(0.5f);
        NotificationManager.Instance.Send(NotifyIds.SYSTEM_CHANGE, arg);
        if (!isLogoutReturn)
            isLogoutReturn = true;
        HideLoading();
        //BuildingSystemNavigationManager.Instance.InitSystem();
    }

    public override void SwitchSystemBefore()
    {
        this.loginPanel?.SetBtnLoginEnabel(true);
        loginPanel?.HideEffect(() =>
        {
            CloseView(loginPanel);
            loginPanel = null;
            HideLoading();
        });
        crtToastTipView?.OnHide(true);
        crtToastTipView = null;
        NetworkClient.RemoveListener(ResponseCode.Login, LoginCallBack);
        //NetworkClient.RemoveListener(ResponseCode.Login, LoginCallBack);
        //NetworkClient.OnDisconnected.RemoveListener(OnDisConnet);
        base.SwitchSystemBefore();
    }

    private void TCPLogin(string username, string password)
    {
        AccountVo loginVo = new AccountVo();
        loginVo.id = 123;
        loginVo.account = username;
        loginVo.password = password;
        loginVo.isfaceLogin = "0";
        Request request = new Request(loginVo, RequestCode.Login);
        NetworkClient.SendMessage(request);
    }

    private void OnQuit()
    {
        if (Application.isEditor && Application.isPlaying)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
        else
        {
            Application.Quit();
        }
    }

    const int SW_SHOWMINIMIZED = 2; //{最小化, 激活}  
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);
    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    private void Update()
    {
#if UNITY_EDITOR
        //if (Input.GetKeyDown(KeyCode.S))
        //{
        //    AccountVo loginVo = new AccountVo();
        //    loginVo.id = 123;
        //    //loginVo.account = username;
        //    //loginVo.password = password;
        //    loginVo.isfaceLogin = "0";
        //    Request request = new Request(loginVo, RequestCode.Login);
        //    NetworkClient.SendMessage(request);
        //}
#endif
    }
}

