using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

public class ElectricityWireColors : IModApi
{

    // Dynamic accessors for private fields
    static FieldInfo fieldCurrentCureNodes;

    // Entry class for A20 patching
    public void InitMod(Mod mod)
    {
        Log.Out(" Loading Patch: " + this.GetType().ToString());
        fieldCurrentCureNodes = AccessTools.Field(typeof(TileEntityPowered), "currentWireNodes");
        new Harmony(GetType().ToString()).PatchAll(Assembly.GetExecutingAssembly());
    }

    // Update wire pulse color value for parent connection of tile entity.
    // Doesn't actually update the color on the mesh (call `TogglePulse`)
    private static void UpdateWireColor(IWireNode wire, TileEntityPowered te)
    {
        if (te == null || wire == null) return;
        PowerItem pitem = te.GetPowerItem();
        if (pitem.Parent != null && pitem.PowerItemType == pitem.Parent.PowerItemType)
        {
            if (te.PowerItemType == PowerItem.PowerItemTypes.TripWireRelay)
            {
                wire.SetPulseColor(te.IsPowered ? Color.magenta : Color.cyan);
            }
            else if (te.PowerItemType == PowerItem.PowerItemTypes.ElectricWireRelay)
            {
                wire.SetPulseColor(te.IsPowered ? Color.red : Color.blue);
            }
            else
            {
                wire.SetPulseColor(te.IsPowered ? Color.yellow : Color.gray);
            }
        }
        else
        {
            wire.SetPulseColor(te.IsPowered ? Color.yellow : Color.gray);
        }
    }

    // Each tile entity only has one parent connection but unfortunately
    // vanilla game doesn't keep track of it on the tile entity object.
    // Unfortunately also only the power item has the parent connection.
    private static void UpdateParentWire(PowerItem item)
    {
        if (item == null || item.Parent == null) return;
        // Get all wires of the parent (ours included)
        // Would be cool if parent wire would be stored here
        List<IWireNode> wires = fieldCurrentCureNodes.
            GetValue(item.Parent.TileEntity) as List<IWireNode>;
        // Now process all wires of parent
        foreach (var wire in wires)
        {
            // Check if the wire has the same end position
            // Note: we could probably get rid of this check
            // Would do a little more work, but still correct!?
            if (wire.GetEndPosition() != item.Position) continue;
            // If so, update the wire pulse color
            UpdateWireColor(wire, item.TileEntity);
            if (WireManager.HasInstance == false) continue;
            wire.TogglePulse(WireManager.Instance.ShowPulse);
        }
    }

    // This is mainly here to ensure correctness
    // Also needed when map is initially loaded
    [HarmonyPatch(typeof(FastWireNode))]
    [HarmonyPatch("TogglePulse")]
    public class FastWireNode_SetPulseColor
    {
        static void Prefix(FastWireNode __instance, bool isOn)
        {
            if (isOn == false) return;
            World world = GameManager.Instance.World;
            Vector3i pos = new Vector3i(__instance.GetEndPosition());
            if (world.GetChunkFromWorldPos(pos) is Chunk chunk)
            {
                if (world.GetTileEntity(chunk.ClrIdx, pos) is TileEntityPowered tileEntity)
                {
                    UpdateWireColor(__instance, tileEntity);
                }
            }
        }
    }

    // Update pulse color when power changed
    // Called when wires get dis/reconnected
    [HarmonyPatch(typeof(PowerConsumer))]
    [HarmonyPatch("IsPoweredChanged")]
    public class PowerConsumer_IsPoweredChanged
    {
        static void Postfix(PowerConsumer __instance,
            bool newPowered, ref bool ___isPowered)
        {
            // Sometimes this is set right after we ran
            // Seems safe to assume that this will stay
            ___isPowered = newPowered;
            UpdateParentWire(__instance);
        }
    }

    // Update pulse color when power changed
    // Called when wires get dis/reconnected
    [HarmonyPatch(typeof(PowerItem))]
    [HarmonyPatch("IsPoweredChanged")]
    public class PowerItem_IsPoweredChanged
    {
        static void Postfix(PowerConsumer __instance,
            bool newPowered, ref bool ___isPowered)
        {
            // Sometimes this is set right after we ran
            // Seems safe to assume that this will stay
            ___isPowered = newPowered;
            UpdateParentWire(__instance);
        }
    }

    // Update pulse color when power is triggered
    [HarmonyPatch(typeof(PowerTrigger))]
    [HarmonyPatch("HandlePowerUpdate")]
    public class PowerTrigger_HandlePowerUpdate
    {
        static void Postfix(PowerTrigger __instance)
        {
            UpdateParentWire(__instance);
        }
    }

}
