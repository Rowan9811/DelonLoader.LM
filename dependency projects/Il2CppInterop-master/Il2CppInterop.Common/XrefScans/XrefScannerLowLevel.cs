using Disarm;

namespace Il2CppInterop.Common.XrefScans;

public static class XrefScannerLowLevel
{
    public static IEnumerable<IntPtr> JumpTargets(IntPtr codeStart, bool ignoreRetn = false, int instructions = 250)
    {
        var decoder = XrefScanner.DecoderForAddress(codeStart, instructions * 4);

        foreach (var instruction in decoder)
        {
            if (instruction.Mnemonic == Arm64Mnemonic.RET && !ignoreRetn)
                yield break;

            var jump = instruction.Mnemonic is Arm64Mnemonic.B or Arm64Mnemonic.BC or Arm64Mnemonic.BR;
            var call = instruction.Mnemonic is Arm64Mnemonic.BL or Arm64Mnemonic.BLR;

            if ((jump || call) && instruction.MnemonicConditionCode == Arm64ConditionCode.NONE && instruction.FinalOpConditionCode == Arm64ConditionCode.NONE)
            {
                var target = XrefScanUtilFinder.ExtractTargetAddress(instruction);
                yield return (IntPtr)target;
            }
        }
    }
}
