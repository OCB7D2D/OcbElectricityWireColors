using DMT;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.Collections;

public class OcbElectricityWireColors
{

    // Entry class for Harmony patching
    public class OcbElectricityWireColors_Init : IHarmony
    {
        public void Start()
        {
            Debug.Log("Loading OCB Electricity Wire Colors Patch: " + GetType().ToString());
            var harmony = new Harmony(GetType().ToString());
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
    
    // Helper function to update pulse color if necessary
    public static void UpdatePulseColor(IWireNode wire, Color col, bool force = false)
    {
        if (wire is FastWireNode fast)
        {
            if (fast.pulseColor != col || true)
            {
                fast.SetPulseColor(col);
                fast.TogglePulse(WireManager.Instance.ShowPulse);
            }
        }
        else if (wire is WireNode node)
        {
            if (node.pulseColor != col || true)
            {
                node.SetPulseColor(col);
                node.TogglePulse(WireManager.Instance.ShowPulse);
            }
        }
    }

    public static void UpdateWireColor(TileEntityPowered __instance)
    {
        // This check is copied from the original function
        // if (!((UnityEngine.Object)__instance.BlockTransform != (UnityEngine.Object)null))
        //     return;
        // Get the power state for this power tile entity
        bool isPowered = __instance.IsPowered;
        // Default pulse color is yellow or gray
        Color pulseColor = isPowered ? Color.yellow : Color.gray;
        // Check if we have a parent wire of the same type
        if (__instance.ParentWire != null && __instance.isParentSameType)
        {
            if (__instance.PowerItemType == PowerItem.PowerItemTypes.TripWireRelay)
            {
                pulseColor = isPowered ? Color.magenta : Color.cyan;
            }
            else if (__instance.PowerItemType == PowerItem.PowerItemTypes.ElectricWireRelay)
            {
                pulseColor = isPowered ? Color.red : Color.blue;
            }
            else {
                // E.g. for Consumer after Consumer (don't change color)
                // pulseColor = isPowered ? Color.green : Color.gray;
            }
        }
        // Update pulse color (if needed/changed)
        UpdatePulseColor(__instance.ParentWire, pulseColor, true);
    }

    public static IEnumerator updateWiresLater(TileEntityPowered __instance)
    {
        yield return (object) new WaitForSeconds(0.5f);
        UpdateWireColor(__instance);
    }

    // Do some additional work after DrawWires was executed
    // Store some information we need later to color the wires
    [HarmonyPatch(typeof(TileEntityPowered))]
    [HarmonyPatch("DrawWires")]
    public class TileEntityPowerSource_DrawWires
    {
        static void Postfix(TileEntityPowered __instance)
        {
            // Check that counts match up (should always be the case, but be cautious)
            // if (__instance.wireDataList.Count != __instance.currentWireNodes.Count) {
            //     Log.Out("TileEntityPowered not correctly initialized?");
            // }
            // ToDo: Check why this can be different at all after `DrawWires`
            // Note: seems to work none the less (so don't really care too much)
            for (int i = 0; i < __instance.currentWireNodes.Count; ++i)
            {
                IWireNode wire = __instance.currentWireNodes[i];
                Vector3i  data = new Vector3i(wire.GetEndPosition());
                if (GameManager.Instance.World.GetChunkFromWorldPos(data) is Chunk chunk)
                {
                    TileEntityPowered tileEntity = GameManager.Instance.World
                        .GetTileEntity(chunk.ClrIdx, data) as TileEntityPowered;
                    if (tileEntity == null) continue;
                    if (wire is FastWireNode fast)
                    {
                        tileEntity.isParentSameType = fast.pulseColor != Color.yellow
                                                        && fast.pulseColor != Color.gray;
                    }
                    else if (wire is WireNode node)
                    {
                        tileEntity.isParentSameType = node.pulseColor != Color.yellow
                                                        && node.pulseColor != Color.gray;
                    }
                    tileEntity.ParentWire = wire;
                }
            }
            GameManager.Instance.StartCoroutine(updateWiresLater(__instance));
            UpdateWireColor(__instance);
        }
    }

    // Update pulseColor when read from server (MP mode)
    [HarmonyPatch(typeof(TileEntityPowered))]
    [HarmonyPatch("read")]
    public class TileEntityPoweredBlock_read
    {
        static void Postfix(TileEntityPoweredBlock __instance)
        {
            UpdateWireColor(__instance);
        }
    }

    // Update pulseColor on each tick (MP mode)
    [HarmonyPatch(typeof(PowerConsumer))]
    [HarmonyPatch("HandlePowerUpdate")]
    public class PowerConsumer_HandlePowerUpdate
    {
        static void Postfix(PowerConsumer __instance)
        {
            if (__instance.TileEntity != null)
                UpdateWireColor(__instance.TileEntity);
        }
    }

    // Update pulseColor when power input changes (SP mode)
    [HarmonyPatch(typeof(PowerConsumerToggle))]
    [HarmonyPatch("HandlePowerUpdate")]
    public class PowerConsumerToggle_HandlePowerUpdate
    {
        static void Postfix(PowerConsumer __instance)
        {
            if (__instance.TileEntity != null)
                UpdateWireColor(__instance.TileEntity);
        }
    }


    // Update pulseColor when power input changes (SP mode)
    [HarmonyPatch(typeof(PowerTrigger))]
    [HarmonyPatch("HandlePowerUpdate")]
    public class PowerTrigger_HandlePowerUpdate
    {
        static void Postfix(PowerConsumer __instance)
        {
            if (__instance.TileEntity != null)
                UpdateWireColor(__instance.TileEntity);
        }
    }

    // Update pulseColor when power input changes (SP mode)
    [HarmonyPatch(typeof(PowerConsumer))]
    [HarmonyPatch("IsPoweredChanged")]
    public class PowerConsumer_IsPoweredChanged
    {
        static void Postfix(PowerConsumer __instance)
        {
            if (__instance.TileEntity != null)
                UpdateWireColor(__instance.TileEntity);
        }
    }

}
