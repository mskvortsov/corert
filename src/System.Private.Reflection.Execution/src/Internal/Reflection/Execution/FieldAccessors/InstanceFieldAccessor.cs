// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using global::System;
using global::System.Threading;
using global::System.Reflection;
using global::System.Diagnostics;
using global::System.Collections.Generic;

using global::Internal.Runtime.Augments;
using global::Internal.Reflection.Execution;
using global::Internal.Reflection.Core.Execution;

namespace Internal.Reflection.Execution.FieldAccessors
{
    internal abstract class InstanceFieldAccessor : FieldAccessor
    {
        public InstanceFieldAccessor(RuntimeTypeHandle declaringTypeHandle, RuntimeTypeHandle fieldTypeHandle)
        {
            this.DeclaringTypeHandle = declaringTypeHandle;
            this.FieldTypeHandle = fieldTypeHandle;
        }

        public abstract override int Offset { get; }

        public sealed override Object GetField(Object obj)
        {
            if (obj == null)
                throw new TargetException(SR.RFLCT_Targ_StatFldReqTarg);
            if (!RuntimeAugments.IsAssignable(obj, this.DeclaringTypeHandle))
                throw new ArgumentException();
            return UncheckedGetField(obj);
        }

        public sealed override object GetFieldDirect(TypedReference typedReference)
        {
            if (RuntimeAugments.IsValueType(this.DeclaringTypeHandle))
            {
                // We're being asked to read a field from the value type pointed to by the TypedReference. This code path
                // avoids boxing that value type by adding this field's offset to the TypedReference's managed pointer.
                Type targetType = TypedReference.GetTargetType(typedReference);
                if (!(targetType.TypeHandle.Equals(this.DeclaringTypeHandle)))
                    throw new ArgumentException();
                return UncheckedGetFieldDirectFromValueType(typedReference);
            }
            else
            {
                // We're being asked to read a field from a reference type. There's no boxing to optimize out in that case so just handle it as 
                // if this was a FieldInfo.GetValue() call.
                object obj = TypedReference.ToObject(typedReference);
                return GetField(obj);
            }
        }

        protected abstract object UncheckedGetFieldDirectFromValueType(TypedReference typedReference);

        public sealed override void SetField(Object obj, Object value, BinderBundle binderBundle)
        {
            if (obj == null)
                throw new TargetException(SR.RFLCT_Targ_StatFldReqTarg);
            if (!RuntimeAugments.IsAssignable(obj, this.DeclaringTypeHandle))
                throw new ArgumentException();
            value = RuntimeAugments.CheckArgument(value, this.FieldTypeHandle, binderBundle);
            UncheckedSetField(obj, value);
        }

        public sealed override void SetFieldDirect(TypedReference typedReference, object value)
        {
            if (RuntimeAugments.IsValueType(this.DeclaringTypeHandle))
            {
                // We're being asked to store a field into the value type pointed to by the TypedReference. This code path
                // bypasses boxing that value type by adding this field's offset to the TypedReference's managed pointer.
                // (Otherwise, the store would go into a useless temporary copy rather than the intended destination.)
                Type targetType = TypedReference.GetTargetType(typedReference);
                if (!(targetType.TypeHandle.Equals(this.DeclaringTypeHandle)))
                    throw new ArgumentException();
                value = RuntimeAugments.CheckArgumentForDirectFieldAccess(value, this.FieldTypeHandle);
                UncheckedSetFieldDirectIntoValueType(typedReference, value);
            }
            else
            {
                // We're being asked to store a field from a reference type. There's no boxing to bypass in that case so just handle it as 
                // if this was a FieldInfo.SetValue() call (but using SetValueDirect's argument coercing semantics)
                object obj = TypedReference.ToObject(typedReference);
                if (obj == null)
                    throw new TargetException(SR.RFLCT_Targ_StatFldReqTarg);
                if (!RuntimeAugments.IsAssignable(obj, this.DeclaringTypeHandle))
                    throw new ArgumentException();
                value = RuntimeAugments.CheckArgumentForDirectFieldAccess(value, this.FieldTypeHandle);
                UncheckedSetField(obj, value);
            }
        }

        protected abstract void UncheckedSetFieldDirectIntoValueType(TypedReference typedReference, object value);

        protected abstract Object UncheckedGetField(Object obj);
        protected abstract void UncheckedSetField(Object obj, Object value);

        protected RuntimeTypeHandle DeclaringTypeHandle { get; private set; }
        protected RuntimeTypeHandle FieldTypeHandle { get; private set; }
    }
}
