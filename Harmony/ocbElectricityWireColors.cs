using DMT;
using HarmonyLib;
using UnityEngine;
using System.Reflection;

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
    public static void UpdatePulseColor(IWireNode wire, Color col)
    {
        if (wire is FastWireNode fast)
        {
            if (fast.pulseColor != col)
            {
                fast.SetPulseColor(col);
                fast.TogglePulse(WireManager.Instance.ShowPulse);
            }
        }
        else if (wire is WireNode node)
        {
            if (node.pulseColor != col)
            {
                node.SetPulseColor(col);
                node.TogglePulse(WireManager.Instance.ShowPulse);
            }
        }
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
            if (__instance.wireDataList.Count != __instance.currentWireNodes.Count) return;
            for (int i = 0; i < __instance.wireDataList.Count; ++i)
            {
                Vector3i data = __instance.wireDataList[i];
                IWireNode wire = __instance.currentWireNodes[i];
                if (GameManager.Instance.World.GetChunkFromWorldPos(data) is Chunk chunk)
                {
                    TileEntityPowered tileEntity = GameManager.Instance.World
                        .GetTileEntity(chunk.ClrIdx, data) as TileEntityPowered;
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
        }
    }

    // Update pulseColor on each tick (maybe add another delay?)
    [HarmonyPatch(typeof(TileEntityPowered))]
    [HarmonyPatch("UpdateTick")]
    public class TileEntityPowerSource_UpdateTick
    {
        static void Postfix(TileEntityPowered __instance)
        {
            // This check is copied from the original function
            if (!((UnityEngine.Object)__instance.BlockTransform != (UnityEngine.Object)null))
                return;
            // Get the power state for this power tile entity
            bool isPowered = __instance.PowerItem != null && __instance.PowerItem.IsPowered;
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
            }
            // Update pulse color (if needed/changed)
            UpdatePulseColor(__instance.ParentWire, pulseColor);
        }
    }

}
