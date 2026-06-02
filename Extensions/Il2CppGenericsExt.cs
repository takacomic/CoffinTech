namespace CoffinTech.Extensions;

public static class Il2CppGenericsExt
{
    
    public static List<T> ToList<T>(this Il2CppSystem.Collections.Generic.List<T> il2CppList) => [..il2CppList];
}