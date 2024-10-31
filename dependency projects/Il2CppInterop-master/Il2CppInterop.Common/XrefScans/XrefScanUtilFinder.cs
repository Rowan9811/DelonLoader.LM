using System.Net;
using System.Runtime.InteropServices;
using Disarm;
using Microsoft.Extensions.Logging;

namespace Il2CppInterop.Common.XrefScans;

internal static class XrefScanUtilFinder
{
    public static IntPtr FindLastRcxReadAddressBeforeCallTo(IntPtr codeStart, IntPtr callTarget)
    {
        var decoder = XrefScanner.DecoderForAddress(codeStart);
        var lastRcxRead = IntPtr.Zero;

        foreach (Arm64Instruction instruction in decoder)
        {
            if (instruction.MnemonicCategory.HasFlag(Arm64MnemonicCategory.Return))
                return IntPtr.Zero;

            if (HasGroup(instruction.Mnemonic, "AArch64_GRP_JUMP"))
                continue;

            if (HasGroup(instruction.Mnemonic, "AArch64_GRP_CALL"))
            {
                var target = ExtractTargetAddress(instruction);
                if ((IntPtr)target == callTarget)
                    return lastRcxRead;
            }

            if (instruction.MnemonicCategory.HasFlag(Arm64MnemonicCategory.Move))
            {
                // seemingly unneeded?
                if (instruction.Op0Kind == Arm64OperandKind.Register && instruction.Op1Kind == Arm64OperandKind.ImmediatePcRelative)
                {
                    var target = (long)instruction.Address + instruction.Op1Imm;
                    lastRcxRead = (IntPtr)target;
                }
            }
        }

        return IntPtr.Zero;
    }

    public static IntPtr FindByteWriteTargetRightAfterCallTo(IntPtr codeStart, IntPtr callTarget)
    {
        var decoder = XrefScanner.DecoderForAddress(codeStart);
        var seenCall = false;

        foreach (Arm64Instruction instruction in decoder)
        {
            if (instruction.MnemonicCategory.HasFlag(Arm64MnemonicCategory.Return))
                return IntPtr.Zero;

            if (HasGroup(instruction.Mnemonic, "AArch64_GRP_JUMP"))
                continue;

            if (HasGroup(instruction.Mnemonic, "AArch64_GRP_CALL"))
            {
                var target = ExtractTargetAddress(instruction);
                if ((IntPtr)target == callTarget)
                {
                    seenCall = true;
                    continue;
                }
            }

            if (instruction.MnemonicCategory.HasFlag(Arm64MnemonicCategory.Move) && seenCall)
            {
                // seemingly unneeded?
                if (instruction.Op0Kind == Arm64OperandKind.Register && instruction.Op1Kind == Arm64OperandKind.ImmediatePcRelative)
                {
                    var target = (long)instruction.Address + instruction.Op1Imm;
                    return (IntPtr)target;
                }
            }
        }

        return IntPtr.Zero;
    }

    public static ulong ExtractTargetAddress(in Arm64Instruction instruction)
    {
        if (instruction.Op0Kind == Arm64OperandKind.None)
        {
            Logger.Instance.LogInformation("Not enough operands to extract target address");
            return 0;
        }

        int lastOperand = -1;
        if (instruction.Op0Kind != Arm64OperandKind.None)
            lastOperand = 0;
        if (instruction.Op1Kind != Arm64OperandKind.None)
            lastOperand = 1;
        if (instruction.Op2Kind != Arm64OperandKind.None)
            lastOperand = 2;
        if (instruction.Op3Kind != Arm64OperandKind.None)
            lastOperand = 3;

        return lastOperand switch
        {
            0 => (ulong)((long)instruction.Address + instruction.Op0Imm),
            1 => (ulong)((long)instruction.Address + instruction.Op1Imm),
            2 => (ulong)((long)instruction.Address + instruction.Op2Imm),
            3 => (ulong)((long)instruction.Address + instruction.Op3Imm),
            _ => 0,
        };
    }

    // group markings stolen from capstone
    public static bool HasGroup(Arm64Mnemonic mnemonic, string group)
    {
        switch (mnemonic)
        {
            case Arm64Mnemonic.B:
                {
                    if (group == "AArch64_GRP_JUMP")
                        return true;
                    if (group == "AArch64_GRP_BRANCH_RELATIVE")
                        return true;
                    return false;
                }
            case Arm64Mnemonic.BC:
                {
                    if (group == "AArch64_GRP_JUMP")
                        return true;
                    if (group == "AArch64_GRP_BRANCH_RELATIVE")
                        return true;
                    return false;
                }
            case Arm64Mnemonic.BL:
                {
                    if (group == "AArch64_GRP_CALL")
                        return true;
                    if (group == "AArch64_GRP_BRANCH_RELATIVE")
                        return true;
                    return false;
                }
            case Arm64Mnemonic.BLR:
                {
                    if (group == "AArch64_GRP_CALL")
                        return true;
                    return false;
                }
            case Arm64Mnemonic.BR:
                {
                    if (group == "AArch64_GRP_JUMP")
                        return true;
                    return false;
                }
            case Arm64Mnemonic.CBNZ:
                {
                    if (group == "AArch64_GRP_JUMP")
                        return true;
                    if (group == "AArch64_GRP_BRANCH_RELATIVE")
                        return true;
                    return false;
                }
            case Arm64Mnemonic.CBZ:
                {
                    if (group == "AArch64_GRP_JUMP")
                        return true;
                    if (group == "AArch64_GRP_BRANCH_RELATIVE")
                        return true;
                    return false;
                }
            case Arm64Mnemonic.TBNZ:
                {
                    if (group == "AArch64_GRP_JUMP")
                        return true;
                    if (group == "AArch64_GRP_BRANCH_RELATIVE")
                        return true;
                    return false;
                }
            case Arm64Mnemonic.TBZ:
                {
                    if (group == "AArch64_GRP_JUMP")
                        return true;
                    if (group == "AArch64_GRP_BRANCH_RELATIVE")
                        return true;
                    return false;
                }
            default: return false;
        }
    }
}
