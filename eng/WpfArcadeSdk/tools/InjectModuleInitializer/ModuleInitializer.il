.class private auto ansi '<Module>'
{
    .method private hidebysig specialname rtspecialname static void .cctor () cil managed 
    {
      // Code size       76 (0x4c)
      .maxstack  3
      
      .locals init (class [mscorlib]System.Type V_0, bool V_1, class [mscorlib]System.Reflection.MethodInfo V_2, bool V_3)
      IL_0000:  nop
      IL_0001:  call       class [mscorlib]System.Reflection.Assembly [mscorlib]System.Reflection.Assembly::GetExecutingAssembly()
      IL_0006:  ldstr      "ModuleInitializer"
      IL_000b:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Reflection.Assembly::GetType(string)
      IL_0010:  stloc.0
      IL_0011:  ldloc.0
      IL_0012:  ldnull
      IL_0013:  call       bool [mscorlib]System.Type::op_Inequality(class [mscorlib]System.Type, class [mscorlib]System.Type)
      IL_0018:  stloc.1
      IL_0019:  ldloc.1
      IL_001a:  brfalse.s  IL_004b

      IL_001c:  nop
      IL_001d:  ldloc.0
      IL_001e:  ldstr      "Initialize"
      IL_0023:  ldc.i4.s   24
      IL_0025:  callvirt   instance class [mscorlib]System.Reflection.MethodInfo [mscorlib]System.Type::GetMethod(string, valuetype [mscorlib]System.Reflection.BindingFlags)
      IL_002a:  stloc.2
      IL_002b:  ldloc.2
      IL_002c:  ldnull
      IL_002d:  call       bool [mscorlib]System.Reflection.MethodInfo::op_Inequality(class [mscorlib]System.Reflection.MethodInfo, class [mscorlib]System.Reflection.MethodInfo)
      IL_0032:  stloc.3
      IL_0033:  ldloc.3
      IL_0034:  brfalse.s  IL_004a

      IL_0036:  nop
      .try
      {
        IL_0037:  nop
        IL_0038:  ldloc.2
        IL_0039:  ldnull
        IL_003a:  ldnull
        IL_003b:  callvirt   instance object [mscorlib]System.Reflection.MethodBase::Invoke(object, object[])
        IL_0040:  pop
        IL_0041:  nop
        IL_0042:  leave.s    IL_0049

      }  // end .try
      catch [mscorlib]System.Exception 
      {
        IL_0044:  pop
        IL_0045:  nop
        IL_0046:  nop
        IL_0047:  leave.s    IL_0049

      }  // end handler
      IL_0049:  nop
      IL_004a:  nop
      IL_004b:  ret
    }
}
