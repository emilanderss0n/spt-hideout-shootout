using EFT;
using EFT.Hideout;
using EFT.InputSystem;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using System.Threading.Tasks;

namespace HideoutShootout
{
    // Sets InShootingRange immediately when the player confirms entry through the context menu,
    // preventing the weapon from being forced to semi-auto during the entry transition.
    internal class Patch_ShootingRangeBehaviour_ManualEnterLocation : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ShootingRangeBehaviour), nameof(ShootingRangeBehaviour.ManualEnterLocation));
        }

        [PatchPostfix]
        private static void Postfix(HideoutPlayerOwner player)
        {
            if (player != null)
            {
                player.InShootingRange = true;
            }
        }
    }

    // Skips HideoutPlayer.SetPatrol(true). The original method calls SetTriggerPressed(false)
    // and SetAim(false) when patrol mode is enabled, which would break full-auto each
    // time DecidePatrolStatus sees the player rotate past the degree limits.
    // SetPatrol(false) is still allowed so the initial unblock on entering the shooting
    // range and the cleanup on ExitShootingRange() (triggered by ESC) work correctly
    internal class Patch_HideoutPlayer_SetPatrol : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutPlayer), nameof(HideoutPlayer.SetPatrol));
        }

        [PatchPrefix]
        private static bool Prefix(bool patrol)
        {
            return !patrol;
        }
    }

    // Gate HideoutPlayerOwner.ExitShootingRange so it only runs when ESC explicitly asks for it.
    // The game otherwise calls it from ShootingRangeBehaviour.OnExitLocation (for example: walking out
    // of the area trigger), which would lower the weapon and disable full-auto. Our ESC pass-
    // through open ze gate just for that single command.
    internal class Patch_HideoutPlayerOwner_ExitShootingRange : ModulePatch
    {
        public static bool AllowExit = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutPlayerOwner), nameof(HideoutPlayerOwner.ExitShootingRange));
        }

        [PatchPrefix]
        private static bool Prefix(ref Task __result)
        {
            if (AllowExit)
            {
                return true;
            }
            __result = Task.CompletedTask;
            return false;
        }
    }

    // Detects the ESC press in shooting-range mode, opens the AllowExit gate so the original
    // ExitShootingRange runs (lowering the weapon and clearing InShootingRange), then closes
    // the gate again
    internal class Patch_HideoutPlayerOwner_TranslateExitScreenInput : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutPlayerOwner), nameof(HideoutPlayerOwner.TranslateExitScreenInput));
        }

        [PatchPrefix]
        private static void Prefix(HideoutPlayerOwner __instance, ECommand command)
        {
            if (command.IsCommand(ECommand.Escape) && __instance.InShootingRange)
            {
                Patch_HideoutPlayerOwner_ExitShootingRange.AllowExit = true;
            }
        }

        [PatchPostfix]
        private static void Postfix()
        {
            Patch_HideoutPlayerOwner_ExitShootingRange.AllowExit = false;
        }
    }
}
