// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/******************************************************************************
 * This file is auto-generated from a template file by the GenerateTests.csx  *
 * script in tests\src\JIT\HardwareIntrinsics\X86\Shared. In order to make    *
 * changes, please update the corresponding template and run according to the *
 * directions listed in the file.                                             *
 ******************************************************************************/

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;

namespace JIT.HardwareIntrinsics.General._Vector64
{
    public static partial class Program
    {
        [Fact]
        public static void ConditionalSelectUInt16()
        {
            var test = new VectorTernaryOpTest__ConditionalSelectUInt16();

            // Validates basic functionality works, using Unsafe.Read
            test.RunBasicScenario_UnsafeRead();

            // Validates calling via reflection works, using Unsafe.Read
            test.RunReflectionScenario_UnsafeRead();

            // Validates passing a static member works
            test.RunClsVarScenario();

            // Validates passing a local works, using Unsafe.Read
            test.RunLclVarScenario_UnsafeRead();

            // Validates passing the field of a local class works
            test.RunClassLclFldScenario();

            // Validates passing an instance member of a class works
            test.RunClassFldScenario();

            // Validates passing the field of a local struct works
            test.RunStructLclFldScenario();

            // Validates passing an instance member of a struct works
            test.RunStructFldScenario();

            if (!test.Succeeded)
            {
                throw new Exception("One or more scenarios did not complete as expected.");
            }
        }
    }

    public sealed unsafe class VectorTernaryOpTest__ConditionalSelectUInt16
    {
        private struct DataTable
        {
            private byte[] inArray1;
            private byte[] inArray2;
            private byte[] inArray3;
            private byte[] outArray;

            private GCHandle inHandle1;
            private GCHandle inHandle2;
            private GCHandle inHandle3;
            private GCHandle outHandle;

            private ulong alignment;

