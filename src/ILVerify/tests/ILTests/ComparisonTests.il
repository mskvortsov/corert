// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Runtime
{
}

.assembly Comparison
{
}

.class public auto ansi beforefieldinit ComparisonTestsType
       extends [System.Runtime]System.Object
{
    .method static public hidebysig void Comparison.NullWithObjRef_Valid() cil managed
    {
        .locals init (
            class [System.Runtime]System.Object V_0
        )
        ldnull
        ldloc.0
        cgt.un
        pop
        ret
    }

    .method static public hidebysig void Comparison.ObjRefWithNull_Valid() cil managed
    {
        .locals init (
            class [System.Runtime]System.Object V_0
        )
        ldloc.0
        ldnull
        cgt.un
        pop
        ret
    }
}
