// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b05617
{
}
.class _simple {

.method static int32 main() {
.entrypoint
.maxstack	100

.locals (int32)
ldc.i4 1
stloc.0

 ldc.i4.2
 ldc.i4.2
 ldc.r4 2.564783e+007
 conv.u1
 ldc.i4.8
 conv.r4
 ldc.r4 6.558334e+008
 mul
 conv.i1
 ldc.i4.M1
 ldloc 0
 add
 mul
 sub
 sub
 or
ldc.i8 4294967086
conv.u4
sub

ldc.i4 100
add
ret
}
}
