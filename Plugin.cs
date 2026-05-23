using BepInEx;
using BepInEx.Logging;

namespace HideoutShootout
{
    [BepInPlugin("com.moxopixel.hideoutshootout", "MoxoPixel-HideoutShootout", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;

        private void Start()
        {
            LogSource = Logger;

            new Patch_ShootingRangeBehaviour_ManualEnterLocation().Enable();
            new Patch_HideoutPlayer_SetPatrol().Enable();
            new Patch_HideoutPlayerOwner_ExitShootingRange().Enable();
            new Patch_HideoutPlayerOwner_TranslateExitScreenInput().Enable();

            LogSource.LogInfo("Hideout Weapon Freedom loaded");
        }
    }
}