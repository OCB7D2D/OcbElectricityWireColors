using Mono.Cecil;
using SDX.Compiler;
using System;
using System.Linq;

// SDX "compile" time patch to alter dll (EAC incompatible)
public class ElectricityWireColorsPatch : IPatcherMod
{

    public void PatchTileEntityPowered(ModuleDefinition module)
    {
        var type = MakeTypePublic(module.Types.First(d => d.Name == "TileEntityPowered"));
        TypeReference boolTypeRef = module.ImportReference(typeof(bool));
        TypeReference wireTypeRef = module.ImportReference(typeof(IWireNode));
        type.Fields.Add(new FieldDefinition("ParentWire", FieldAttributes.Public, wireTypeRef));
        type.Fields.Add(new FieldDefinition("isParentSameType", FieldAttributes.Public, boolTypeRef));
    }

    public bool Patch(ModuleDefinition module)
    {
        Console.WriteLine("Applying OCB Electricity Wire Colors Patch");

        PatchTileEntityPowered(module);

        return true;
    }

    // Called after the patching process and after scripts are compiled.
    // Used to link references between both assemblies
    // Return true if successful
    public bool Link(ModuleDefinition gameModule, ModuleDefinition modModule)
    {
        return true;
    }


    // Helper functions to allow us to access and change variables that are otherwise unavailable.
    private void SetMethodToVirtual(MethodDefinition method)
    {
        method.IsVirtual = true;
    }

    private TypeDefinition MakeTypePublic(TypeDefinition type)
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

    private void SetFieldToPublic(FieldDefinition field)
    {
        field.IsFamily = false;
        field.IsPrivate = false;
        field.IsPublic = true;

    }
    private void SetMethodToPublic(MethodDefinition field, bool force = false)
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
