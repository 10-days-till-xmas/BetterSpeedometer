using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static HarmonyLib.AccessTools;
namespace BetterSpeedometer.Patchers;

[HarmonyPatch(typeof(Speedometer))]
public static class SpeedometerPatcher
{

    [HarmonyPostfix]
    [HarmonyPatch("Awake")]
    public static void AwakePostfix(Speedometer __instance, bool ___classicVersion, int ___type)
    {
        ResizeSpeedometer(__instance, ___classicVersion, ___type);
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnEnable")]
    public static void OnEnablePostfix(Speedometer __instance, bool ___classicVersion, int ___type)
    {
        ResizeSpeedometer(__instance, ___classicVersion, ___type);
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnPrefChanged")]
    public static void OnPrefChangedPostfix(Speedometer __instance, string id, object value, int ___type, bool ___classicVersion)
    {
        if (id != "speedometer" || value is not int) return;
        ResizeSpeedometer(__instance, ___classicVersion, ___type);
    }

    private static readonly FieldInfo f_Speedometer_type = Field(typeof(Speedometer), "type");
    [HarmonyTranspiler]
    [HarmonyPatch("FixedUpdate")]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        // ReSharper disable  ReturnValueOfPureMethodIsNotUsed
        // ReSharper disable FormatStringProblem
        var matcher = new CodeMatcher(instructions);

        matcher.Start() // I could transpile out the if check entirely but ehhh
               .MatchForward(useEnd: true,
                    new CodeMatch(OpCodes.Ldstr, "{0:0}"))
               .ThrowIfInvalid("Could not find the first format string in Speedometer.FixedUpdate")
               .RemoveInstruction()
               .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, f_Speedometer_type),
                    CodeInstruction.Call(() => GetStringClassic(0f, 0)))
               .RemoveInstructions(3) // removes IL_00af: ldloc.0 up to IL_00b5: call string [netstandard]System.String::Format(string, object)
               .MatchForward(useEnd: true,
                    new CodeMatch(OpCodes.Ldstr, "SPEED: {0:0.00} {1}/s"))
               .ThrowIfInvalid("Could not find the second format string in Speedometer.FixedUpdate")
               .RemoveInstruction()
               .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, f_Speedometer_type),
                    new CodeInstruction(OpCodes.Ldloc_1),
                    CodeInstruction.Call(() => GetStringStandard(0f, 0, "")))
               .RemoveInstructions(4); // removes IL_00cc: ldloc.0 up to IL_00d3: call string [netstandard]System.String::Format(string, object, object)

        return matcher.InstructionEnumeration();
    }

    // ReSharper disable UnusedMethodReturnValue.Local
    private static string GetStringClassic(float num, int type)
    {
        if (type != SettingsItemBuilderPatcher.SPEEDOMETER_ITEM_INDEX) return $"{num:0}";
        var v = PlayerTracker.Instance!.GetPlayerVelocity(true);
        var horizontal = Vector3.ProjectOnPlane(v, Vector3.up).magnitude;
        var vertical = v.y;
        var speed = v.magnitude;
        return $"({horizontal:0}, {vertical:0})\n{speed:0}";
    }
    private static string GetStringStandard(float num, int type, string unit)
    {
        if (type != SettingsItemBuilderPatcher.SPEEDOMETER_ITEM_INDEX) return $"SPEED: {num:0.00} {unit}/s";
        var v = PlayerTracker.Instance!.GetPlayerVelocity(true);
        var horizontal = Vector3.ProjectOnPlane(v, Vector3.up).magnitude;
        var vertical = v.y;
        var speed = v.magnitude;
        return $"SPEED: ({horizontal:0.00}, {vertical:0.00})\n{speed:0.00} u/s";
    }

    private static void ResizeSpeedometer(Speedometer sm, bool classicVersion, int type)
    {
        Plugin.Logger.LogInfo("Resizing speedometer");
        var rt = sm.gameObject.GetComponent<RectTransform>();
        var tm = sm.textMesh;

        tm.enableWordWrapping = type is not SettingsItemBuilderPatcher.SPEEDOMETER_ITEM_INDEX; // default is true
        switch ((classicVersion, type))
        {
            case (true, SettingsItemBuilderPatcher.SPEEDOMETER_ITEM_INDEX):
                rt.sizeDelta = rt.sizeDelta with { x = 100 };
                tm.fontSize = 18;
                tm.fontSizeMax = 18;
                tm.fontSizeMin = 10;
                rt.pivot = rt.pivot with { x = 0.615f };
                break;
            case (false, SettingsItemBuilderPatcher.SPEEDOMETER_ITEM_INDEX):
                rt.sizeDelta = rt.sizeDelta with { y = 40 };
                tm.fontSize = 14;
                tm.fontSizeMax = 20;
                tm.fontSizeMin = 10;
                break;
            case (true, _): // classic, vanilla
                rt.sizeDelta = rt.sizeDelta with { x = 77.5f };
                tm.fontSize = 22;
                tm.fontSizeMax = 36;
                tm.fontSizeMin = 18;
                rt.pivot = rt.pivot with { x = 0.5f };
                break;
            case (false, _): // standard, vanilla
                rt.sizeDelta = rt.sizeDelta with { y = 25 };
                tm.fontSizeMin = 16;
                tm.fontSizeMax = 72;
                tm.fontSize = 18;
                break;
        }
    }
}