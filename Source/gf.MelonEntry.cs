using MelonLoader;

[assembly: MelonInfo(typeof(GfMod), "gf Mod", "1.2.0", "Cb12438")]
[assembly: MelonGame]

// MelonLoader entry point — compiled only in the MelonLoader build.
// Wires GfLog / GfCoroutine and bridges Unity lifecycle methods.
public partial class GfMod : MelonMod
{
    [System.Obsolete]
    public override void OnApplicationStart()
    {
        GfLog.MsgImpl  = s => MelonLogger.Msg(s);
        GfLog.WarnImpl = s => MelonLogger.Warning(s);
        GfCoroutine.StartImpl = e => MelonCoroutines.Start(e);
        GfInit();
    }

    public override void OnUpdate()     => GfUpdate();
    public override void OnLateUpdate() => GfLateUpdate();
    public override void OnGUI()        => GfOnGUI();
}
