using Mono.Cecil;
using System;
using System.Linq;
using System.Collections.Generic;

public static class ElectricityWireColorsPatcher
{

    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

    public static void Patch(AssemblyDefinition assembly)
    {
        Console.WriteLine("Applying OCB Electricity Wire Colors Patch");
        PatchTileEntityPowered(assembly.MainModule);
    }

    public static void PatchTileEntityPowered(ModuleDefinition module)
    {
        var type = MakeTypePublic(module.Types.First(d => d.Name == "TileEntityPowered"));
        TypeReference boolTypeRef = module.ImportReference(typeof(bool));
        // This fails in 7D2DModLauncher with "Could not load file or assembly"
        // TypeReference wireTypeRef = module.ImportReference(typeof(IWireNode));
        TypeReference wireTypeRef = module.Types.First(d => d.Name == "IWireNode");
        type.Fields.Add(new FieldDefinition("ParentWire", FieldAttributes.Public, wireTypeRef));
        type.Fields.Add(new FieldDefinition("isParentSameType", FieldAttributes.Public, boolTypeRef));
    }

    // Called after the patching process and after scripts are compiled.
    // Used to link references between both assemblies
    // Return true if successful
    public static bool Link(ModuleDefinition gameModule, ModuleDefinition modModule)
    {
        return true;
    }


    // Helper functions to allow us to access and change variables that are otherwise unavailable.
    private static void SetMethodToVirtual(MethodDefinition method)
    {
        method.IsVirtual = true;
    }

    private static TypeDefinition MakeTypePublic(TypeDefinition type)
    {
        foreach (var myField in type.Fields)
        {
            SetFieldToPublic(myField);
        }
        foreach (var myMethod in type.Methods)
        {
            SetMethodToPublic(myMethod);
        }

        return type;
    }

    private static void SetFieldToPublic(FieldDefinition field)
    {
        field.IsFamily = false;
        field.IsPrivate = false;
        field.IsPublic = true;

    }
    private static void SetMethodToPublic(MethodDefinition field, bool force = false)
    {
        // Leave protected virtual methods alone to avoid
        // issues with others inheriting from it, as it gives
        // a compile error when protection level mismatches.
        // Unsure if this changes anything on runtime though?
        if (!field.IsFamily || !field.IsVirtual || force) {
            field.IsFamily = false;
            field.IsPrivate = false;
            field.IsPublic = true;
        }
    }

}
