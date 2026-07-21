using BepInEx;
using BepInEx.Logging;

// BepInEx entry point — compiled only in the BepInEx build.
// Wires GfLog / GfCoroutine and bridges Unity lifecycle methods.
[BepInPlugin("com.cb12438.gfmod", "gf Mod", "1.2.0")]
public partial class GfMod : BaseUnityPlugin
{
    private new ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;
        GfLog.MsgImpl  = s => Logger.LogInfo(s);
        GfLog.WarnImpl = s => Logger.LogWarning(s);
        GfCoroutine.StartImpl = e => StartCoroutine(e);
        GfInit();
    }

    private void Update()     => GfUpdate();
    private void LateUpdate() => GfLateUpdate();
    private void OnGUI()      => GfOnGUI();
}
