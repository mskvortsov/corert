// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Internal.TypeSystem;

using ILCompiler;
using ILCompiler.DependencyAnalysis.ARM;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// ARM specific portions of ReadyToRunHelperNode
    /// </summary>
    partial class ReadyToRunHelperNode
    {
        protected override void EmitCode(NodeFactory factory, ref ARMEmitter encoder, bool relocsOnly)
        {
            switch (Id)
            {
                case ReadyToRunHelperId.NewHelper:
                    {
                        TypeDesc target = (TypeDesc)Target;
                        encoder.EmitMOV(encoder.TargetRegister.Arg0, factory.ConstructedTypeSymbol(target));
                        encoder.EmitJMP(factory.ExternSymbol(JitHelper.GetNewObjectHelperForType(target)));
                    }
                    break;

                case ReadyToRunHelperId.VirtualCall:
                    {
                        ARMDebug.EmitHelperNYIAssert(factory, ref encoder, ReadyToRunHelperId.VirtualCall);
                        /*
                       ***
                       NOT TESTED!!!
                       ***
                        MethodDesc targetMethod = (MethodDesc)Target;

                        Debug.Assert(!targetMethod.OwningType.IsInterface);

                        int pointerSize = factory.Target.PointerSize;

                        int slot = 0;
                        if (!relocsOnly)
                        {
                            slot = VirtualMethodSlotHelper.GetVirtualMethodSlot(factory, targetMethod);
                            Debug.Assert(slot != -1);
                        }

                        encoder.EmitLDR(encoder.TargetRegister.InterproceduralScratch, encoder.TargetRegister.Arg0, 0);
                        encoder.EmitLDR(encoder.TargetRegister.InterproceduralScratch, encoder.TargetRegister.InterproceduralScratch,
                                        (short)(EETypeNode.GetVTableOffset(pointerSize) + (slot * pointerSize)));
                        encoder.EmitJMP(encoder.TargetRegister.InterproceduralScratch);
                        */
                    }
                    break;

                case ReadyToRunHelperId.IsInstanceOf:
                    {
                        TypeDesc target = (TypeDesc)Target;
                        encoder.EmitMOV(encoder.TargetRegister.Arg1, factory.NecessaryTypeSymbol(target));
                        encoder.EmitJMP(factory.ExternSymbol(JitHelper.GetCastingHelperNameForType(target, false)));
                    }
                    break;

                case ReadyToRunHelperId.CastClass:
                    {
                        TypeDesc target = (TypeDesc)Target;
                        encoder.EmitMOV(encoder.TargetRegister.Arg1, factory.NecessaryTypeSymbol(target));
                        encoder.EmitJMP(factory.ExternSymbol(JitHelper.GetCastingHelperNameForType(target, true)));
                    }
                    break;

                case ReadyToRunHelperId.NewArr1:
                    {
                        TypeDesc target = (TypeDesc)Target;
                        encoder.EmitMOV(encoder.TargetRegister.Arg1, encoder.TargetRegister.Arg0);
                        encoder.EmitMOV(encoder.TargetRegister.Arg0, factory.ConstructedTypeSymbol(target));
                        encoder.EmitJMP(factory.ExternSymbol(JitHelper.GetNewArrayHelperForType(target)));
                    }
                    break;

                case ReadyToRunHelperId.GetNonGCStaticBase:
                    {
                        MetadataType target = (MetadataType)Target;
                        bool hasLazyStaticConstructor = factory.TypeSystemContext.HasLazyStaticConstructor(target);
                        encoder.EmitMOV(encoder.TargetRegister.Result, factory.TypeNonGCStaticsSymbol(target));

                        if (!hasLazyStaticConstructor)
                        {
                            encoder.EmitRET();
                        }
                        else
                        {
                            // We need to trigger the cctor before returning the base. It is stored at the beginning of the non-GC statics region.
                            encoder.EmitMOV(encoder.TargetRegister.Arg2, factory.TypeNonGCStaticsSymbol(target));
                            encoder.EmitSUB(encoder.TargetRegister.Arg2, ((byte)NonGCStaticsNode.GetClassConstructorContextStorageSize(factory.Target, target)));

                            // cmp [r2 + ptrSize], 1
                            encoder.EmitLDR(encoder.TargetRegister.Arg3, encoder.TargetRegister.Arg2, ((short)factory.Target.PointerSize));
                            encoder.EmitCMP(encoder.TargetRegister.Arg3, ((byte)1));
                            // return if cmp
                            encoder.EmitRETIfEqual();

                            encoder.EmitMOV(encoder.TargetRegister.Arg1, encoder.TargetRegister.Result);
                            encoder.EmitMOV(encoder.TargetRegister.Arg0/*Result*/, encoder.TargetRegister.Arg2);
                            encoder.EmitJMP(factory.HelperEntrypoint(HelperEntrypoint.EnsureClassConstructorRunAndReturnNonGCStaticBase));
                        }
                    }
                    break;

                case ReadyToRunHelperId.GetThreadStaticBase:
                    {
                        ARMDebug.EmitHelperNYIAssert(factory, ref encoder, ReadyToRunHelperId.GetThreadStaticBase);
                        /*
                       ***
                       NOT TESTED!!!
                       ***
                        MetadataType target = (MetadataType)Target;
                        encoder.EmitMOV(encoder.TargetRegister.Arg2, factory.TypeThreadStaticIndex(target));

                        // First arg: address of the TypeManager slot that provides the helper with
                        // information about module index and the type manager instance (which is used
                        // for initialization on first access).
                        encoder.EmitLDR(encoder.TargetRegister.Arg0, encoder.TargetRegister.Arg2);

                        // Second arg: index of the type in the ThreadStatic section of the modules
                        encoder.EmitLDR(encoder.TargetRegister.Arg1, encoder.TargetRegister.Arg2, ((short)factory.Target.PointerSize));

                        if (!factory.TypeSystemContext.HasLazyStaticConstructor(target))
                        {
                            encoder.EmitJMP(factory.HelperEntrypoint(HelperEntrypoint.GetThreadStaticBaseForType));
                        }
                        else
                        {
                            encoder.EmitMOV(encoder.TargetRegister.Arg2, factory.TypeNonGCStaticsSymbol(target));
                            encoder.EmitSUB(encoder.TargetRegister.Arg2, (byte)(NonGCStaticsNode.GetClassConstructorContextStorageSize(factory.Target, target)));
                            // TODO: performance optimization - inline the check verifying whether we need to trigger the cctor
                            encoder.EmitJMP(factory.HelperEntrypoint(HelperEntrypoint.EnsureClassConstructorRunAndReturnThreadStaticBase));
                        }
                        */
                    }
                    break;

                case ReadyToRunHelperId.GetGCStaticBase:
                    {
                        MetadataType target = (MetadataType)Target;
                        encoder.EmitMOV(encoder.TargetRegister.Result, factory.TypeGCStaticsSymbol(target));
                        encoder.EmitLDR(encoder.TargetRegister.Result, encoder.TargetRegister.Result);
                        encoder.EmitLDR(encoder.TargetRegister.Result, encoder.TargetRegister.Result);
                        if (!factory.TypeSystemContext.HasLazyStaticConstructor(target))
                        {
                            encoder.EmitRET();
                        }
                        else
                        {
                            // We need to trigger the cctor before returning the base. It is stored at the beginning of the non-GC statics region.
                            encoder.EmitMOV(encoder.TargetRegister.Arg2, factory.TypeNonGCStaticsSymbol(target));
                            // Get cctor pointer: offset is usually equal to the double size of the pointer, therefore we can use arm sub imm
                            encoder.EmitSUB(encoder.TargetRegister.Arg2, (byte)(NonGCStaticsNode.GetClassConstructorContextStorageSize(factory.Target, target)));
                            // cmp [r2 + ptrSize], 1
                            encoder.EmitLDR(encoder.TargetRegister.Arg3, encoder.TargetRegister.Arg2, ((short)factory.Target.PointerSize));
                            encoder.EmitCMP(encoder.TargetRegister.Arg3, (byte)1);
                            // return if cmp
                            encoder.EmitRETIfEqual();

                            encoder.EmitMOV(encoder.TargetRegister.Arg1, encoder.TargetRegister.Result);
                            encoder.EmitMOV(encoder.TargetRegister.Arg0/*Result*/, encoder.TargetRegister.Arg2);
                            encoder.EmitJMP(factory.HelperEntrypoint(HelperEntrypoint.EnsureClassConstructorRunAndReturnGCStaticBase));
                        }
                    }
                    break;

                case ReadyToRunHelperId.DelegateCtor:
                    {
                        ARMDebug.EmitHelperNYIAssert(factory, ref encoder, ReadyToRunHelperId.DelegateCtor);
                        /*
                       ***
                       NOT TESTED!!!
                       ***
                        DelegateCreationInfo target = (DelegateCreationInfo)Target;

                        if (target.TargetNeedsVTableLookup)
                        {
                            encoder.EmitLDR(encoder.TargetRegister.Arg2, encoder.TargetRegister.Arg1);

                            int slot = 0;
                            if (!relocsOnly)
                                slot = VirtualMethodSlotHelper.GetVirtualMethodSlot(factory, target.TargetMethod);

                            Debug.Assert(slot != -1);
                            encoder.EmitLDR(encoder.TargetRegister.Arg2, encoder.TargetRegister.Arg2,
                                            ((short)(EETypeNode.GetVTableOffset(factory.Target.PointerSize) + (slot * factory.Target.PointerSize))));
                        }
                        else
                        {
                            ISymbolNode targetMethodNode = target.GetTargetNode(factory);
                            encoder.EmitMOV(encoder.TargetRegister.Arg2, target.GetTargetNode(factory));
                        }

                        if (target.Thunk != null)
                        {
                            Debug.Assert(target.Constructor.Method.Signature.Length == 3);
                            encoder.EmitMOV(encoder.TargetRegister.Arg3, target.Thunk);
                        }
                        else
                        {
                            Debug.Assert(target.Constructor.Method.Signature.Length == 2);
                        }

                        encoder.EmitJMP(target.Constructor);
                        */
                    }
                    break;

                case ReadyToRunHelperId.ResolveVirtualFunction:
                    {
                        ARMDebug.EmitHelperNYIAssert(factory, ref encoder, ReadyToRunHelperId.ResolveVirtualFunction);
                        /*
                       ***
                       NOT TESTED!!!
                       ***
                        MethodDesc targetMethod = (MethodDesc)Target;
                        if (targetMethod.OwningType.IsInterface)
                        {
                            encoder.EmitMOV(encoder.TargetRegister.Arg1, factory.InterfaceDispatchCell(targetMethod));
                            encoder.EmitJMP(factory.ExternSymbol("RhpResolveInterfaceMethod"));
                        }
                        else
                        {
                            if (relocsOnly)
                                break;

                            encoder.EmitLDR(encoder.TargetRegister.Result, encoder.TargetRegister.Arg0);

                            int slot = VirtualMethodSlotHelper.GetVirtualMethodSlot(factory, targetMethod);
                            Debug.Assert(slot != -1);
                            encoder.EmitLDR(encoder.TargetRegister.Result, encoder.TargetRegister.Result,
                                            ((short)(EETypeNode.GetVTableOffset(factory.Target.PointerSize) + (slot * factory.Target.PointerSize))));
                            encoder.EmitRET();
                        }
                        */
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
