using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;

public class ElectricityWireColors : IModApi
{

    // Dynamic accessors for private fields
    static FieldInfo fieldCurrentCureNodes;
    static FieldInfo fieldWireDataList;

    // Entry class for A20 patching
    public void InitMod(Mod mod)
    {
        Log.Out(" Loading Patch: " + this.GetType().ToString());
        fieldCurrentCureNodes = AccessTools.Field(typeof(TileEntityPowered), "currentWireNodes");
        fieldWireDataList = AccessTools.Field(typeof(TileEntityPowered), "wireDataList");
        var harmony = new HarmonyLib.Harmony(GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    // Helper function to update pulse color if necessary
    public static void UpdateWirePulseColor(IWireNode wire, Color col)
    {
        // Just sets the color member
        wire.SetPulseColor(col);
        // Make sure color is actually updated on the shader
        wire.TogglePulse(WireManager.Instance.ShowPulse);
    }

    public static void UpdateWirePulseColors(TileEntityPowered __instance)
    {
        // Bail out if code is running on a server
        if (GameManager.IsDedicatedServer) return;

        // Get some private fields from TileEntityPowered (fail hard if this returns null)
        List<Vector3i> wireDataList = fieldWireDataList.GetValue(__instance) as List<Vector3i>;
        List<IWireNode> currentWireNodes = fieldCurrentCureNodes.GetValue(__instance) as List<IWireNode>;

        // Play safe in case parent class (or we) left the two arrays in a bad state
        int max = MathUtils.Min(currentWireNodes.Count, wireDataList.Count);

        for (int index = 0; index < max; ++index)
        {
            Vector3i wireData = wireDataList[index];
            if (GameManager.Instance.World.GetChunkFromWorldPos(wireData) is Chunk chunk)
            {
                if (GameManager.Instance.World.GetTileEntity(chunk.ClrIdx, wireData) is TileEntityPowered tileEntity)
                {
                    bool isPowered = __instance.IsPowered && tileEntity.IsPowered;
                    Color pulseColor = isPowered ? Color.yellow : Color.gray;
                    if (__instance.PowerItemType == tileEntity.PowerItemType)
                    {
                        if (__instance.PowerItemType == PowerItem.PowerItemTypes.TripWireRelay)
                        {
                            pulseColor = isPowered ? Color.magenta : Color.cyan;
                        }
                        else if (__instance.PowerItemType == PowerItem.PowerItemTypes.ElectricWireRelay)
                        {
                            pulseColor = isPowered ? Color.red : Color.blue;
                        }
                    }
                    UpdateWirePulseColor(currentWireNodes[index], pulseColor);
                }
            }
        }
    }

    // At some point we had an issue that we were called to early (e.g. when power source is powered on)
    public static IEnumerator UpdateWirePulseColorsLater(TileEntityPowered __instance, float delay)
    {
        yield return new WaitForSeconds(delay);
        UpdateWirePulseColors(__instance);
    }

    // Do some additional work after DrawWires was executed
    // Store some information we need later to color the wires
    [HarmonyPatch(typeof(TileEntityPowered))]
    [HarmonyPatch("DrawWires")]
    public class TileEntityPowered_DrawWires
    {
        static void Postfix(TileEntityPowered __instance)
        {
            IEnumerator coroutine = UpdateWirePulseColorsLater(__instance, 0.5f);
            GameManager.Instance.StartCoroutine(coroutine);
            UpdateWirePulseColors(__instance);
        }
    }

    // Update pulse color when power source is switched on/off
    // This is problematic, as downstream children are not powered yet
    // The power distribution will take place when power manager ticks
    [HarmonyPatch(typeof(PowerSource))]
    [HarmonyPatch("HandleOnOffSound")]
    public class PowerSource_HandleOnOffSound
    {
        static void Postfix(PowerSource __instance)
        {
            if (__instance.TileEntity != null)
            {
                // Schedule an update to happen in 0.25 (in order to let power source tick once)
                // Necessary since the items connected to this source haven't been powered yet
                // They will get separate `HandlePowerUpdate` events later, but they only color
                // their child wires and not the parent connection, as we previously did when
                // we stored the `parentWire` information to the TileEntity. Since we now only
                // try to be A20 compatible, we can't easily get the parent wire. There are ways,
                // but this seems to be a much nicer solution, as it should be quite CPU friendly.
                IEnumerator coroutine = UpdateWirePulseColorsLater(__instance.TileEntity, 0.25f);
                GameManager.Instance.StartCoroutine(coroutine);
                // The off state can be determined easily :)
                UpdateWirePulseColors(__instance.TileEntity);
            }
        }
    }

    // Update pulse color when power changed
    // Called when wires get dis/reconnected
    [HarmonyPatch(typeof(PowerConsumer))]
    [HarmonyPatch("HandlePowerUpdate")]
    public class PowerConsumer_HandlePowerUpdate
    {
        static void Postfix(PowerConsumer __instance)
        {
            // Parent connection may not be there yet!
            if (__instance.TileEntity == null) return;
            UpdateWirePulseColors(__instance.TileEntity);
        }
    }

    // Update pulse color when available power changes
    [HarmonyPatch(typeof(PowerConsumerToggle))]
    [HarmonyPatch("HandlePowerUpdate")]
    public class PowerConsumerToggle_HandlePowerUpdate
    {
        static void Postfix(PowerConsumer __instance)
        {
            if (__instance.TileEntity != null)
                UpdateWirePulseColors(__instance.TileEntity);
        }
    }

    // Update pulse color when power is triggered
    [HarmonyPatch(typeof(PowerTrigger))]
    [HarmonyPatch("HandlePowerUpdate")]
    public class PowerTrigger_HandlePowerUpdate
    {
        static void Postfix(PowerTrigger __instance)
        {
            if (__instance.TileEntity == null) return;
            UpdateWirePulseColors(__instance.TileEntity);
        }
    }

    // Update pulse color when power is time triggered
    [HarmonyPatch(typeof(PowerTimerRelay))]
    [HarmonyPatch("HandlePowerUpdate")]
    public class PowerTimerRelay_HandlePowerUpdate
    {
        static void Postfix(PowerTimerRelay __instance)
        {
            if (__instance.TileEntity == null) return;
            UpdateWirePulseColors(__instance.TileEntity);
        }
    }

    // Update pulseColor when read from server (MP mode)
    [HarmonyPatch(typeof(TileEntityPowered))]
    [HarmonyPatch("read")]
    public class TileEntityPowered_read
    {
        static void Postfix(TileEntityPowered __instance)
        {
            UpdateWirePulseColors(__instance);
        }
    }

    // Update pulseColor when power input changes (SP mode)
    // [HarmonyPatch(typeof(PowerConsumer))]
    // [HarmonyPatch("IsPoweredChanged")]
    // public class PowerConsumer_IsPoweredChanged
    // {
    //     static void Postfix(PowerConsumer __instance)
    //     {
    //         if (__instance.TileEntity != null)
    //             UpdateWirePulseColors(__instance.TileEntity);
    //     }
    // }

}
