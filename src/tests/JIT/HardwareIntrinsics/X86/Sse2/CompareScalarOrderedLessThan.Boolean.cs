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
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace JIT.HardwareIntrinsics.X86._Sse2
{
    public static partial class Program
    {
        [Fact]
        public static void CompareScalarOrderedLessThanBoolean()
        {
            var test = new BooleanBinaryOpTest__CompareScalarOrderedLessThanBoolean();

            if (test.IsSupported)
            {
                // Validates basic functionality works, using Unsafe.Read
                test.RunBasicScenario_UnsafeRead();

                if (Sse2.IsSupported)
                {
                    // Validates basic functionality works, using Load
                    test.RunBasicScenario_Load();

                    // Validates basic functionality works, using LoadAligned
                    test.RunBasicScenario_LoadAligned();
                }

                // Validates calling via reflection works, using Unsafe.Read
                test.RunReflectionScenario_UnsafeRead();

                if (Sse2.IsSupported)
                {
                    // Validates calling via reflection works, using Load
                    test.RunReflectionScenario_Load();

                    // Validates calling via reflection works, using LoadAligned
                    test.RunReflectionScenario_LoadAligned();
                }

                // Validates passing a static member works
                test.RunClsVarScenario();

                if (Sse2.IsSupported)
                {
                    // Validates passing a static member works, using pinning and Load
                    test.RunClsVarScenario_Load();
                }

                // Validates passing a local works, using Unsafe.Read
                test.RunLclVarScenario_UnsafeRead();

                if (Sse2.IsSupported)
                {
                    // Validates passing a local works, using Load
                    test.RunLclVarScenario_Load();

                    // Validates passing a local works, using LoadAligned
                    test.RunLclVarScenario_LoadAligned();
                }

                // Validates passing the field of a local class works
                test.RunClassLclFldScenario();

                if (Sse2.IsSupported)
                {
                    // Validates passing the field of a local class works, using pinning and Load
                    test.RunClassLclFldScenario_Load();
                }

                // Validates passing an instance member of a class works
                test.RunClassFldScenario();

                if (Sse2.IsSupported)
                {
                    // Validates passing an instance member of a class works, using pinning and Load
                    test.RunClassFldScenario_Load();
                }

                // Validates passing the field of a local struct works
                test.RunStructLclFldScenario();

                if (Sse2.IsSupported)
                {
                    // Validates passing the field of a local struct works, using pinning and Load
                    test.RunStructLclFldScenario_Load();
                }

                // Validates passing an instance member of a struct works
                test.RunStructFldScenario();

                if (Sse2.IsSupported)
                {
                    // Validates passing an instance member of a struct works, using pinning and Load
                    test.RunStructFldScenario_Load();
                }
            }
            else
            {
                // Validates we throw on unsupported hardware
                test.RunUnsupportedScenario();
            }

            if (!test.Succeeded)
            {
                throw new Exception("One or more scenarios did not complete as expected.");
            }
        }
    }

    public sealed unsafe class BooleanBinaryOpTest__CompareScalarOrderedLessThanBoolean
    {
        private struct DataTable
        {
            private byte[] inArray1;
            private byte[] inArray2;

            private GCHandle inHandle1;
            private GCHandle inHandle2;

            private ulong alignment;

            public DataTable(Double[] inArray1, Double[] inArray2, int alignment)
            {
                int sizeOfinArray1 = inArray1.Length * Unsafe.SizeOf<Double>();
                int sizeOfinArray2 = inArray2.Length * Unsafe.SizeOf<Double>();
                if ((alignment != 32 && alignment != 16) || (alignment * 2) < sizeOfinArray1 || (alignment * 2) < sizeOfinArray2)
                {
                    throw new ArgumentException("Invalid value of alignment");
                }

                this.inArray1 = new byte[alignment * 2];
                this.inArray2 = new byte[alignment * 2];

                this.inHandle1 = GCHandle.Alloc(this.inArray1, GCHandleType.Pinned);
                this.inHandle2 = GCHandle.Alloc(this.inArray2, GCHandleType.Pinned);

                this.alignment = (ulong)alignment;

                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray1Ptr), ref Unsafe.As<Double, byte>(ref inArray1[0]), (uint)sizeOfinArray1);
                Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(inArray2Ptr), ref Unsafe.As<Double, byte>(ref inArray2[0]), (uint)sizeOfinArray2);
            }

            public void* inArray1Ptr => Align((byte*)(inHandle1.AddrOfPinnedObject().ToPointer()), alignment);
            public void* inArray2Ptr => Align((byte*)(inHandle2.AddrOfPinnedObject().ToPointer()), alignment);

            public void Dispose()
            {
                inHandle1.Free();
                inHandle2.Free();
            }

            private static unsafe void* Align(byte* buffer, ulong expectedAlignment)
            {
                return (void*)(((ulong)buffer + expectedAlignment - 1) & ~(expectedAlignment - 1));
            }
        }

        private struct TestStruct
        {
            public Vector128<Double> _fld1;
            public Vector128<Double> _fld2;

            public static TestStruct Create()
            {
                var testStruct = new TestStruct();

                for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetDouble(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Double>, byte>(ref testStruct._fld1), ref Unsafe.As<Double, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<Double>>());
                for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetDouble(); }
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Double>, byte>(ref testStruct._fld2), ref Unsafe.As<Double, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<Double>>());

                return testStruct;
            }

            public void RunStructFldScenario(BooleanBinaryOpTest__CompareScalarOrderedLessThanBoolean testClass)
            {
                var result = Sse2.CompareScalarOrderedLessThan(_fld1, _fld2);
                testClass.ValidateResult(_fld1, _fld2, result);
            }

            public void RunStructFldScenario_Load(BooleanBinaryOpTest__CompareScalarOrderedLessThanBoolean testClass)
            {
                fixed (Vector128<Double>* pFld1 = &_fld1)
                fixed (Vector128<Double>* pFld2 = &_fld2)
                {
                    var result = Sse2.CompareScalarOrderedLessThan(
                        Sse2.LoadVector128((Double*)(pFld1)),
                        Sse2.LoadVector128((Double*)(pFld2))
                    );

                    testClass.ValidateResult(_fld1, _fld2, result);
                }
            }
        }

        private static readonly int LargestVectorSize = 16;

        private static readonly int Op1ElementCount = Unsafe.SizeOf<Vector128<Double>>() / sizeof(Double);
        private static readonly int Op2ElementCount = Unsafe.SizeOf<Vector128<Double>>() / sizeof(Double);

        private static Double[] _data1 = new Double[Op1ElementCount];
        private static Double[] _data2 = new Double[Op2ElementCount];

        private static Vector128<Double> _clsVar1;
        private static Vector128<Double> _clsVar2;

        private Vector128<Double> _fld1;
        private Vector128<Double> _fld2;

        private DataTable _dataTable;

        static BooleanBinaryOpTest__CompareScalarOrderedLessThanBoolean()
        {
            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetDouble(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Double>, byte>(ref _clsVar1), ref Unsafe.As<Double, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<Double>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetDouble(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Double>, byte>(ref _clsVar2), ref Unsafe.As<Double, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<Double>>());
        }

        public BooleanBinaryOpTest__CompareScalarOrderedLessThanBoolean()
        {
            Succeeded = true;

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetDouble(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Double>, byte>(ref _fld1), ref Unsafe.As<Double, byte>(ref _data1[0]), (uint)Unsafe.SizeOf<Vector128<Double>>());
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetDouble(); }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector128<Double>, byte>(ref _fld2), ref Unsafe.As<Double, byte>(ref _data2[0]), (uint)Unsafe.SizeOf<Vector128<Double>>());

            for (var i = 0; i < Op1ElementCount; i++) { _data1[i] = TestLibrary.Generator.GetDouble(); }
            for (var i = 0; i < Op2ElementCount; i++) { _data2[i] = TestLibrary.Generator.GetDouble(); }
            _dataTable = new DataTable(_data1, _data2, LargestVectorSize);
        }

        public bool IsSupported => Sse2.IsSupported;

        public bool Succeeded { get; set; }

        public void RunBasicScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_UnsafeRead));

            var result = Sse2.CompareScalarOrderedLessThan(
                Unsafe.Read<Vector128<Double>>(_dataTable.inArray1Ptr),
                Unsafe.Read<Vector128<Double>>(_dataTable.inArray2Ptr)
            );

            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, result);
        }

        public void RunBasicScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_Load));

            var result = Sse2.CompareScalarOrderedLessThan(
                Sse2.LoadVector128((Double*)(_dataTable.inArray1Ptr)),
                Sse2.LoadVector128((Double*)(_dataTable.inArray2Ptr))
            );

            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, result);
        }

        public void RunBasicScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario_LoadAligned));

            var result = Sse2.CompareScalarOrderedLessThan(
                Sse2.LoadAlignedVector128((Double*)(_dataTable.inArray1Ptr)),
                Sse2.LoadAlignedVector128((Double*)(_dataTable.inArray2Ptr))
            );

            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, result);
        }

        public void RunReflectionScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_UnsafeRead));

            var result = typeof(Sse2).GetMethod(nameof(Sse2.CompareScalarOrderedLessThan), new Type[] { typeof(Vector128<Double>), typeof(Vector128<Double>) })
                                     .Invoke(null, new object[] {
                                        Unsafe.Read<Vector128<Double>>(_dataTable.inArray1Ptr),
                                        Unsafe.Read<Vector128<Double>>(_dataTable.inArray2Ptr)
                                     });

            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, (bool)(result));
        }

        public void RunReflectionScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_Load));

            var result = typeof(Sse2).GetMethod(nameof(Sse2.CompareScalarOrderedLessThan), new Type[] { typeof(Vector128<Double>), typeof(Vector128<Double>) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadVector128((Double*)(_dataTable.inArray1Ptr)),
                                        Sse2.LoadVector128((Double*)(_dataTable.inArray2Ptr))
                                     });

            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, (bool)(result));
        }

        public void RunReflectionScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario_LoadAligned));

            var result = typeof(Sse2).GetMethod(nameof(Sse2.CompareScalarOrderedLessThan), new Type[] { typeof(Vector128<Double>), typeof(Vector128<Double>) })
                                     .Invoke(null, new object[] {
                                        Sse2.LoadAlignedVector128((Double*)(_dataTable.inArray1Ptr)),
                                        Sse2.LoadAlignedVector128((Double*)(_dataTable.inArray2Ptr))
                                     });

            ValidateResult(_dataTable.inArray1Ptr, _dataTable.inArray2Ptr, (bool)(result));
        }

        public void RunClsVarScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario));

            var result = Sse2.CompareScalarOrderedLessThan(
                _clsVar1,
                _clsVar2
            );

            ValidateResult(_clsVar1, _clsVar2, result);
        }

        public void RunClsVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClsVarScenario_Load));

            fixed (Vector128<Double>* pClsVar1 = &_clsVar1)
            fixed (Vector128<Double>* pClsVar2 = &_clsVar2)
            {
                var result = Sse2.CompareScalarOrderedLessThan(
                    Sse2.LoadVector128((Double*)(pClsVar1)),
                    Sse2.LoadVector128((Double*)(pClsVar2))
                );

                ValidateResult(_clsVar1, _clsVar2, result);
            }
        }

        public void RunLclVarScenario_UnsafeRead()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_UnsafeRead));

            var op1 = Unsafe.Read<Vector128<Double>>(_dataTable.inArray1Ptr);
            var op2 = Unsafe.Read<Vector128<Double>>(_dataTable.inArray2Ptr);
            var result = Sse2.CompareScalarOrderedLessThan(op1, op2);

            ValidateResult(op1, op2, result);
        }

        public void RunLclVarScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_Load));

            var op1 = Sse2.LoadVector128((Double*)(_dataTable.inArray1Ptr));
            var op2 = Sse2.LoadVector128((Double*)(_dataTable.inArray2Ptr));
            var result = Sse2.CompareScalarOrderedLessThan(op1, op2);

            ValidateResult(op1, op2, result);
        }

        public void RunLclVarScenario_LoadAligned()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunLclVarScenario_LoadAligned));

            var op1 = Sse2.LoadAlignedVector128((Double*)(_dataTable.inArray1Ptr));
            var op2 = Sse2.LoadAlignedVector128((Double*)(_dataTable.inArray2Ptr));
            var result = Sse2.CompareScalarOrderedLessThan(op1, op2);

            ValidateResult(op1, op2, result);
        }

        public void RunClassLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario));

            var test = new BooleanBinaryOpTest__CompareScalarOrderedLessThanBoolean();
            var result = Sse2.CompareScalarOrderedLessThan(test._fld1, test._fld2);

            ValidateResult(test._fld1, test._fld2, result);
        }

        public void RunClassLclFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassLclFldScenario_Load));

            var test = new BooleanBinaryOpTest__CompareScalarOrderedLessThanBoolean();

            fixed (Vector128<Double>* pFld1 = &test._fld1)
            fixed (Vector128<Double>* pFld2 = &test._fld2)
            {
                var result = Sse2.CompareScalarOrderedLessThan(
                    Sse2.LoadVector128((Double*)(pFld1)),
                    Sse2.LoadVector128((Double*)(pFld2))
                );

                ValidateResult(test._fld1, test._fld2, result);
            }
        }

        public void RunClassFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario));

            var result = Sse2.CompareScalarOrderedLessThan(_fld1, _fld2);

            ValidateResult(_fld1, _fld2, result);
        }

        public void RunClassFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunClassFldScenario_Load));

            fixed (Vector128<Double>* pFld1 = &_fld1)
            fixed (Vector128<Double>* pFld2 = &_fld2)
            {
                var result = Sse2.CompareScalarOrderedLessThan(
                    Sse2.LoadVector128((Double*)(pFld1)),
                    Sse2.LoadVector128((Double*)(pFld2))
                );

                ValidateResult(_fld1, _fld2, result);
            }
        }

        public void RunStructLclFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario));

            var test = TestStruct.Create();
            var result = Sse2.CompareScalarOrderedLessThan(test._fld1, test._fld2);
            ValidateResult(test._fld1, test._fld2, result);
        }

        public void RunStructLclFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructLclFldScenario_Load));

            var test = TestStruct.Create();
            var result = Sse2.CompareScalarOrderedLessThan(
                Sse2.LoadVector128((Double*)(&test._fld1)),
                Sse2.LoadVector128((Double*)(&test._fld2))
            );

            ValidateResult(test._fld1, test._fld2, result);
        }

        public void RunStructFldScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructFldScenario));

            var test = TestStruct.Create();
            test.RunStructFldScenario(this);
        }

        public void RunStructFldScenario_Load()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunStructFldScenario_Load));

            var test = TestStruct.Create();
            test.RunStructFldScenario_Load(this);
        }

        public void RunUnsupportedScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunUnsupportedScenario));

            bool succeeded = false;

            try
            {
                RunBasicScenario_UnsafeRead();
            }
            catch (PlatformNotSupportedException)
            {
                succeeded = true;
            }

            if (!succeeded)
            {
                Succeeded = false;
            }
        }

        private void ValidateResult(Vector128<Double> op1, Vector128<Double> op2, bool result, [CallerMemberName] string method = "")
        {
            Double[] inArray1 = new Double[Op1ElementCount];
            Double[] inArray2 = new Double[Op2ElementCount];

            Unsafe.WriteUnaligned(ref Unsafe.As<Double, byte>(ref inArray1[0]), op1);
            Unsafe.WriteUnaligned(ref Unsafe.As<Double, byte>(ref inArray2[0]), op2);

            ValidateResult(inArray1, inArray2, result, method);
        }

        private void ValidateResult(void* op1, void* op2, bool result, [CallerMemberName] string method = "")
        {
            Double[] inArray1 = new Double[Op1ElementCount];
            Double[] inArray2 = new Double[Op2ElementCount];

            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Double, byte>(ref inArray1[0]), ref Unsafe.AsRef<byte>(op1), (uint)Unsafe.SizeOf<Vector128<Double>>());
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Double, byte>(ref inArray2[0]), ref Unsafe.AsRef<byte>(op2), (uint)Unsafe.SizeOf<Vector128<Double>>());

            ValidateResult(inArray1, inArray2, result, method);
        }

        private void ValidateResult(Double[] left, Double[] right, bool result, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            if ((left[0] < right[0]) != result)
            {
                succeeded = false;
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"{nameof(Sse2)}.{nameof(Sse2.CompareScalarOrderedLessThan)}<Boolean>(Vector128<Double>, Vector128<Double>): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"    left: ({string.Join(", ", left)})");
                TestLibrary.TestFramework.LogInformation($"   right: ({string.Join(", ", right)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({result})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
