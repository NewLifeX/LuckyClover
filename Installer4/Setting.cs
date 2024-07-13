using System;
using System.ComponentModel;
using NewLife;
using NewLife.Configuration;
using NewLife.Remoting.Clients;

namespace Installer;

[Config("Clover")]
public class Setting : Config<Setting>, IClientSetting
{
    #region 属性
    /// <summary>证书</summary>
    [Description("证书")]
    public String Code { get; set; }

    /// <summary>密钥</summary>
    [Description("密钥")]
    public String Secret { get; set; }

    /// <summary>服务地址端口。默认为空，子网内自动发现</summary>
    [Description("服务地址端口。默认为空，子网内自动发现")]
    public String Server { get; set; } = "";
    #endregion

    #region 加载/保存
    protected override void OnLoaded()
    {
        if (Server.IsNullOrEmpty()) Server = "http://s.newlifex.com:6600";

        base.OnLoaded();
    }
    #endregion
}