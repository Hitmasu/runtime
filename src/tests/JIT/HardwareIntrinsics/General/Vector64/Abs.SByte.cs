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
        public static void AbsSByte()
        {
            var test = new VectorUnaryOpTest__AbsSByte();

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

    public sealed unsafe class VectorUnaryOpTest__AbsSByte
    {
        private struct DataTable
        {
            private byte[] inArray1;
            private byte[] outArray;

            private GCHandle inHandle1;
            private GCHandle outHandle;

            private ulong alignment;

            public DataTable(SByte[] inArray1, SByte[] outArray, int alignment)
            {
                int sizeOfinArray1 = inArray1.Length * Unsafe.SizeOf<SByte>();
                int sizeOfoutArray = outArray.Length * Unsafe.SizeOf<SByte>();
                if ((alignment != 32 && alignment != 16 && alignment != 8) || (alignment * 2) < sizeOfinArray1 || (alignment * 2) < sizeOfoutArray)
                {
                    throw new ArgumentException("Invalid value of alignment");
                }

                this.inArray1 = new byte[alignment * 2];
                this.outArray = new byte[alignment * 2];

                this.inHandle1 = GCHandle.Alloc(this.inArray1, GCHandleType.Pinned);
                this.outHandle = GCHandle.Alloc(this.outArray, GCHandleType.Pinned);

                this.alignment = (ulong)alignment;

                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray1Ptr), ref Unsafe.As<SByte, byte>(ref inArray1[0]), (uint)sizeOfinArray1);
            }

            public void* inArray1Ptr => Align((byte*)(inHandle1.AddrOfPinnedObject().ToPointer()), alignment);
            public void* outArrayPtr => Align((byte*)(outHandle.AddrOfPinnedObject().ToPointer()), alignment);

            public void Dispose()
            {
                inHandle1.Free();
                outHandle.Free();
            }

            private static unsafe void* Align(byte* buffer, ulong expectedAlignment)
            {
                return (void*)(((ulong)buffer + expectedAlignment - 1) & ~(expectedAlignment - 1));
            }
        }

        private struct TestStruct
        {
            public Vector64<SByte> _fld1;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetSByte(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<SByte>, byte>(ref testStruct._fld1), ref Unsafe.As<SByte, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector64<SByte>>());

                return testStruct;
            }

            public void RunStructFldScenario(VectorUnaryOpTest__AbsSByte testClass)
            {
                var result = Vector64.Abs(_fld1);

                Unsafe.Write(testClass._dataTable.outArrayPtr, result);
                testClass.ValidateResult(_fld1, testClass._dataTable.outArrayPtr);
            }
        }

        private static readonly int LargestVectorSize = 8;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector64<SByte>>() / sizeof(SByte);
        private static readonly int RetElementCount = Unsafe.SizeOf<Vector64<SByte>>() / sizeof(SByte);

        private static SByte[] _data1 = new SByte[Op1ElementCount];

        private static Vector64<SByte> _clsVar1;

        private Vector64<SByte> _fld1;

        private DataTable _dataTable;

        static VectorUnaryOpTest__AbsSByte()
        {
            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetSByte(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<SByte>, byte>(ref _clsVar1), ref Unsafe.As<SByte, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector64<SByte>>());
        }

        public VectorUnaryOpTest__AbsSByte()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetSByte(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector64<SByte>, byte>(ref _fld1), ref Unsafe.As<SByte, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector64<SByte>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetSByte(); }
            _dataTable = new DataTable(_data1, new SByte[RetElementCount], LargestVectorSize);
        }

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = Vector64.Abs(
                Unsafe.Read<Vector64<SByte>>(_dataTable.inArray1Ptr)
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.outArrayPtr);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var method = typeof(Vector64).GetMethod(nameof(Vector64.Abs), new Type[] {
                typeof(Vector64<SByte>)
            });

            if (method is null)
            {
                method = typeof(Vector64).GetMethod(nameof(Vector64.Abs), 1, new Type[] {
                    typeof(Vector64<>).MakeGenericType(Type.MakeGenericMethodParameter(0))
                });
            }

            if (method.IsGenericMethodDefinition)
            {
                method = method.MakeGenericMethod(typeof(SByte));
            }

            var result = method.Invoke(null, new object[] {
                Unsafe.Read<Vector64<SByte>>(_dataTable.inArray1Ptr)
            });

            Unsafe.Write(_dataTable.outArrayPtr, (Vector64<SByte>)(result));
            ValidateResult(_dataTable.inArray1Ptr, _dataTable.outArrayPtr);
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = Vector64.Abs(
                _clsVar1
            );

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_clsVar1, _dataTable.outArrayPtr);
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var op1 = Unsafe.Read<Vector64<SByte>>(_dataTable.inArray1Ptr);
            var result = Vector64.Abs(op1);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(op1, _dataTable.outArrayPtr);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new VectorUnaryOpTest__AbsSByte();
            var result = Vector64.Abs(test._fld1);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, _dataTable.outArrayPtr);
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = Vector64.Abs(_fld1);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(_fld1, _dataTable.outArrayPtr);
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = Vector64.Abs(test._fld1);

            Unsafe.Write(_dataTable.outArrayPtr, result);
            ValidateResult(test._fld1, _dataTable.outArrayPtr);
        }

        public void RunStructFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructFldScenario));

            var test = TestStruct.Create();
            test.RunStructFldScenario(this);
        }

        private void ValidateResult(Vector64<SByte> op1, void* result, [CallerMemberName] string method = "")
        {
            SByte[] inArray1 = new SByte[Op1ElementCount];
            SByte[] outArray = new SByte[RetElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<SByte, byte>(ref inArray1[0]), op1);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<SByte, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector64<SByte>>());

            ValidateResult(inArray1, outArray, method);
        }

        private void ValidateResult(void* op1, void* result, [CallerMemberName] string method = "")
        {
            SByte[] inArray1 = new SByte[Op1ElementCount];
            SByte[] outArray = new SByte[RetElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<SByte, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(op1), (uint)Unsafe.SizeOf<Vector64<SByte>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<SByte, byte>(ref outArray[0]), ref Unsafe.AsRef<byte>(result), (uint)Unsafe.SizeOf<Vector64<SByte>>());

            ValidateResult(inArray1, outArray, method);
        }

        private void ValidateResult(SByte[] firstOp, SByte[] result, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            if (result[0] != Math.Abs(firstOp[0]))
            {
                succeeded = false;
            }
            else
            {
                for (var i = 1; i < RetElementCount; i++)
                {
                    if (result[i] != Math.Abs(firstOp[i]))
                    {
                        succeeded = false;
                        break;
                    }
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Vector64)}.{nameof(Vector64.Abs)}<SByte>(Vector64<SByte>): {method} failed:");
                TestLibrary.TestFramework.LogInformation($" firstOp: ({string.Join(", ", firstOp)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