            public DataTable(UInt16[] inArray1, UInt16[] inArray2, UInt16[] inArray3, UInt16[] outArray, int alignment)
            {
                int sizeOfinArray1 = inArray1.Length * Unsafe.SizeOf<UInt16>();
                int sizeOfinArray2 = inArray2.Length * Unsafe.SizeOf<UInt16>();
                int sizeOfinArray3 = inArray3.Length * Unsafe.SizeOf<UInt16>();
                int sizeOfoutArray = outArray.Length * Unsafe.SizeOf<UInt16>();
                if ((alignment != 32 && alignment != 16 && alignment != 8) || (alignment * 2) < sizeOfinArray1 || (alignment * 2) < sizeOfinArray2 || (alignment * 2) < sizeOfinArray3 || (alignment * 2) < sizeOfoutArray)
                {
                    throw new ArgumentException("Invalid value of alignment");
                }

                this.inArray1 = new byte[alignment * 2];
                this.inArray2 = new byte[alignment * 2];
                this.inArray3 = new byte[alignment * 2];
                this.outArray = new byte[alignment * 2];

                this.inHandle1 = GCHandle.Alloc(this.inArray1, GCHandleType.Pinned);
                this.inHandle2 = GCHandle.Alloc(this.inArray2, GCHandleType.Pinned);
                this.inHandle3 = GCHandle.Alloc(this.inArray3, GCHandleType.Pinned);
                this.outHandle = GCHandle.Alloc(this.outArray, GCHandleType.Pinned);

                this.alignment = (ulong)alignment;

                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray1Ptr), ref Unsafe.As<UInt16, byte>(ref inArray1[0]), (uint)sizeOfinArray1);
                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray2Ptr), ref Unsafe.As<UInt16, byte>(ref inArray2[0]), (uint)sizeOfinArray2);
                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray3Ptr), ref Unsafe.As<UInt16, byte>(ref inArray3[0]), (uint)sizeOfinArray3);
            }

            public void* inArray1Ptr => Align((byte*)(inHandle1.AddrOfPinnedObject().ToPointer()), alignment);
            public void* inArray2Ptr => Align((byte*)(inHandle2.AddrOfPinnedObject().ToPointer()), alignment);
            public void* inArray3Ptr => Align((byte*)(inHandle3.AddrOfPinnedObject().ToPointer()), alignment);
            public void* outArrayPtr => Align((byte*)(outHandle.AddrOfPinnedObject().ToPointer()), alignment);

            public void Dispose()
            {
                inHandle1.Free();
                inHandle2.Free();
                inHandle3.Free();
                outHandle.Free();
            }

            private static unsafe void* Align(byte* buffer, ulong expectedAlignment)
            {
                return (void*)(((ulong)buffer + expectedAlignment - 1) & ~(expectedAlignment - 1));
            }
        }

        private struct TestStruct
        {
            public Vector64<UInt16> _fld1;
            public Vector64<UInt16> _fld2;
            public Vector64<UInt16> _fld3;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetUInt16(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<UInt16>, byte>(ref testStruct._fld1), ref Unsafe.As<UInt16, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector64<UInt16>>());
                for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetUInt16(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<UInt16>, byte>(ref testStruct._fld2), ref Unsafe.As<UInt16, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector64<UInt16>>());
                for (var i = 0; i < Op3ElementCount; i++) { _data3[i] = TestLibrary.Generator.GetUInt16(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<UInt16>, byte>(ref testStruct._fld3), ref Unsafe.As<UInt16, byte>(ref _data3[0]), (uint)Unsafe.SizeOf<Vector64<UInt16>>());

                return testStruct;
            }

            public void RunStructFldScenario(VectorTernaryOpTest__ConditionalSelectUInt16 testClass)
            {
                var result = Vector64.ConditionalSelect(_fld1, _fld2, _fld3);

                Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                testClass.ValidateResult(_fld1, _fld2, _fld3, testClass._dataTable.outArrayPtr);
            }
        }

        private static readonly int LargestVectorSize = 8;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector64<UInt16>>() / sizeof(UInt16);
        private static readonly int Op2ElementCount = Unsafe.SizeOf<Vector64<UInt16>>() / sizeof(UInt16);
        private static readonly int Op3ElementCount = Unsafe.SizeOf<Vector64<UInt16>>() / sizeof(UInt16);
        private static readonly int RetElementCount = Unsafe.SizeOf<Vector64<UInt16>>() / sizeof(UInt16);

        private static UInt16[] _data1 = new UInt16[Op1ElementCount];
        private static UInt16[] _data2 = new UInt16[Op2ElementCount];
        private static UInt16[] _data3 = new UInt16[Op3ElementCount];

        private static Vector64<UInt16> _clsVar1;
        private static Vector64<UInt16> _clsVar2;
        private static Vector64<UInt16> _clsVar3;

        private Vector64<UInt16> _fld1;
        private Vector64<UInt16> _fld2;
        private Vector64<UInt16> _fld3;

        private DataTable _dataTable;

        static VectorTernaryOpTest__ConditionalSelectUInt16()
        {
            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetUInt16(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<UInt16>, byte>(ref _clsVar1), ref Unsafe.As<UInt16, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector64<UInt16>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetUInt16(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<UInt16>, byte>(ref _clsVar2), ref Unsafe.As<UInt16, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector64<UInt16>>());
            for (var i = 0; i < Op3ElementCount; i++) { _data3[i] = TestLibrary.Generator.GetUInt16(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<UInt16>, byte>(ref _clsVar3), ref Unsafe.As<UInt16, byte>(ref _data3[0]), (uint)Unsafe.SizeOf<Vector64<UInt16>>());
        }

        public VectorTernaryOpTest__ConditionalSelectUInt16()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetUInt16(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<UInt16>, byte>(ref _fld1), ref Unsafe.As<UInt16, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector64<UInt16>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetUInt16(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<UInt16>, byte>(ref _fld2), ref Unsafe.As<UInt16, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector64<UInt16>>());
            for (var i = 0; i < Op3ElementCount; i++) { _data3[i] = TestLibrary.Generator.GetUInt16(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<UInt16>, byte>(ref _fld3), ref Unsafe.As<UInt16, byte>(ref _data3[0]), (uint)Unsafe.SizeOf<Vector64<UInt16>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetUInt16(); }
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetUInt16(); }
            for (var i = 0; i < Op3ElementCount; i++) { _data3[i] = TestLibrary.Generator.GetUInt16(); }
            _dataTable = new DataTable(_data1, _data2, _data3, new UInt16[RetElementCount], LargestVectorSize);
        }

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = Vector64.ConditionalSelect(
                Unsafe.Read<Vector64<UInt16>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector64<UInt16>>(_dataTable.inArray2Ptr),
                Unsafe.Read<Vector64<UInt16>>(_dataTable.inArray3Ptr)
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var method = typeof(Vector64).GetMethod(nameof(Vector64.ConditionalSelect), new Type[] {
                typeof(Vector64<UInt16>),
                typeof(Vector64<UInt16>),
                typeof(Vector64<UInt16>)
            });

            if (method is null)
            {
                method = typeof(Vector64).GetMethod(nameof(Vector64.ConditionalSelect), 1, new Type[] {
                    typeof(Vector64<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
                    typeof(Vector64<>).MakeGenericType(Type.MakeGenericMethodParameter(0)),
                    typeof(Vector64<>).MakeGenericType(Type.MakeGenericMethodParameter(0))
                });
            }

            if (method.IsGenericMethodDefinition)
            {
                method = method.MakeGenericMethod(typeof(UInt16));
            }

            var result = method.Invoke(null, new object[] {
                Unsafe.Read<Vector64<UInt16>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector64<UInt16>>(_dataTable.inArray2Ptr),
                Unsafe.Read<Vector64<UInt16>>(_dataTable.inArray3Ptr)
            });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector64<UInt16>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, _dataTable.inArray3Ptr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = Vector64.ConditionalSelect(
                _clsVar1,
                _clsVar2,
                _clsVar3
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar1, _clsVar2, _clsVar3, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var op1 = Unsafe.Read<Vector64<UInt16>>(_dataTable.inArray1Ptr);
            var op2 = Unsafe.Read<Vector64<UInt16>>(_dataTable.inArray2Ptr);
            var op3 = Unsafe.Read<Vector64<UInt16>>(_dataTable.inArray3Ptr);
            var result = Vector64.ConditionalSelect(op1, op2, op3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(op1, op2, op3, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new VectorTernaryOpTest__ConditionalSelectUInt16();
            var result = Vector64.ConditionalSelect(test._fld1, test._fld2, test._fld3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, test._fld3, _dataTable.outArrayPtr);
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = Vector64.ConditionalSelect(_fld1, _fld2, _fld3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_fld1, _fld2, _fld3, _dataTable.outArrayPtr);
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = Vector64.ConditionalSelect(test._fld1, test._fld2, test._fld3);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, test._fld2, test._fld3, _dataTable.outArrayPtr);
        }

        public void RunStructFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructFldScenario));

            var test = TestStruct.Create();
            test.RunStructFldScenario(this);
        }

        private void ValidateResult(Vector64<UInt16> op1, Vector64<UInt16> op2, Vector64<UInt16> op3, void* result, [CallerMemberName] string method = "")
        {
            UInt16[] inArray1 = new UInt16[Op1ElementCount];
            UInt16[] inArray2 = new UInt16[Op2ElementCount];
            UInt16[] inArray3 = new UInt16[Op3ElementCount];
            UInt16[] outArray = new UInt16[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<UInt16, byte>(ref inArray1[0]), op1);
            Unsafe.WriteUnaligned(ref Unsafe.As<UInt16, byte>(ref inArray2[0]), op2);
            Unsafe.WriteUnaligned(ref Unsafe.As<UInt16, byte>(ref inArray3[0]), op3);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector64<UInt16>>());

            ValidateResult(inArray1, inArray2, inArray3, outArray, method);
        }

        private void ValidateResult(void* op1, void* op2, void* op3, void* result, [CallerMemberName] string method = "")
        {
            UInt16[] inArray1 = new UInt16[Op1ElementCount];
            UInt16[] inArray2 = new UInt16[Op2ElementCount];
            UInt16[] inArray3 = new UInt16[Op3ElementCount];
            UInt16[] outArray = new UInt16[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(op1), (uint)Unsafe.SizeOf<Vector64<UInt16>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref inArray2[0]), ref Unsafe.AsRef<byte>(op2), (uint)Unsafe.SizeOf<Vector64<UInt16>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref inArray3[0]), ref Unsafe.AsRef<byte>(op3), (uint)Unsafe.SizeOf<Vector64<UInt16>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<UInt16, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector64<UInt16>>());

            ValidateResult(inArray1, inArray2, inArray3, outArray, method);
        }

        private void ValidateResult(UInt16[] firstOp, UInt16[] secondOp, UInt16[] thirdOp, UInt16[] result, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            if (result[0] != (ushort)((secondOp[0] & firstOp[0]) | (thirdOp[0] & ~firstOp[0])))
            {
                succeeded = false;
            }
            else
            {
                for (var i = 1; i < RetElementCount; i++)
                {
                    if (result[i] != (ushort)((secondOp[i] & firstOp[i]) | (thirdOp[i] & ~firstOp[i])))
                    {
                        succeeded = false;
                        break;
                    }
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Vector64)}.{nameof(Vector64.ConditionalSelect)}<UInt16>(Vector64<UInt16>, Vector64<UInt16>, Vector64<UInt16>): {method} failed:");
                TestLibrary.TestFramework.LogInformation($" firstOp: ({string.Join(", ", firstOp)})");
                TestLibrary.TestFramework.LogInformation($"secondOp: ({string.Join(", ", secondOp)})");
                TestLibrary.TestFramework.LogInformation($" thirdOp: ({string.Join(", ", thirdOp)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
