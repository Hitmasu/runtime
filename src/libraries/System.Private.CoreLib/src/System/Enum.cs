// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// This implementation includes partial support for float/double/nint/nuint-based enums.
// The type loader does not prohibit such enums, and older versions of the ECMA spec include
// them as possible enum types. However there are many things broken throughout the stack for
// float/double/nint/nuint enums. There was a conscious decision made to not fix the whole stack
// to work well for them because the right behavior is often unclear, and it is hard to test and
// very low value because such enums cannot be expressed in C# and are very rarely encountered.

#pragma warning disable 8500 // Allow taking address of managed types

namespace System
{
    /// <summary>Provides the base class for enumerations.</summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public abstract partial class Enum : ValueType, IComparable, ISpanFormattable, IConvertible
    {
        /// <summary>Character used to separate flag enum values when formatted in a list.</summary>
        private const char EnumSeparatorChar = ',';

        /// <summary>Retrieves the name of the constant in the specified enumeration type that has the specified value.</summary>
        /// <typeparam name="TEnum">The type of the enumeration.</typeparam>
        /// <param name="value">The value of a particular enumerated constant in terms of its underlying type.</param>
        /// <returns>
        /// A string containing the name of the enumerated constant in <typeparamref name="TEnum"/> whose value is <paramref name="value"/>,
        /// or <see langword="null"/> if no such constant is found.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe string? GetName<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            RuntimeType rt = (RuntimeType)typeof(TEnum);
            Type underlyingType = typeof(TEnum).GetEnumUnderlyingType();

            if (underlyingType == typeof(int)) return GetNameInlined(GetEnumInfo<int>(rt), *(int*)&value);
            if (underlyingType == typeof(uint)) return GetNameInlined(GetEnumInfo<uint>(rt), *(uint*)&value);
            if (underlyingType == typeof(long)) return GetNameInlined(GetEnumInfo<long>(rt), *(long*)&value);
            if (underlyingType == typeof(ulong)) return GetNameInlined(GetEnumInfo<ulong>(rt), *(ulong*)&value);
            if (underlyingType == typeof(byte)) return GetNameInlined(GetEnumInfo<byte>(rt), *(byte*)&value);
            if (underlyingType == typeof(sbyte)) return GetNameInlined(GetEnumInfo<sbyte>(rt), *(sbyte*)&value);
            if (underlyingType == typeof(short)) return GetNameInlined(GetEnumInfo<short>(rt), *(short*)&value);
            if (underlyingType == typeof(ushort)) return GetNameInlined(GetEnumInfo<ushort>(rt), *(ushort*)&value);
            if (underlyingType == typeof(nint)) return GetNameInlined(GetEnumInfo<nint>(rt), *(nint*)&value);
            if (underlyingType == typeof(nuint)) return GetNameInlined(GetEnumInfo<nuint>(rt), *(nuint*)&value);
            if (underlyingType == typeof(char)) return GetNameInlined(GetEnumInfo<char>(rt), *(char*)&value);
            if (underlyingType == typeof(float)) return GetNameInlined(GetEnumInfo<float>(rt), *(float*)&value);
            if (underlyingType == typeof(double)) return GetNameInlined(GetEnumInfo<double>(rt), *(double*)&value);
            if (underlyingType == typeof(bool)) return GetNameInlined(GetEnumInfo<byte>(rt), *(bool*)&value ? (byte)1 : (byte)0);

            throw CreateUnknownEnumTypeException();
        }

        /// <summary>Retrieves the name of the constant in the specified enumeration type that has the specified value.</summary>
        /// <param name="enumType">An enumeration type.</param>
        /// <param name="value">The value of a particular enumerated constant in terms of its underlying type.</param>
        /// <returns>
        /// A string containing the name of the enumerated constant in <paramref name="enumType"/> whose value is <paramref name="value"/>,
        /// or <see langword="null"/> if no such constant is found.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> or <paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="enumType"/> is not an <see cref="Enum"/>, or <paramref name="value"/> is neither of type <paramref name="enumType"/>
        /// nor does it have the same underlying type as <paramref name="enumType"/>.
        /// </exception>
        public static string? GetName(Type enumType, object value)
        {
            ArgumentNullException.ThrowIfNull(enumType);
            return enumType.GetEnumName(value);
        }

        /// <summary>Retrieves the name of the constant in the specified enumeration type that has the specified value.</summary>
        /// <param name="enumType">An enumeration type.</param>
        /// <param name="uint64Value">The value of a particular enumerated constant in terms of its underlying type, cast to a <see cref="ulong"/>.</param>
        /// <returns>
        /// A string containing the name of the enumerated constant in <paramref name="enumType"/> whose value is <paramref name="uint64Value"/>,
        /// or <see langword="null"/> if no such constant is found.
        /// </returns>
        internal static string? GetName(RuntimeType enumType, ulong uint64Value)
        {
            // For a given underlying type, validate that the specified ulong is in the range
            // of that type.  If it's not, it definitely doesn't match.  If it is, delegate
            // to GetName<TUnderlyingType> to look it up.
            Type underlyingType = enumType.GetEnumUnderlyingType();
            switch (Type.GetTypeCode(underlyingType)) // can't use InternalGetCorElementType as enumType may actually be the underlying type
            {
                case TypeCode.SByte:
                    if ((long)uint64Value < sbyte.MinValue || (long)uint64Value > sbyte.MaxValue) return null;
                    return GetName(GetEnumInfo<sbyte>(enumType), (sbyte)uint64Value);

                case TypeCode.Byte:
                    if (uint64Value > byte.MaxValue) return null;
                    return GetName(GetEnumInfo<byte>(enumType), (byte)uint64Value);

                case TypeCode.Boolean:
                    if (uint64Value > 1) return null;
                    return GetName(GetEnumInfo<byte>(enumType), (byte)uint64Value);

                case TypeCode.Int16:
                    if ((long)uint64Value < short.MinValue || (long)uint64Value > short.MaxValue) return null;
                    return GetName(GetEnumInfo<short>(enumType), (short)uint64Value);

                case TypeCode.UInt16:
                    if (uint64Value > ushort.MaxValue) return null;
                    return GetName(GetEnumInfo<ushort>(enumType), (ushort)uint64Value);

                case TypeCode.Char:
                    if (uint64Value > char.MaxValue) return null;
                    return GetName(GetEnumInfo<char>(enumType), (char)uint64Value);

                case TypeCode.UInt32:
                    if (uint64Value > uint.MaxValue) return null;
                    return GetName(GetEnumInfo<uint>(enumType), (uint)uint64Value);

                case TypeCode.Int32:
                    if ((long)uint64Value < int.MinValue || (long)uint64Value > int.MaxValue) return null;
                    return GetName(GetEnumInfo<int>(enumType), (int)uint64Value);

                case TypeCode.UInt64:
                    return GetName(GetEnumInfo<ulong>(enumType), uint64Value);

                case TypeCode.Int64:
                    return GetName(GetEnumInfo<long>(enumType), (long)uint64Value);
            };

            if (underlyingType == typeof(nint))
            {
                if ((long)uint64Value < nint.MinValue || (long)uint64Value > nint.MaxValue) return null;
                return GetName(GetEnumInfo<nint>(enumType), (nint)uint64Value);
            }

            if (underlyingType == typeof(nuint))
            {
                if (uint64Value > nuint.MaxValue) return null;
                return GetName(GetEnumInfo<nuint>(enumType), (nuint)uint64Value);
            }

            throw CreateUnknownEnumTypeException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string? GetName<TUnderlyingValue>(EnumInfo<TUnderlyingValue> enumInfo, TUnderlyingValue value)
            where TUnderlyingValue : struct, INumber<TUnderlyingValue> =>
            GetNameInlined(enumInfo, value);

        /// <summary>Look up the name for the specified underlying value using the cached <see cref="EnumInfo{TUnderlyingValue}"/> for the associated enum.</summary>
        /// <typeparam name="TUnderlyingValue">The type of the value underlying the associated enum.</typeparam>
        /// <param name="enumInfo">The cached <see cref="EnumInfo{TUnderlyingValue}"/> for the enum type.</param>
        /// <param name="value">The underlying value for which we're searching.</param>
        /// <returns>The name of the value if found; otherwise, <see langword="null"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string? GetNameInlined<TUnderlyingValue>(EnumInfo<TUnderlyingValue> enumInfo, TUnderlyingValue value)
            where TUnderlyingValue : struct, INumber<TUnderlyingValue>
        {
            string[] names = enumInfo.Names;

            // If the values are known to be sequential starting from 0, then we can simply compare the value
            // against the length of the array.  The value matches iff it's in-bounds, and if it is, the value
            // in the array is where the corresponding name is stored.
            if (enumInfo.ValuesAreSequentialFromZero)
            {
                if (Unsafe.SizeOf<TUnderlyingValue>() <= sizeof(int))
                {
                    // Special-case types types that are <= sizeof(int), as we can then eliminate a bounds check on the array.
                    uint uint32Value = uint.CreateTruncating(value);
                    if (uint32Value < (uint)names.Length)
                    {
                        return names[uint32Value];
                    }
                }
                else
                {
                    // Handle the remaining types.
                    if (ulong.CreateTruncating(value) < (ulong)names.Length)
                    {
                        return names[uint.CreateTruncating(value)];
                    }
                }
            }
            else
            {
                // Search for the value in the array of values. If we find a non-negative index,
                // that's the location of the corresponding name in the names array.
                int index = FindDefinedIndex(enumInfo.Values, value);
                if ((uint)index < (uint)names.Length)
                {
                    return names[index];
                }
            }

            // Return null so the caller knows no individual named value could be found.
            return null;
        }

        /// <summary>Retrieves an array of the names of the constants in a specified enumeration type.</summary>
        /// <typeparam name="TEnum">The type of the enumeration.</typeparam>
        /// <returns>A string array of the names of the constants in <typeparamref name="TEnum"/>.</returns>
        public static string[] GetNames<TEnum>() where TEnum : struct, Enum
        {
            string[] names;

            RuntimeType rt = (RuntimeType)typeof(TEnum);
            Type underlyingType = typeof(TEnum).GetEnumUnderlyingType();

            // Get the cached names array.
            if (underlyingType == typeof(int)) names = GetEnumInfo<int>(rt).Names;
            else if (underlyingType == typeof(uint)) names = GetEnumInfo<uint>(rt).Names;
            else if (underlyingType == typeof(long)) names = GetEnumInfo<long>(rt).Names;
            else if (underlyingType == typeof(ulong)) names = GetEnumInfo<ulong>(rt).Names;
            else if (underlyingType == typeof(byte)) names = GetEnumInfo<byte>(rt).Names;
            else if (underlyingType == typeof(sbyte)) names = GetEnumInfo<sbyte>(rt).Names;
            else if (underlyingType == typeof(short)) names = GetEnumInfo<short>(rt).Names;
            else if (underlyingType == typeof(ushort)) names = GetEnumInfo<ushort>(rt).Names;
            else if (underlyingType == typeof(nint)) names = GetEnumInfo<nint>(rt).Names;
            else if (underlyingType == typeof(nuint)) names = GetEnumInfo<nuint>(rt).Names;
            else if (underlyingType == typeof(char)) names = GetEnumInfo<char>(rt).Names;
            else if (underlyingType == typeof(float)) names = GetEnumInfo<float>(rt).Names;
            else if (underlyingType == typeof(double)) names = GetEnumInfo<double>(rt).Names;
            else if (underlyingType == typeof(bool)) names = GetEnumInfo<byte>(rt).Names;
            else throw CreateUnknownEnumTypeException();

            // Return a clone of the array to avoid exposing the cached array instance.
            return new ReadOnlySpan<string>(names).ToArray();
        }

        /// <summary>Retrieves an array of the names of the constants in a specified enumeration.</summary>
        /// <param name="enumType">An enumeration type.</param>
        /// <returns>A string array of the names of the constants in <paramref name="enumType"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not an <see cref="Enum"/>.</exception>
        public static string[] GetNames(Type enumType)
        {
            ArgumentNullException.ThrowIfNull(enumType);
            return enumType.GetEnumNames();
        }

        /// <summary>Gets the cached names array for the specified enum type, without making a copy.</summary>
        /// <remarks>The returned array should not be exposed outside of this assembly.</remarks>
        internal static string[] GetNamesNoCopy(RuntimeType enumType)
        {
            Debug.Assert(enumType.IsActualEnum);

            return InternalGetCorElementType(enumType) switch
            {
                CorElementType.ELEMENT_TYPE_I1 => GetEnumInfo<sbyte>(enumType).Names,
                CorElementType.ELEMENT_TYPE_U1 => GetEnumInfo<byte>(enumType).Names,
                CorElementType.ELEMENT_TYPE_I2 => GetEnumInfo<short>(enumType).Names,
                CorElementType.ELEMENT_TYPE_U2 => GetEnumInfo<ushort>(enumType).Names,
                CorElementType.ELEMENT_TYPE_I4 => GetEnumInfo<int>(enumType).Names,
                CorElementType.ELEMENT_TYPE_U4 => GetEnumInfo<uint>(enumType).Names,
                CorElementType.ELEMENT_TYPE_I8 => GetEnumInfo<long>(enumType).Names,
                CorElementType.ELEMENT_TYPE_U8 => GetEnumInfo<ulong>(enumType).Names,
                CorElementType.ELEMENT_TYPE_R4 => GetEnumInfo<float>(enumType).Names,
                CorElementType.ELEMENT_TYPE_R8 => GetEnumInfo<double>(enumType).Names,
                CorElementType.ELEMENT_TYPE_I => GetEnumInfo<nint>(enumType).Names,
                CorElementType.ELEMENT_TYPE_U => GetEnumInfo<nuint>(enumType).Names,
                CorElementType.ELEMENT_TYPE_CHAR => GetEnumInfo<char>(enumType).Names,
                CorElementType.ELEMENT_TYPE_BOOLEAN => GetEnumInfo<byte>(enumType).Names,
                _ => throw CreateUnknownEnumTypeException(),
            };
        }

        /// <summary>Returns the underlying type of the specified enumeration.</summary>
        /// <param name="enumType">The enumeration whose underlying type will be retrieved.</param>
        /// <returns>The underlying type of <paramref name="enumType"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not an <see cref="Enum"/>.</exception>
        public static Type GetUnderlyingType(Type enumType)
        {
            ArgumentNullException.ThrowIfNull(enumType);
            return enumType.GetEnumUnderlyingType();
        }

        /// <summary>Retrieves an array of the values of the constants in a specified enumeration type.</summary>
        /// <typeparam name="TEnum">The type of the enumeration.</typeparam>
        /// <returns>An array that contains the values of the constants in <typeparamref name="TEnum"/>.</returns>
        public static TEnum[] GetValues<TEnum>() where TEnum : struct, Enum
        {
            Array values = GetValuesAsUnderlyingTypeNoCopy((RuntimeType)typeof(TEnum));
            var result = new TEnum[values.Length];
            Array.Copy(values, result, values.Length);
            return result;
        }

        /// <summary>Retrieves an array of the values of the constants in a specified enumeration.</summary>
        /// <param name="enumType">An enumeration type.</param>
        /// <returns>An array that contains the values of the constants in <paramref name="enumType"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not an <see cref="Enum"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// The method is invoked by reflection in a reflection-only context, or <paramref name="enumType"/> is a type from an assembly loaded in a reflection-only context.
        /// </exception>
        [RequiresDynamicCode("It might not be possible to create an array of the enum type at runtime. Use the GetValues<TEnum> overload or the GetValuesAsUnderlyingType method instead.")]
        public static Array GetValues(Type enumType)
        {
            ArgumentNullException.ThrowIfNull(enumType);
            return enumType.GetEnumValues();
        }

        /// <summary>Retrieves an array of the values of the underlying type constants in a specified enumeration type.</summary>
        /// <typeparam name="TEnum">An enumeration type.</typeparam>
        /// <remarks>
        /// You can use this method to get enumeration values when it's hard to create an array of the enumeration type.
        /// For example, you might use this method for the <see cref="T:System.Reflection.MetadataLoadContext" /> enumeration or on a platform where run-time code generation is not available.
        /// </remarks>
        /// <returns>An array that contains the values of the underlying type constants in <typeparamref name="TEnum" />.</returns>
        public static Array GetValuesAsUnderlyingType<TEnum>() where TEnum : struct, Enum =>
            typeof(TEnum).GetEnumValuesAsUnderlyingType();

        /// <summary>Retrieves an array of the values of the underlying type constants in a specified enumeration.</summary>
        /// <param name="enumType">An enumeration type.</param>
        /// <remarks>
        /// You can use this method to get enumeration values when it's hard to create an array of the enumeration type.
        /// For example, you might use this method for the MetadataLoadContext enumeration or on a platform where run-time code generation is not available.
        /// </remarks>
        /// <returns>An array that contains the values of the underlying type constants in  <paramref name="enumType" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType" /> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType" /> is not an enumeration type.</exception>
        public static Array GetValuesAsUnderlyingType(Type enumType)
        {
            ArgumentNullException.ThrowIfNull(enumType);
            return enumType.GetEnumValuesAsUnderlyingType();
        }

        /// <summary>Retrieves an array of the values of the underlying type constants in a specified enumeration type.</summary>
        internal static Array GetValuesAsUnderlyingType(RuntimeType enumType)
        {
            Debug.Assert(enumType.IsActualEnum);

            return InternalGetCorElementType(enumType) switch
            {
                CorElementType.ELEMENT_TYPE_I1 => GetEnumInfo<sbyte>(enumType, getNames: false).CloneValues(),
                CorElementType.ELEMENT_TYPE_U1 => GetEnumInfo<byte>(enumType, getNames: false).CloneValues(),
                CorElementType.ELEMENT_TYPE_I2 => GetEnumInfo<short>(enumType, getNames: false).CloneValues(),
                CorElementType.ELEMENT_TYPE_U2 => GetEnumInfo<ushort>(enumType, getNames: false).CloneValues(),
                CorElementType.ELEMENT_TYPE_I4 => GetEnumInfo<int>(enumType, getNames: false).CloneValues(),
                CorElementType.ELEMENT_TYPE_U4 => GetEnumInfo<uint>(enumType, getNames: false).CloneValues(),
                CorElementType.ELEMENT_TYPE_I8 => GetEnumInfo<long>(enumType, getNames: false).CloneValues(),
                CorElementType.ELEMENT_TYPE_U8 => GetEnumInfo<ulong>(enumType, getNames: false).CloneValues(),
                CorElementType.ELEMENT_TYPE_R4 => GetEnumInfo<float>(enumType, getNames: false).CloneValues(),
                CorElementType.ELEMENT_TYPE_R8 => GetEnumInfo<double>(enumType, getNames: false).CloneValues(),
                CorElementType.ELEMENT_TYPE_I => GetEnumInfo<nint>(enumType, getNames: false).CloneValues(),
                CorElementType.ELEMENT_TYPE_U => GetEnumInfo<nuint>(enumType, getNames: false).CloneValues(),
                CorElementType.ELEMENT_TYPE_CHAR => GetEnumInfo<char>(enumType, getNames: false).CloneValues(),
                CorElementType.ELEMENT_TYPE_BOOLEAN => CopyByteArrayToNewBoolArray(GetEnumInfo<byte>(enumType, getNames: false).Values),
                _ => throw CreateUnknownEnumTypeException(),
            };
        }

        /// <summary>Retrieves the cached array of the values of the underlying type constants in a specified enumeration.</summary>
        /// <remarks>The returned array should not be exposed outside of this assembly.</remarks>
        internal static Array GetValuesAsUnderlyingTypeNoCopy(RuntimeType enumType)
        {
            Debug.Assert(enumType.IsActualEnum);

            return InternalGetCorElementType(enumType) switch
            {
                CorElementType.ELEMENT_TYPE_I1 => GetEnumInfo<sbyte>(enumType, getNames: false).Values,
                CorElementType.ELEMENT_TYPE_U1 => GetEnumInfo<byte>(enumType, getNames: false).Values,
                CorElementType.ELEMENT_TYPE_I2 => GetEnumInfo<short>(enumType, getNames: false).Values,
                CorElementType.ELEMENT_TYPE_U2 => GetEnumInfo<ushort>(enumType, getNames: false).Values,
                CorElementType.ELEMENT_TYPE_I4 => GetEnumInfo<int>(enumType, getNames: false).Values,
                CorElementType.ELEMENT_TYPE_U4 => GetEnumInfo<uint>(enumType, getNames: false).Values,
                CorElementType.ELEMENT_TYPE_I8 => GetEnumInfo<long>(enumType, getNames: false).Values,
                CorElementType.ELEMENT_TYPE_U8 => GetEnumInfo<ulong>(enumType, getNames: false).Values,
                CorElementType.ELEMENT_TYPE_R4 => GetEnumInfo<float>(enumType, getNames: false).Values,
                CorElementType.ELEMENT_TYPE_R8 => GetEnumInfo<double>(enumType, getNames: false).Values,
                CorElementType.ELEMENT_TYPE_I => GetEnumInfo<nint>(enumType, getNames: false).Values,
                CorElementType.ELEMENT_TYPE_U => GetEnumInfo<nuint>(enumType, getNames: false).Values,
                CorElementType.ELEMENT_TYPE_CHAR => GetEnumInfo<char>(enumType, getNames: false).Values,
                CorElementType.ELEMENT_TYPE_BOOLEAN => CopyByteArrayToNewBoolArray(GetEnumInfo<byte>(enumType, getNames: false).Values), // this is the only case that clones, out of necessity
                _ => throw CreateUnknownEnumTypeException(),
            };
        }

        /// <summary>Determines whether one or more bit fields are set in the current instance.</summary>
        /// <param name="flag">An enumeration value.</param>
        /// <returns><see langword="true"/> if the bit field or bit fields that are set in flag are also set in the current instance; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="flag"/> is a different type than the current instance.</exception>
        [Intrinsic]
        public bool HasFlag(Enum flag)
        {
            ArgumentNullException.ThrowIfNull(flag);

            if (GetType() != flag.GetType() && !GetType().IsEquivalentTo(flag.GetType()))
                throw new ArgumentException(SR.Format(SR.Argument_EnumTypeDoesNotMatch, flag.GetType(), GetType()));

            ref byte pThisValue = ref this.GetRawData();
            ref byte pFlagsValue = ref flag.GetRawData();

            switch (InternalGetCorElementType())
            {
                case CorElementType.ELEMENT_TYPE_I1:
                case CorElementType.ELEMENT_TYPE_U1:
                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                    {
                        byte flagsValue = pFlagsValue;
                        return (pThisValue & flagsValue) == flagsValue;
                    }

                case CorElementType.ELEMENT_TYPE_I2:
                case CorElementType.ELEMENT_TYPE_U2:
                case CorElementType.ELEMENT_TYPE_CHAR:
                    {
                        ushort flagsValue = Unsafe.As<byte, ushort>(ref pFlagsValue);
                        return (Unsafe.As<byte, ushort>(ref pThisValue) & flagsValue) == flagsValue;
                    }

                case CorElementType.ELEMENT_TYPE_I4:
                case CorElementType.ELEMENT_TYPE_U4:
#if TARGET_32BIT
                case CorElementType.ELEMENT_TYPE_I:
                case CorElementType.ELEMENT_TYPE_U:
#endif
                case CorElementType.ELEMENT_TYPE_R4:
                    {
                        uint flagsValue = Unsafe.As<byte, uint>(ref pFlagsValue);
                        return (Unsafe.As<byte, uint>(ref pThisValue) & flagsValue) == flagsValue;
                    }

                case CorElementType.ELEMENT_TYPE_I8:
                case CorElementType.ELEMENT_TYPE_U8:
#if TARGET_64BIT
                case CorElementType.ELEMENT_TYPE_I:
                case CorElementType.ELEMENT_TYPE_U:
#endif
                case CorElementType.ELEMENT_TYPE_R8:
                    {
                        ulong flagsValue = Unsafe.As<byte, ulong>(ref pFlagsValue);
                        return (Unsafe.As<byte, ulong>(ref pThisValue) & flagsValue) == flagsValue;
                    }

                default:
                    Debug.Fail("Unknown enum underlying type");
                    return false;
            }
        }

        /// <summary>Marshals a byte[] to a bool[].</summary>
        /// <param name="bytes">The input byte array.</param>
        /// <returns>A new bool[] containing true whereever a byte was non-zero and false wherever a byte was zero.</returns>
        /// <remarks>
        /// Since bool isn't a numeric type, we use an EnumInfo{byte} for bool-based enums. This method is used to translate
        /// that EnumInfo's Values back to bools.
        /// </remarks>
        private static bool[] CopyByteArrayToNewBoolArray(byte[] bytes)
        {
            bool[] result = new bool[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                result[i] = bytes[i] != 0;
            }
            return result;
        }

        /// <summary>Returns a <see cref="bool"/> telling whether a given integral value, or its name as a string, exists in a specified enumeration.</summary>
        /// <typeparam name="TEnum">The type of the enumeration.</typeparam>
        /// <param name="value">The value or name of a constant in <typeparamref name="TEnum"/>.</param>
        /// <returns><see langword="true"/> if a given integral value exists in a specified enumeration; <see langword="false"/>, otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsDefined<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            RuntimeType rt = (RuntimeType)typeof(TEnum);
            Type underlyingType = typeof(TEnum).GetEnumUnderlyingType();

            if (underlyingType == typeof(int)) return IsDefinedPrimitive(rt, *(int*)&value);
            if (underlyingType == typeof(uint)) return IsDefinedPrimitive(rt, *(uint*)&value);
            if (underlyingType == typeof(long)) return IsDefinedPrimitive(rt, *(long*)&value);
            if (underlyingType == typeof(ulong)) return IsDefinedPrimitive(rt, *(ulong*)&value);
            if (underlyingType == typeof(byte)) return IsDefinedPrimitive(rt, *(byte*)&value);
            if (underlyingType == typeof(sbyte)) return IsDefinedPrimitive(rt, *(sbyte*)&value);
            if (underlyingType == typeof(short)) return IsDefinedPrimitive(rt, *(short*)&value);
            if (underlyingType == typeof(ushort)) return IsDefinedPrimitive(rt, *(ushort*)&value);
            if (underlyingType == typeof(nint)) return IsDefinedPrimitive(rt, *(nint*)&value);
            if (underlyingType == typeof(nuint)) return IsDefinedPrimitive(rt, *(nuint*)&value);
            if (underlyingType == typeof(char)) return IsDefinedPrimitive(rt, *(char*)&value);
            if (underlyingType == typeof(float)) return IsDefinedPrimitive(rt, *(float*)&value);
            if (underlyingType == typeof(double)) return IsDefinedPrimitive(rt, *(double*)&value);
            if (underlyingType == typeof(bool)) return IsDefinedPrimitive(rt, *(bool*)&value ? (byte)1 : (byte)0);

            throw CreateUnknownEnumTypeException();
        }

        /// <summary>Gets whether the specified individual value is defined in the specified enum.</summary>
        internal static bool IsDefinedPrimitive<TUnderlyingValue>(RuntimeType enumType, TUnderlyingValue value)
            where TUnderlyingValue : struct, INumber<TUnderlyingValue>
        {
            EnumInfo<TUnderlyingValue> enumInfo = GetEnumInfo<TUnderlyingValue>(enumType, getNames: false);
            TUnderlyingValue[] values = enumInfo.Values;

            // If the enum's values are all sequentially numbered starting from 0, then we can
            // just return if the requested index is in range.
            if (enumInfo.ValuesAreSequentialFromZero)
            {
                return ulong.CreateTruncating(value) < (ulong)values.Length;
            }

            // Otherwise, search for the value.
            return FindDefinedIndex(values, value) >= 0;
        }

        /// <summary>Returns a <see cref="bool"/> telling whether a given integral value, or its name as a string, exists in a specified enumeration.</summary>
        /// <param name="enumType">An enumeration type.</param>
        /// <param name="value">The value or name of a constant in <paramref name="enumType"/>.</param>
        /// <returns><see langword="true"/> if a constant in <paramref name="enumType"/> has a value equal to value; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> or <paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="enumType"/> is not an <see cref="Enum"/>,
        /// or the type of <paramref name="value"/> is an enumeration but it is not an enumeration of type <paramref name="enumType"/>,
        /// or the type of <paramref name="value"/> is not an underlying type of <paramref name="enumType"/>.
        /// </exception>
        public static bool IsDefined(Type enumType, object value)
        {
            ArgumentNullException.ThrowIfNull(enumType);
            return enumType.IsEnumDefined(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FindDefinedIndex<TUnderlyingValue>(TUnderlyingValue[] values, TUnderlyingValue value)
            where TUnderlyingValue : struct, IEquatable<TUnderlyingValue>, IComparable<TUnderlyingValue>
        {
            // Binary searching has a higher constant overhead than linear searching.
            // For smaller enums, use IndexOf.
            // For larger enums, use BinarySearch.
            const int NumberOfValuesThreshold = 32; // This threshold can be tweaked over time as optimizations evolve.
            ReadOnlySpan<TUnderlyingValue> span = values;
            return values.Length <= NumberOfValuesThreshold ?
                span.IndexOf(value) :
                SpanHelpers.BinarySearch(span, value);
        }

        /// <summary>Converts the string representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.</summary>
        /// <param name="enumType">An enumeration type.</param>
        /// <param name="value">A string containing the name or value to convert.</param>
        /// <returns>An <see cref="object"/> of type <paramref name="enumType"/> whose value is represented by <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not an <see cref="Enum"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is either an empty string or only contains white space.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is a name, but not one of the named constants defined for the enumeration.</exception>
        /// <exception cref="OverflowException"><paramref name="value"/> is outside the range of the underlying type of <paramref name="enumType"/></exception>
        public static object Parse(Type enumType, string value) =>
            Parse(enumType, value, ignoreCase: false);

        /// <summary>Converts the span of chars representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.</summary>
        /// <param name="enumType">An enumeration type.</param>
        /// <param name="value">A span containing the name or value to convert.</param>
        /// <returns>An <see cref="object"/> of type <paramref name="enumType"/> whose value is represented by <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not an <see cref="Enum"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is either an empty string or only contains white space.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is a name, but not one of the named constants defined for the enumeration.</exception>
        /// <exception cref="OverflowException"><paramref name="value"/> is outside the range of the underlying type of <paramref name="enumType"/></exception>
        public static object Parse(Type enumType, ReadOnlySpan<char> value) =>
            Parse(enumType, value, ignoreCase: false);

        /// <summary>
        /// Converts the string representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.
        /// A parameter specifies whether the operation is case-insensitive.
        /// </summary>
        /// <param name="enumType">An enumeration type.</param>
        /// <param name="value">A string containing the name or value to convert.</param>
        /// <param name="ignoreCase"><see langword="true"/> to ignore case; <see langword="false"/> to regard case.</param>
        /// <returns>An <see cref="object"/> of type <paramref name="enumType"/> whose value is represented by <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not an <see cref="Enum"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is either an empty string or only contains white space.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is a name, but not one of the named constants defined for the enumeration.</exception>
        /// <exception cref="OverflowException"><paramref name="value"/> is outside the range of the underlying type of <paramref name="enumType"/></exception>
        public static object Parse(Type enumType, string value, bool ignoreCase)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            bool success = TryParse(enumType, value.AsSpan(), ignoreCase, throwOnFailure: true, out object? result);
            Debug.Assert(success && result is not null);
            return result;
        }

        /// <summary>
        /// Converts the span of chars representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.
        /// A parameter specifies whether the operation is case-insensitive.
        /// </summary>
        /// <param name="enumType">An enumeration type.</param>
        /// <param name="value">A span containing the name or value to convert.</param>
        /// <param name="ignoreCase"><see langword="true"/> to ignore case; <see langword="false"/> to regard case.</param>
        /// <returns>An <see cref="object"/> of type <paramref name="enumType"/> whose value is represented by <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not an <see cref="Enum"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is either an empty string or only contains white space.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is a name, but not one of the named constants defined for the enumeration.</exception>
        /// <exception cref="OverflowException"><paramref name="value"/> is outside the range of the underlying type of <paramref name="enumType"/></exception>
        public static object Parse(Type enumType, ReadOnlySpan<char> value, bool ignoreCase)
        {
            bool success = TryParse(enumType, value, ignoreCase, throwOnFailure: true, out object? result);
            Debug.Assert(success && result is not null);
            return result;
        }

        /// <summary>Converts the string representation of the name or numeric value of one or more enumerated constants specified by <typeparamref name="TEnum"/> to an equivalent enumerated object.</summary>
        /// <typeparam name="TEnum">An enumeration type.</typeparam>
        /// <param name="value">A string containing the name or value to convert.</param>
        /// <returns><typeparamref name="TEnum"/> An object of type <typeparamref name="TEnum"/> whose value is represented by <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><typeparamref name="TEnum"/> is not an <see cref="Enum"/> type</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> does not contain enumeration information</exception>
        public static TEnum Parse<TEnum>(string value) where TEnum : struct =>
            Parse<TEnum>(value, ignoreCase: false);

        /// <summary>Converts the span of chars representation of the name or numeric value of one or more enumerated constants specified by <typeparamref name="TEnum"/> to an equivalent enumerated object.</summary>
        /// <typeparam name="TEnum">An enumeration type.</typeparam>
        /// <param name="value">A span containing the name or value to convert.</param>
        /// <returns><typeparamref name="TEnum"/> An object of type <typeparamref name="TEnum"/> whose value is represented by <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentException"><typeparamref name="TEnum"/> is not an <see cref="Enum"/> type</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> does not contain enumeration information</exception>
        public static TEnum Parse<TEnum>(ReadOnlySpan<char> value) where TEnum : struct =>
           Parse<TEnum>(value, ignoreCase: false);

        /// <summary>
        /// Converts the string representation of the name or numeric value of one or more enumerated constants specified by <typeparamref name="TEnum"/> to an equivalent enumerated object.
        /// A parameter specifies whether the operation is case-insensitive.
        /// </summary>
        /// <typeparam name="TEnum">An enumeration type.</typeparam>
        /// <param name="value">A string containing the name or value to convert.</param>
        /// <param name="ignoreCase"><see langword="true"/> to ignore case; <see langword="false"/> to regard case.</param>
        /// <returns><typeparamref name="TEnum"/> An object of type <typeparamref name="TEnum"/> whose value is represented by <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><typeparamref name="TEnum"/> is not an <see cref="Enum"/> type</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> does not contain enumeration information</exception>
        public static TEnum Parse<TEnum>(string value, bool ignoreCase) where TEnum : struct
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            bool success = TryParse(value.AsSpan(), ignoreCase, throwOnFailure: true, out TEnum result);
            Debug.Assert(success);
            return result;
        }

        /// <summary>
        /// Converts the span of chars representation of the name or numeric value of one or more enumerated constants specified by <typeparamref name="TEnum"/> to an equivalent enumerated object.
        /// A parameter specifies whether the operation is case-insensitive.
        /// </summary>
        /// <typeparam name="TEnum">An enumeration type.</typeparam>
        /// <param name="value">A span containing the name or value to convert.</param>
        /// <param name="ignoreCase"><see langword="true"/> to ignore case; <see langword="false"/> to regard case.</param>
        /// <returns><typeparamref name="TEnum"/> An object of type <typeparamref name="TEnum"/> whose value is represented by <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentException"><typeparamref name="TEnum"/> is not an <see cref="Enum"/> type</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> does not contain enumeration information</exception>
        public static TEnum Parse<TEnum>(ReadOnlySpan<char> value, bool ignoreCase) where TEnum : struct
        {
            bool success = TryParse(value, ignoreCase, throwOnFailure: true, out TEnum result);
            Debug.Assert(success);
            return result;
        }

        /// <summary>Converts the string representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.</summary>
        /// <param name="enumType">The enum type to use for parsing.</param>
        /// <param name="value">The string representation of the name or numeric value of one or more enumerated constants.</param>
        /// <param name="result">When this method returns <see langword="true"/>, an object containing an enumeration constant representing the parsed value.</param>
        /// <returns><see langword="true"/> if the conversion succeeded; <see langword="false"/> otherwise.</returns>
        public static bool TryParse(Type enumType, string? value, [NotNullWhen(true)] out object? result) =>
            TryParse(enumType, value, ignoreCase: false, out result);

        /// <summary>Converts the span of chars representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.</summary>
        /// <param name="enumType">The enum type to use for parsing.</param>
        /// <param name="value">The span representation of the name or numeric value of one or more enumerated constants.</param>
        /// <param name="result">When this method returns <see langword="true"/>, an object containing an enumeration constant representing the parsed value.</param>
        /// <returns><see langword="true"/> if the conversion succeeded; <see langword="false"/> otherwise.</returns>
        public static bool TryParse(Type enumType, ReadOnlySpan<char> value, [NotNullWhen(true)] out object? result) =>
          TryParse(enumType, value, ignoreCase: false, out result);

        /// <summary>
        /// Converts the string representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.
        /// A parameter specifies whether the operation is case-insensitive.
        /// </summary>
        /// <param name="enumType">The enum type to use for parsing.</param>
        /// <param name="value">The string representation of the name or numeric value of one or more enumerated constants.</param>
        /// <param name="ignoreCase"><see langword="true"/> to read <paramref name="enumType"/> in case insensitive mode; <see langword="false"/> to read <paramref name="enumType"/> in case sensitive mode.</param>
        /// <param name="result">When this method returns <see langword="true"/>, an object containing an enumeration constant representing the parsed value.</param>
        /// <returns><see langword="true"/> if the conversion succeeded; <see langword="false"/> otherwise.</returns>
        public static bool TryParse(Type enumType, string? value, bool ignoreCase, [NotNullWhen(true)] out object? result)
        {
            if (value is not null)
            {
                return TryParse(enumType, value.AsSpan(), ignoreCase, throwOnFailure: false, out result);
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Converts the span of chars representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.
        /// A parameter specifies whether the operation is case-insensitive.
        /// </summary>
        /// <param name="enumType">The enum type to use for parsing.</param>
        /// <param name="value">The span representation of the name or numeric value of one or more enumerated constants.</param>
        /// <param name="ignoreCase"><see langword="true"/> to read <paramref name="enumType"/> in case insensitive mode; <see langword="false"/> to read <paramref name="enumType"/> in case sensitive mode.</param>
        /// <param name="result">When this method returns <see langword="true"/>, an object containing an enumeration constant representing the parsed value.</param>
        /// <returns><see langword="true"/> if the conversion succeeded; <see langword="false"/> otherwise.</returns>
        public static bool TryParse(Type enumType, ReadOnlySpan<char> value, bool ignoreCase, [NotNullWhen(true)] out object? result) =>
            TryParse(enumType, value, ignoreCase, throwOnFailure: false, out result);

        /// <summary>Core implementation for all non-generic {Try}Parse methods.</summary>
        private static unsafe bool TryParse(Type enumType, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, [NotNullWhen(true)] out object? result)
        {
            bool parsed = false;
            long longScratch = 0;

            // Validation on the enum type itself.  Failures here are considered non-parsing failures
            // and thus always throw rather than returning false.
            RuntimeType rt = ValidateRuntimeType(enumType);

            switch (InternalGetCorElementType(rt))
            {
                case CorElementType.ELEMENT_TYPE_I1:
                    parsed = TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out *(sbyte*)&longScratch);
                    longScratch = *(sbyte*)&longScratch;
                    break;

                case CorElementType.ELEMENT_TYPE_U1:
                    parsed = TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out *(byte*)&longScratch);
                    longScratch = *(byte*)&longScratch;
                    break;

                case CorElementType.ELEMENT_TYPE_I2:
                    parsed = TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out *(short*)&longScratch);
                    longScratch = *(short*)&longScratch;
                    break;

                case CorElementType.ELEMENT_TYPE_U2:
                    parsed = TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out *(ushort*)&longScratch);
                    longScratch = *(ushort*)&longScratch;
                    break;

                case CorElementType.ELEMENT_TYPE_I4:
                    parsed = TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out *(int*)&longScratch);
                    longScratch = *(int*)&longScratch;
                    break;

                case CorElementType.ELEMENT_TYPE_U4:
                    parsed = TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out *(uint*)&longScratch);
                    longScratch = *(uint*)&longScratch;
                    break;

                case CorElementType.ELEMENT_TYPE_I8:
                    parsed = TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out longScratch);
                    break;

                case CorElementType.ELEMENT_TYPE_U8:
                    parsed = TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out *(ulong*)&longScratch);
                    break;

                default:
                    parsed = TryParseRareTypes(rt, value, ignoreCase, throwOnFailure, out longScratch);
                    break;
            }

            result = parsed ? InternalBoxEnum(rt, longScratch) : null;
            return parsed;

            [MethodImpl(MethodImplOptions.NoInlining)]
            static bool TryParseRareTypes(RuntimeType rt, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, [NotNullWhen(true)] out long result)
            {
                bool parsed;

                switch (InternalGetCorElementType(rt))
                {
                    case CorElementType.ELEMENT_TYPE_R4:
                        {
                            parsed = TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out float localResult);
                            result = BitConverter.SingleToInt32Bits(localResult);
                        }
                        break;

                    case CorElementType.ELEMENT_TYPE_R8:
                        {
                            parsed = TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out double localResult);
                            result = BitConverter.DoubleToInt64Bits(localResult);
                        }
                        break;

                    case CorElementType.ELEMENT_TYPE_I:
                        {
                            parsed = TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out nint localResult);
                            result = localResult;
                        }
                        break;

                    case CorElementType.ELEMENT_TYPE_U:
                        {
                            parsed = TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out nuint localResult);
                            result = (long)localResult;
                        }
                        break;

                    case CorElementType.ELEMENT_TYPE_CHAR:
                        {
                            parsed = TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out char localResult);
                            result = localResult;
                        }
                        break;

                    case CorElementType.ELEMENT_TYPE_BOOLEAN:
                        {
                            parsed = TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out byte localResult);
                            result = localResult;
                        }
                        break;

                    default:
                        throw CreateUnknownEnumTypeException();
                }

                return parsed;
            }
        }

        /// <summary>Converts the string representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.</summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="value">The string representation of the name or numeric value of one or more enumerated constants.</param>
        /// <param name="result">When this method returns <see langword="true"/>, an object containing an enumeration constant representing the parsed value.</param>
        /// <returns><see langword="true"/> if the conversion succeeded; <see langword="false"/> otherwise.</returns>
        /// <exception cref="ArgumentException"><typeparamref name="TEnum"/> is not an enumeration type</exception>
        public static bool TryParse<TEnum>([NotNullWhen(true)] string? value, out TEnum result) where TEnum : struct =>
            TryParse(value, ignoreCase: false, out result);

        /// <summary>Converts the span of chars representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.</summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="value">The span of chars representation of the name or numeric value of one or more enumerated constants.</param>
        /// <param name="result">When this method returns <see langword="true"/>, an object containing an enumeration constant representing the parsed value.</param>
        /// <returns><see langword="true"/> if the conversion succeeded; <see langword="false"/> otherwise.</returns>
        /// <exception cref="ArgumentException"><typeparamref name="TEnum"/> is not an enumeration type</exception>
        public static bool TryParse<TEnum>(ReadOnlySpan<char> value, out TEnum result) where TEnum : struct =>
            TryParse(value, ignoreCase: false, throwOnFailure: false, out result);

        /// <summary>
        /// Converts the string representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.
        /// A parameter specifies whether the operation is case-sensitive.
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="value">The string representation of the name or numeric value of one or more enumerated constants.</param>
        /// <param name="ignoreCase"><see langword="true"/> to ignore case; <see langword="false"/> to consider case.</param>
        /// <param name="result">When this method returns <see langword="true"/>, an object containing an enumeration constant representing the parsed value.</param>
        /// <returns><see langword="true"/> if the conversion succeeded; <see langword="false"/> otherwise.</returns>
        /// <exception cref="ArgumentException"><typeparamref name="TEnum"/> is not an enumeration type</exception>
        public static bool TryParse<TEnum>([NotNullWhen(true)] string? value, bool ignoreCase, out TEnum result) where TEnum : struct
        {
            if (value is not null)
            {
                return TryParse(value.AsSpan(), ignoreCase, throwOnFailure: false, out result);
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Converts the span of chars representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.
        /// A parameter specifies whether the operation is case-sensitive.
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="value">The span representation of the name or numeric value of one or more enumerated constants.</param>
        /// <param name="ignoreCase"><see langword="true"/> to ignore case; <see langword="false"/> to consider case.</param>
        /// <param name="result">When this method returns <see langword="true"/>, an object containing an enumeration constant representing the parsed value.</param>
        /// <returns><see langword="true"/> if the conversion succeeded; <see langword="false"/> otherwise.</returns>
        /// <exception cref="ArgumentException"><typeparamref name="TEnum"/> is not an enumeration type</exception>
        public static bool TryParse<TEnum>(ReadOnlySpan<char> value, bool ignoreCase, out TEnum result) where TEnum : struct =>
            TryParse(value, ignoreCase, throwOnFailure: false, out result);

        /// <summary>Core implementation for all generic {Try}Parse methods.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // compiles to a single call
        private static bool TryParse<TEnum>(ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, out TEnum result) where TEnum : struct
        {
            // Validation on the enum type itself.  Failures here are considered non-parsing failures
            // and thus always throw rather than returning false.
            if (!typeof(TEnum).IsEnum) // with IsEnum being an intrinsic, this whole block will be eliminated for all meaningful cases
            {
                throw new ArgumentException(SR.Arg_MustBeEnum, nameof(TEnum));
            }

            Unsafe.SkipInit(out result);
            RuntimeType rt = (RuntimeType)typeof(TEnum);
            Type underlyingType = typeof(TEnum).GetEnumUnderlyingType();

            if (underlyingType == typeof(int)) return TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, int>(ref result));
            if (underlyingType == typeof(uint)) return TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, uint>(ref result));
            if (underlyingType == typeof(long)) return TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, long>(ref result));
            if (underlyingType == typeof(ulong)) return TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, ulong>(ref result));
            if (underlyingType == typeof(byte)) return TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, byte>(ref result));
            if (underlyingType == typeof(sbyte)) return TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, sbyte>(ref result));
            if (underlyingType == typeof(short)) return TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, short>(ref result));
            if (underlyingType == typeof(ushort)) return TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, ushort>(ref result));
            if (underlyingType == typeof(float)) return TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, float>(ref result));
            if (underlyingType == typeof(double)) return TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, double>(ref result));
            if (underlyingType == typeof(char)) return TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, char>(ref result));
            if (underlyingType == typeof(nint)) return TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, nint>(ref result));
            if (underlyingType == typeof(nuint)) return TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, nuint>(ref result));
            if (underlyingType == typeof(bool)) return TryParseByValueOrName(rt, value, ignoreCase, throwOnFailure, out Unsafe.As<TEnum, byte>(ref result));

            if (throwOnFailure)
            {
                throw CreateUnknownEnumTypeException();
            }
            result = default;
            return false;
        }

        /// <summary>Core implementation for all {Try}Parse methods, both generic and non-generic, parsing either by value or by name.</summary>
        private static unsafe bool TryParseByValueOrName<TUnderlyingValue>(
            RuntimeType enumType, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, out TUnderlyingValue result)
            where TUnderlyingValue : struct, INumber<TUnderlyingValue>, IBitwiseOperators<TUnderlyingValue, TUnderlyingValue, TUnderlyingValue>, IMinMaxValue<TUnderlyingValue>
        {
            if (!value.IsEmpty)
            {
                char c = value[0];
                if (char.IsWhiteSpace(c))
                {
                    value = value.TrimStart();
                    if (value.IsEmpty)
                    {
                        goto ParseFailure;
                    }

                    c = value[0];
                }

                if (!char.IsAsciiDigit(c) && c != '-' && c != '+')
                {
                    return TryParseByName(enumType, value, ignoreCase, throwOnFailure, out result);
                }

                NumberFormatInfo numberFormat = CultureInfo.InvariantCulture.NumberFormat;
                const NumberStyles NumberStyle = NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingWhite;

                Number.ParsingStatus status;
                if (typeof(TUnderlyingValue) == typeof(int))
                {
                    Unsafe.SkipInit(out result);
                    status = Number.TryParseInt32IntegerStyle(value, NumberStyle, numberFormat, out Unsafe.As<TUnderlyingValue, int>(ref result));
                    if (status == Number.ParsingStatus.OK)
                    {
                        return true;
                    }
                }
                else if (typeof(TUnderlyingValue) == typeof(uint))
                {
                    Unsafe.SkipInit(out result);
                    status = Number.TryParseUInt32IntegerStyle(value, NumberStyle, numberFormat, out Unsafe.As<TUnderlyingValue, uint>(ref result));
                    if (status == Number.ParsingStatus.OK)
                    {
                        return true;
                    }
                }
                else if (typeof(TUnderlyingValue) == typeof(long))
                {
                    Unsafe.SkipInit(out result);
                    status = Number.TryParseInt64IntegerStyle(value, NumberStyle, numberFormat, out Unsafe.As<TUnderlyingValue, long>(ref result));
                    if (status == Number.ParsingStatus.OK)
                    {
                        return true;
                    }
                }
                else if (typeof(TUnderlyingValue) == typeof(ulong))
                {
                    Unsafe.SkipInit(out result);
                    status = Number.TryParseUInt64IntegerStyle(value, NumberStyle, numberFormat, out Unsafe.As<TUnderlyingValue, ulong>(ref result));
                    if (status == Number.ParsingStatus.OK)
                    {
                        return true;
                    }
                }
                else if (typeof(TUnderlyingValue) == typeof(byte) || typeof(TUnderlyingValue) == typeof(ushort))
                {
                    status = Number.TryParseUInt32IntegerStyle(value, NumberStyle, numberFormat, out uint uint32result);
                    if (status == Number.ParsingStatus.OK)
                    {
                        if (uint32result <= uint.CreateTruncating(TUnderlyingValue.MaxValue))
                        {
                            result = TUnderlyingValue.CreateTruncating(uint32result);
                            return true;
                        }
                        status = Number.ParsingStatus.Overflow;
                    }
                }
                else if (typeof(TUnderlyingValue) == typeof(sbyte) || typeof(TUnderlyingValue) == typeof(short))
                {
                    status = Number.TryParseInt32IntegerStyle(value, NumberStyle, numberFormat, out int int32result);
                    if (status == Number.ParsingStatus.OK)
                    {
                        if (int32result >= int.CreateTruncating(TUnderlyingValue.MinValue) && int32result <= int.CreateTruncating(TUnderlyingValue.MaxValue))
                        {
                            result = TUnderlyingValue.CreateTruncating(int32result);
                            return true;
                        }
                        status = Number.ParsingStatus.Overflow;
                    }
                }
                else
                {
                    // Rare types not expressible in C#
                    Type underlyingType = GetUnderlyingType(enumType);
                    try
                    {
                        result = (TUnderlyingValue)ToObject(enumType, Convert.ChangeType(value.ToString(), underlyingType, CultureInfo.InvariantCulture)!);
                        return true;
                    }
                    catch (FormatException)
                    {
                        status = Number.ParsingStatus.Failed; // e.g. tlbimp enums that can have values of the form "3D"
                    }
                    catch when (!throwOnFailure)
                    {
                        status = Number.ParsingStatus.Overflow; // fall through to returning failure
                    }
                }

                if (status != Number.ParsingStatus.Overflow)
                {
                    return TryParseByName(enumType, value, ignoreCase, throwOnFailure, out result);
                }

                if (throwOnFailure)
                {
                    Number.ThrowOverflowException(Type.GetTypeCode(typeof(TUnderlyingValue)));
                }
            }

            ParseFailure:
            if (throwOnFailure)
            {
                ThrowInvalidEmptyParseArgument();
            }

            result = default;
            return false;
        }

        /// <summary>Handles just the name parsing portion of <see cref="TryParseByValueOrName"/>.</summary>
        private static bool TryParseByName<TUnderlyingValue>(RuntimeType enumType, ReadOnlySpan<char> value, bool ignoreCase, bool throwOnFailure, out TUnderlyingValue result)
            where TUnderlyingValue : struct, INumber<TUnderlyingValue>, IBitwiseOperators<TUnderlyingValue, TUnderlyingValue, TUnderlyingValue>
        {
            ReadOnlySpan<char> originalValue = value;

            // Find the field. Let's assume that these are always static classes because the class is an enum.
            EnumInfo<TUnderlyingValue> enumInfo = GetEnumInfo<TUnderlyingValue>(enumType);
            string[] enumNames = enumInfo.Names;
            TUnderlyingValue[] enumValues = enumInfo.Values;

            bool parsed = true;
            TUnderlyingValue localResult = default;
            while (value.Length > 0)
            {
                // Find the next separator.
                ReadOnlySpan<char> subvalue;
                int endIndex = value.IndexOf(EnumSeparatorChar);
                if (endIndex < 0)
                {
                    // No next separator; use the remainder as the next value.
                    subvalue = value.Trim();
                    value = default;
                }
                else if (endIndex != value.Length - 1)
                {
                    // Found a separator before the last char.
                    subvalue = value.Slice(0, endIndex).Trim();
                    value = value.Slice(endIndex + 1);
                }
                else
                {
                    // Last char was a separator, which is invalid.
                    parsed = false;
                    break;
                }

                // Try to match this substring against each enum name
                bool success = false;
                if (ignoreCase)
                {
                    for (int i = 0; i < enumNames.Length; i++)
                    {
                        if (subvalue.EqualsOrdinalIgnoreCase(enumNames[i]))
                        {
                            localResult |= enumValues[i];
                            success = true;
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < enumNames.Length; i++)
                    {
                        if (subvalue.SequenceEqual(enumNames[i]))
                        {
                            localResult |= enumValues[i];
                            success = true;
                            break;
                        }
                    }
                }

                if (!success)
                {
                    parsed = false;
                    break;
                }
            }

            if (parsed)
            {
                result = localResult;
                return true;
            }

            if (throwOnFailure)
            {
                throw new ArgumentException(SR.Format(SR.Arg_EnumValueNotFound, originalValue.ToString()));
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Silently convert the <paramref name="value"/> to a <see cref="ulong"/> from the other base types for enum without
        /// throwing an exception (other than for an unknown enum type).
        /// </summary>
        /// <remarks>This is needed since the Convert functions do overflow checks.</remarks>
        internal static ulong ToUInt64(object value)
        {
            switch (Convert.GetTypeCode(value))
            {
                case TypeCode.SByte: return (ulong)(sbyte)value;
                case TypeCode.Byte: return (byte)value;
                case TypeCode.Boolean: return (bool)value ? 1UL : 0UL;
                case TypeCode.Int16: return (ulong)(short)value;
                case TypeCode.UInt16: return (ushort)value;
                case TypeCode.Char: return (char)value;
                case TypeCode.UInt32: return (uint)value;
                case TypeCode.Int32: return (ulong)(int)value;
                case TypeCode.UInt64: return (ulong)value;
                case TypeCode.Int64: return (ulong)(long)value;
            };

            if (value is not null)
            {
                Type valueType = value.GetType();
                if (valueType.IsEnum)
                {
                    valueType = valueType.GetEnumUnderlyingType();
                }

                if (valueType == typeof(nint)) return (ulong)(nint)value;
                if (valueType == typeof(nuint)) return (nuint)value;
            }

            throw CreateUnknownEnumTypeException();
        }

        /// <summary>Gets a boxed underlying value of this enum.</summary>
        internal object GetValue()
        {
            ref byte data = ref this.GetRawData();
            return InternalGetCorElementType() switch
            {
                CorElementType.ELEMENT_TYPE_I1 => Unsafe.As<byte, sbyte>(ref data),
                CorElementType.ELEMENT_TYPE_U1 => data,
                CorElementType.ELEMENT_TYPE_I2 => Unsafe.As<byte, short>(ref data),
                CorElementType.ELEMENT_TYPE_U2 => Unsafe.As<byte, ushort>(ref data),
                CorElementType.ELEMENT_TYPE_I4 => Unsafe.As<byte, int>(ref data),
                CorElementType.ELEMENT_TYPE_U4 => Unsafe.As<byte, uint>(ref data),
                CorElementType.ELEMENT_TYPE_I8 => Unsafe.As<byte, long>(ref data),
                CorElementType.ELEMENT_TYPE_U8 => Unsafe.As<byte, ulong>(ref data),
                CorElementType.ELEMENT_TYPE_R4 => Unsafe.As<byte, float>(ref data),
                CorElementType.ELEMENT_TYPE_R8 => Unsafe.As<byte, double>(ref data),
                CorElementType.ELEMENT_TYPE_I => Unsafe.As<byte, IntPtr>(ref data),
                CorElementType.ELEMENT_TYPE_U => Unsafe.As<byte, UIntPtr>(ref data),
                CorElementType.ELEMENT_TYPE_CHAR => Unsafe.As<byte, char>(ref data),
                CorElementType.ELEMENT_TYPE_BOOLEAN => Unsafe.As<byte, bool>(ref data),
                _ => throw CreateUnknownEnumTypeException(),
            };
        }

        /// <inheritdoc/>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is null)
                return false;

            if (this == obj)
                return true;

            if (this.GetType() != obj.GetType())
                return false;

            ref byte pThisValue = ref this.GetRawData();
            ref byte pOtherValue = ref obj.GetRawData();

            switch (InternalGetCorElementType())
            {
                case CorElementType.ELEMENT_TYPE_I1:
                case CorElementType.ELEMENT_TYPE_U1:
                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                    return pThisValue == pOtherValue;

                case CorElementType.ELEMENT_TYPE_I2:
                case CorElementType.ELEMENT_TYPE_U2:
                case CorElementType.ELEMENT_TYPE_CHAR:
                    return Unsafe.As<byte, ushort>(ref pThisValue) == Unsafe.As<byte, ushort>(ref pOtherValue);

                case CorElementType.ELEMENT_TYPE_I4:
                case CorElementType.ELEMENT_TYPE_U4:
#if TARGET_32BIT
                case CorElementType.ELEMENT_TYPE_I:
                case CorElementType.ELEMENT_TYPE_U:
#endif
                case CorElementType.ELEMENT_TYPE_R4:
                    return Unsafe.As<byte, uint>(ref pThisValue) == Unsafe.As<byte, uint>(ref pOtherValue);

                case CorElementType.ELEMENT_TYPE_I8:
                case CorElementType.ELEMENT_TYPE_U8:
#if TARGET_64BIT
                case CorElementType.ELEMENT_TYPE_I:
                case CorElementType.ELEMENT_TYPE_U:
#endif
                case CorElementType.ELEMENT_TYPE_R8:
                    return Unsafe.As<byte, ulong>(ref pThisValue) == Unsafe.As<byte, ulong>(ref pOtherValue);

                default:
                    Debug.Fail("Unknown enum underlying type");
                    return false;
            }
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            // CONTRACT with the runtime: GetHashCode of enum types is implemented as GetHashCode of the underlying type.
            // The runtime can bypass calls to Enum::GetHashCode and call the underlying type's GetHashCode directly
            // to avoid boxing the enum.
            ref byte data = ref this.GetRawData();
            return InternalGetCorElementType() switch
            {
                CorElementType.ELEMENT_TYPE_I1 => Unsafe.As<byte, sbyte>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_U1 => data.GetHashCode(),
                CorElementType.ELEMENT_TYPE_I2 => Unsafe.As<byte, short>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_U2 => Unsafe.As<byte, ushort>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_I4 => Unsafe.As<byte, int>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_U4 => Unsafe.As<byte, uint>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_I8 => Unsafe.As<byte, long>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_U8 => Unsafe.As<byte, ulong>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_R4 => Unsafe.As<byte, float>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_R8 => Unsafe.As<byte, double>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_I => Unsafe.As<byte, IntPtr>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_U => Unsafe.As<byte, UIntPtr>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_CHAR => Unsafe.As<byte, char>(ref data).GetHashCode(),
                CorElementType.ELEMENT_TYPE_BOOLEAN => Unsafe.As<byte, bool>(ref data).GetHashCode(),
                _ => throw CreateUnknownEnumTypeException(),
            };
        }

        /// <inheritdoc/>
        public int CompareTo(object? target)
        {
            if (target == this)
                return 0;

            if (target == null)
                return 1; // all values are greater than null

            if (GetType() != target.GetType())
                throw new ArgumentException(SR.Format(SR.Arg_EnumAndObjectMustBeSameType, target.GetType(), GetType()));

            ref byte pThisValue = ref this.GetRawData();
            ref byte pTargetValue = ref target.GetRawData();

            switch (InternalGetCorElementType())
            {
                case CorElementType.ELEMENT_TYPE_I1:
                    return Unsafe.As<byte, sbyte>(ref pThisValue).CompareTo(Unsafe.As<byte, sbyte>(ref pTargetValue));

                case CorElementType.ELEMENT_TYPE_U1:
                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                    return pThisValue.CompareTo(pTargetValue);

                case CorElementType.ELEMENT_TYPE_I2:
                    return Unsafe.As<byte, short>(ref pThisValue).CompareTo(Unsafe.As<byte, short>(ref pTargetValue));

                case CorElementType.ELEMENT_TYPE_U2:
                case CorElementType.ELEMENT_TYPE_CHAR:
                    return Unsafe.As<byte, ushort>(ref pThisValue).CompareTo(Unsafe.As<byte, ushort>(ref pTargetValue));

                case CorElementType.ELEMENT_TYPE_I4:
#if TARGET_32BIT
                case CorElementType.ELEMENT_TYPE_I:
#endif
                    return Unsafe.As<byte, int>(ref pThisValue).CompareTo(Unsafe.As<byte, int>(ref pTargetValue));

                case CorElementType.ELEMENT_TYPE_U4:
#if TARGET_32BIT
                case CorElementType.ELEMENT_TYPE_U:
#endif
                    return Unsafe.As<byte, uint>(ref pThisValue).CompareTo(Unsafe.As<byte, uint>(ref pTargetValue));

                case CorElementType.ELEMENT_TYPE_I8:
#if TARGET_64BIT
                case CorElementType.ELEMENT_TYPE_I:
#endif
                    return Unsafe.As<byte, long>(ref pThisValue).CompareTo(Unsafe.As<byte, long>(ref pTargetValue));

                case CorElementType.ELEMENT_TYPE_U8:
#if TARGET_64BIT
                case CorElementType.ELEMENT_TYPE_U:
#endif
                    return Unsafe.As<byte, ulong>(ref pThisValue).CompareTo(Unsafe.As<byte, ulong>(ref pTargetValue));

                case CorElementType.ELEMENT_TYPE_R4:
                    return Unsafe.As<byte, float>(ref pThisValue).CompareTo(Unsafe.As<byte, float>(ref pTargetValue));

                case CorElementType.ELEMENT_TYPE_R8:
                    return Unsafe.As<byte, double>(ref pThisValue).CompareTo(Unsafe.As<byte, double>(ref pTargetValue));

                default:
                    Debug.Fail("Unknown enum underlying type");
                    return 0;
            }
        }

        /// <summary>Converts the value of this instance to its equivalent string representation.</summary>
        /// <remarks>The string representation of the value of this instance.</remarks>
        public override string ToString()
        {
            RuntimeType enumType = (RuntimeType)GetType();
            ref byte rawData = ref this.GetRawData();
            return InternalGetCorElementType() switch
            {
                // Inlined for the most common base types
                CorElementType.ELEMENT_TYPE_I1 => ToString<sbyte>(enumType, ref rawData),
                CorElementType.ELEMENT_TYPE_U1 => ToStringInlined<byte>(enumType, ref rawData),
                CorElementType.ELEMENT_TYPE_I2 => ToString<short>(enumType, ref rawData),
                CorElementType.ELEMENT_TYPE_U2 => ToString<ushort>(enumType, ref rawData),
                CorElementType.ELEMENT_TYPE_I4 => ToStringInlined<int>(enumType, ref rawData),
                CorElementType.ELEMENT_TYPE_U4 => ToString<uint>(enumType, ref rawData),
                CorElementType.ELEMENT_TYPE_I8 => ToString<long>(enumType, ref rawData),
                CorElementType.ELEMENT_TYPE_U8 => ToString<ulong>(enumType, ref rawData),
                _ => HandleRareTypes(enumType, ref rawData)
            };

            [MethodImpl(MethodImplOptions.NoInlining)]
            static string HandleRareTypes(RuntimeType enumType, ref byte rawData) =>
                InternalGetCorElementType(enumType) switch
                {
                    CorElementType.ELEMENT_TYPE_R4 => ToString<float>(enumType, ref rawData),
                    CorElementType.ELEMENT_TYPE_R8 => ToString<double>(enumType, ref rawData),
                    CorElementType.ELEMENT_TYPE_I => ToString<nint>(enumType, ref rawData),
                    CorElementType.ELEMENT_TYPE_U => ToString<nuint>(enumType, ref rawData),
                    CorElementType.ELEMENT_TYPE_CHAR => ToString<char>(enumType, ref rawData),
                    CorElementType.ELEMENT_TYPE_BOOLEAN => ToStringBool(enumType, null, ref rawData),
                    _ => throw CreateUnknownEnumTypeException(),
                };
        }

        /// <summary>Converts the value of this instance to its equivalent string representation using the specified format.</summary>
        /// <param name="format">A format string.</param>
        /// <returns>The string representation of the value of this instance as specified by <paramref name="format"/>.</returns>
        /// <exception cref="FormatException"><paramref name="format"/> contains an invalid specification.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="format"/> equals "X" or "x", but the enumeration type is unknown.</exception>
        public string ToString([StringSyntax(StringSyntaxAttribute.EnumFormat)] string? format)
        {
            if (string.IsNullOrEmpty(format))
            {
                return ToString();
            }

            if (format.Length == 1)
            {
                char formatChar = format[0];
                RuntimeType enumType = (RuntimeType)GetType();
                ref byte rawData = ref this.GetRawData();
                return InternalGetCorElementType() switch
                {
                    // Inlined for the most common base types
                    CorElementType.ELEMENT_TYPE_I1 => ToString<sbyte>(enumType, formatChar, ref rawData),
                    CorElementType.ELEMENT_TYPE_U1 => ToStringInlined<byte>(enumType, formatChar, ref rawData),
                    CorElementType.ELEMENT_TYPE_I2 => ToString<short>(enumType, formatChar, ref rawData),
                    CorElementType.ELEMENT_TYPE_U2 => ToString<ushort>(enumType, formatChar, ref rawData),
                    CorElementType.ELEMENT_TYPE_I4 => ToStringInlined<int>(enumType, formatChar, ref rawData),
                    CorElementType.ELEMENT_TYPE_U4 => ToString<uint>(enumType, formatChar, ref rawData),
                    CorElementType.ELEMENT_TYPE_I8 => ToString<long>(enumType, formatChar, ref rawData),
                    CorElementType.ELEMENT_TYPE_U8 => ToString<ulong>(enumType, formatChar, ref rawData),
                    _ => HandleRareTypes(enumType, formatChar, ref rawData)
                };
            }

            throw CreateInvalidFormatSpecifierException();

            [MethodImpl(MethodImplOptions.NoInlining)]
            static string HandleRareTypes(RuntimeType enumType, char formatChar, ref byte rawData) =>
                InternalGetCorElementType(enumType) switch
                {
                    CorElementType.ELEMENT_TYPE_R4 => ToString<float>(enumType, formatChar, ref rawData),
                    CorElementType.ELEMENT_TYPE_R8 => ToString<double>(enumType, formatChar, ref rawData),
                    CorElementType.ELEMENT_TYPE_I => ToString<nint>(enumType, formatChar, ref rawData),
                    CorElementType.ELEMENT_TYPE_U => ToString<nuint>(enumType, formatChar, ref rawData),
                    CorElementType.ELEMENT_TYPE_CHAR => ToString<char>(enumType, formatChar, ref rawData),
                    CorElementType.ELEMENT_TYPE_BOOLEAN => ToStringBool(enumType, formatChar.ToString(), ref rawData),
                    _ => throw CreateUnknownEnumTypeException(),
                };
        }

        /// <summary>This method overload is obsolete; use <see cref="ToString()"/>.</summary>
        [Obsolete("The provider argument is not used. Use ToString() instead.")]
        public string ToString(IFormatProvider? provider) =>
            ToString();

        /// <summary>This method overload is obsolete; use <see cref="ToString(string)"/>.</summary>
        [Obsolete("The provider argument is not used. Use ToString(String) instead.")]
        public string ToString([StringSyntax(StringSyntaxAttribute.EnumFormat)] string? format, IFormatProvider? provider) =>
            ToString(format);

        [MethodImpl(MethodImplOptions.NoInlining)] // avoid bloating call sites for underlying types and/or call sites that aren't perf critical
        private static string ToString<TUnderlyingValue>(RuntimeType enumType, ref byte rawData)
            where TUnderlyingValue : struct, INumber<TUnderlyingValue>, IBitwiseOperators<TUnderlyingValue, TUnderlyingValue, TUnderlyingValue> =>
            ToStringInlined<TUnderlyingValue>(enumType, ref rawData);

        /// <summary>Converts the value of an enum to its equivalent string representation using the default format.</summary>
        /// <typeparam name="TUnderlyingValue">The type of the underlying value.</typeparam>
        /// <param name="enumType">The enum type.</param>
        /// <param name="rawData">A reference to the enum's value.</param>
        /// <returns>The string representation of the value of this instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // used for most important types at most important call sites
        private static string ToStringInlined<TUnderlyingValue>(RuntimeType enumType, ref byte rawData)
            where TUnderlyingValue : struct, INumber<TUnderlyingValue>, IBitwiseOperators<TUnderlyingValue, TUnderlyingValue, TUnderlyingValue>
        {
            TUnderlyingValue value = Unsafe.As<byte, TUnderlyingValue>(ref rawData);

            EnumInfo<TUnderlyingValue> enumInfo = GetEnumInfo<TUnderlyingValue>(enumType);
            string? result = enumInfo.HasFlagsAttribute ?
                FormatFlagNames(enumInfo, value) :
                GetNameInlined(enumInfo, value);

            return result ?? value.ToString()!;
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // avoid bloating call sites for underlying types and/or call sites that aren't perf critical
        private static string ToString<TUnderlyingValue>(RuntimeType enumType, char format, ref byte rawData)
            where TUnderlyingValue : struct, INumber<TUnderlyingValue>, IBitwiseOperators<TUnderlyingValue, TUnderlyingValue, TUnderlyingValue>, IMinMaxValue<TUnderlyingValue> =>
            ToStringInlined<TUnderlyingValue>(enumType, format, ref rawData);

        /// <summary>Converts the value of an enum to its equivalent string representation using the default format.</summary>
        /// <typeparam name="TUnderlyingValue">The type of the underlying value.</typeparam>
        /// <param name="enumType">The enum type.</param>
        /// <param name="format">A format string.</param>
        /// <param name="rawData">A reference to the enum's value.</param>
        /// <returns>The string representation of the value of this instance as specified by <paramref name="format"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // used for most important types at most important call sites
        private static string ToStringInlined<TUnderlyingValue>(RuntimeType enumType, char format, ref byte rawData)
            where TUnderlyingValue : struct, INumber<TUnderlyingValue>, IBitwiseOperators<TUnderlyingValue, TUnderlyingValue, TUnderlyingValue>, IMinMaxValue<TUnderlyingValue>
        {
            TUnderlyingValue value = Unsafe.As<byte, TUnderlyingValue>(ref rawData);

            string? result;
            switch (format | 0x20)
            {
                case 'g':
                    EnumInfo<TUnderlyingValue> enumInfo = GetEnumInfo<TUnderlyingValue>(enumType);
                    result = enumInfo.HasFlagsAttribute ? FormatFlagNames(enumInfo, value) : GetNameInlined(enumInfo, value);
                    if (result is null)
                    {
                        goto case 'd';
                    }
                    break;

                case 'd':
                    result = value.ToString()!;
                    break;

                case 'x':
                    result = FormatNumberAsHex<TUnderlyingValue>(ref rawData);
                    break;

                case 'f':
                    result = FormatFlagNames(GetEnumInfo<TUnderlyingValue>(enumType), value);
                    if (result is null)
                    {
                        goto case 'd';
                    }
                    break;

                default:
                    throw CreateInvalidFormatSpecifierException();
            };

            return result;
        }

        /// <summary>ToString implementation to use for enums with bool as an underlying type.</summary>
        /// <remarks>
        /// bool is the only underlying type that doesn't fit our scheme using INumber{T}, since it doesn't implement the numeric interfaces.
        /// We can mostly make everything work for it by substituting byte, but for some rendering purposes we need to use a dedicated implementation.
        /// Performance here is not an issue; these can't be expressed in C#, and serve little purpose, so bool-based enums are exceedingly rare.
        /// This exists purely for compat.
        /// </remarks>
        private static string ToStringBool(RuntimeType enumType, string? format, ref byte rawData)
        {
            if (string.IsNullOrEmpty(format))
            {
                format = "g";
            }

            byte byteValue = rawData;
            bool boolValue = byteValue != 0;

            string? result;
            switch (format[0] | 0x20)
            {
                case 'g':
                    EnumInfo<byte> enumInfo = GetEnumInfo<byte>(enumType);
                    result = enumInfo.HasFlagsAttribute ? FormatFlagNames(enumInfo, byteValue) : GetName(enumInfo, byteValue);
                    if (result is null)
                    {
                        goto case 'd';
                    }
                    break;

                case 'd':
                    return boolValue.ToString();

                case 'x':
                    return boolValue ? "01" : "00";

                case 'f':
                    result = FormatFlagNames(GetEnumInfo<byte>(enumType), byteValue);
                    if (result is null)
                    {
                        goto case 'd';
                    }
                    break;

                default:
                    throw CreateInvalidFormatSpecifierException();
            }

            return result;
        }

        /// <summary>TryFormat implementation to use for enums with bool as an underlying type.</summary>
        /// <remarks>See remarks on <see cref="ToStringBool"/>.</remarks>
        private static bool TryFormatBool(RuntimeType enumType, bool boolValue, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format)
        {
            byte byteValue = boolValue ? (byte)1 : (byte)0;

            string result = ToStringBool(enumType, format.ToString(), ref byteValue);
            if (result.TryCopyTo(destination))
            {
                charsWritten = result.Length;
                return true;
            }

            charsWritten = 0;
            return false;
        }

        /// <summary>Formats the data for the underlying value as hex into a new, fixed-length string.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe string FormatNumberAsHex<TUnderlyingValue>(ref byte data) where TUnderlyingValue : struct
        {
            fixed (byte* ptr = &data)
            {
                return string.Create(Unsafe.SizeOf<TUnderlyingValue>() * 2, (IntPtr)ptr, (destination, intptr) =>
                {
                    bool success = TryFormatNumberAsHex<TUnderlyingValue>(ref *(byte*)intptr, destination, out int charsWritten);
                    Debug.Assert(success);
                    Debug.Assert(charsWritten == Unsafe.SizeOf<TUnderlyingValue>() * 2);
                });
            }
        }

        /// <summary>Tries to format the data for the underlying value as hex into the destination span.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryFormatNumberAsHex<TUnderlyingValue>(ref byte data, Span<char> destination, out int charsWritten) where TUnderlyingValue : struct
        {
            if (Unsafe.SizeOf<TUnderlyingValue>() * 2 <= destination.Length)
            {
                if (typeof(TUnderlyingValue) == typeof(byte) ||
                    typeof(TUnderlyingValue) == typeof(sbyte))
                {
                    HexConverter.ToCharsBuffer(data, destination);
                }
                else if (typeof(TUnderlyingValue) == typeof(ushort) ||
                         typeof(TUnderlyingValue) == typeof(short) ||
                         typeof(TUnderlyingValue) == typeof(char))
                {
                    ushort value = Unsafe.As<byte, ushort>(ref data);
                    HexConverter.ToCharsBuffer((byte)(value >> 8), destination);
                    HexConverter.ToCharsBuffer((byte)value, destination, 2);
                }
                else if (typeof(TUnderlyingValue) == typeof(uint) ||
#if TARGET_32BIT
                         typeof(TUnderlyingValue) == typeof(nuint) ||
                         typeof(TUnderlyingValue) == typeof(nint) ||
#endif
                         typeof(TUnderlyingValue) == typeof(int))
                {
                    uint value = Unsafe.As<byte, uint>(ref data);
                    HexConverter.ToCharsBuffer((byte)(value >> 24), destination);
                    HexConverter.ToCharsBuffer((byte)(value >> 16), destination, 2);
                    HexConverter.ToCharsBuffer((byte)(value >> 8), destination, 4);
                    HexConverter.ToCharsBuffer((byte)value, destination, 6);
                }
                else if (typeof(TUnderlyingValue) == typeof(ulong) ||
#if TARGET_64BIT
                         typeof(TUnderlyingValue) == typeof(nuint) ||
                         typeof(TUnderlyingValue) == typeof(nint) ||
#endif
                         typeof(TUnderlyingValue) == typeof(long))
                {
                    ulong value = Unsafe.As<byte, ulong>(ref data);
                    HexConverter.ToCharsBuffer((byte)(value >> 56), destination);
                    HexConverter.ToCharsBuffer((byte)(value >> 48), destination, 2);
                    HexConverter.ToCharsBuffer((byte)(value >> 40), destination, 4);
                    HexConverter.ToCharsBuffer((byte)(value >> 32), destination, 6);
                    HexConverter.ToCharsBuffer((byte)(value >> 24), destination, 8);
                    HexConverter.ToCharsBuffer((byte)(value >> 16), destination, 10);
                    HexConverter.ToCharsBuffer((byte)(value >> 8), destination, 12);
                    HexConverter.ToCharsBuffer((byte)value, destination, 14);
                }
                else
                {
                    throw new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);
                }

                charsWritten = Unsafe.SizeOf<TUnderlyingValue>() * 2;
                return true;
            }

            charsWritten = 0;
            return false;
        }

        /// <summary>Converts the specified value of a specified enumerated type to its equivalent string representation according to the specified format.</summary>
        /// <param name="enumType">The enumeration type of the value to convert.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="format">The output format to use.</param>
        /// <returns>A string representation of <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="enumType"/>, <paramref name="value"/>, or <paramref name="format"/> parameter is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="enumType"/> parameter is not an <see cref="Enum"/> type.</exception>
        /// <exception cref="ArgumentException">The <paramref name="value"/> is from an enumeration that differs in type from <paramref name="enumType"/>.</exception>
        /// <exception cref="ArgumentException">The type of <paramref name="value"/> is not an underlying type of <paramref name="enumType"/>.</exception>
        /// <exception cref="FormatException"><paramref name="format"/> contains an invalid value.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="format"/> equals "X" or "x", but the enumeration type is unknown.</exception>
        public static string Format(Type enumType, object value, [StringSyntax(StringSyntaxAttribute.EnumFormat)] string format)
        {
            ArgumentNullException.ThrowIfNull(value);
            ArgumentNullException.ThrowIfNull(format);

            RuntimeType rtType = ValidateRuntimeType(enumType);

            Type valueType = value.GetType();
            if (valueType.IsEnum)
            {
                // If the value is an enum type, then it must be equivalent to the specified type.
                if (!valueType.IsEquivalentTo(rtType))
                    throw new ArgumentException(SR.Format(SR.Arg_EnumAndObjectMustBeSameType, valueType, rtType));

                // If the format isn't empty, just delegate to ToString(format). The length check is necessary
                // here for compat, as Enum.Format prohibits a null or empty format whereas ToString(string) allows it.
                if (format.Length == 1)
                    return ((Enum)value).ToString(format);
            }
            else
            {
                // The value isn't an enum type. It's either an underlying type or it's invalid,
                // and as an underlying type, it must match the underlying type of the enum type.
                Type underlyingType = GetUnderlyingType(rtType);
                if (valueType != underlyingType)
                    throw new ArgumentException(SR.Format(SR.Arg_EnumFormatUnderlyingTypeAndObjectMustBeSameType, valueType, underlyingType));

                // If the format isn't empty, delegate to ToString with the format.
                if (format.Length == 1)
                {
                    char formatChar = format[0];
                    ref byte rawData = ref value.GetRawData();
                    return InternalGetCorElementType(rtType) switch
                    {
                        CorElementType.ELEMENT_TYPE_I1 => ToString<sbyte>(rtType, formatChar, ref rawData),
                        CorElementType.ELEMENT_TYPE_U1 => ToString<byte>(rtType, formatChar, ref rawData),
                        CorElementType.ELEMENT_TYPE_I2 => ToString<short>(rtType, formatChar, ref rawData),
                        CorElementType.ELEMENT_TYPE_U2 => ToString<ushort>(rtType, formatChar, ref rawData),
                        CorElementType.ELEMENT_TYPE_I4 => ToString<int>(rtType, formatChar, ref rawData),
                        CorElementType.ELEMENT_TYPE_U4 => ToString<uint>(rtType, formatChar, ref rawData),
                        CorElementType.ELEMENT_TYPE_I8 => ToString<long>(rtType, formatChar, ref rawData),
                        CorElementType.ELEMENT_TYPE_U8 => ToString<ulong>(rtType, formatChar, ref rawData),
                        CorElementType.ELEMENT_TYPE_R4 => ToString<float>(rtType, formatChar, ref rawData),
                        CorElementType.ELEMENT_TYPE_R8 => ToString<double>(rtType, formatChar, ref rawData),
                        CorElementType.ELEMENT_TYPE_I => ToString<nint>(rtType, formatChar, ref rawData),
                        CorElementType.ELEMENT_TYPE_U => ToString<nuint>(rtType, formatChar, ref rawData),
                        CorElementType.ELEMENT_TYPE_CHAR => ToString<char>(rtType, formatChar, ref rawData),
                        CorElementType.ELEMENT_TYPE_BOOLEAN => ToStringBool(rtType, format, ref rawData),
                        _ => throw CreateUnknownEnumTypeException(),
                    };
                }
            }

            throw CreateInvalidFormatSpecifierException();
        }

        /// <summary>Tries to format the value of the enum into the provided span of characters.</summary>
        /// <param name="destination">The span in which to write this instance's value formatted as a span of characters.</param>
        /// <param name="charsWritten">When this method returns, contains the number of characters that were written in destination.</param>
        /// <param name="format">The format specifier.</param>
        /// <param name="provider">An optional object that supplies culture-specific formatting information for destination. This is ignored.</param>
        /// <returns><see langword="true"/> if the formatting was successful; otherwise, <see langword="false"/>.</returns>
        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            RuntimeType enumType = (RuntimeType)GetType();
            ref byte rawData = ref this.GetRawData();
            CorElementType corElementType = InternalGetCorElementType();

            if (format.IsEmpty)
            {
                return corElementType switch
                {
                    CorElementType.ELEMENT_TYPE_I1 => TryFormatPrimitiveDefault(enumType, (sbyte)rawData, destination, out charsWritten),
                    CorElementType.ELEMENT_TYPE_U1 => TryFormatPrimitiveDefault(enumType, rawData, destination, out charsWritten),
                    CorElementType.ELEMENT_TYPE_I2 => TryFormatPrimitiveDefault(enumType, Unsafe.As<byte, short>(ref rawData), destination, out charsWritten),
                    CorElementType.ELEMENT_TYPE_U2 => TryFormatPrimitiveDefault(enumType, Unsafe.As<byte, ushort>(ref rawData), destination, out charsWritten),
                    CorElementType.ELEMENT_TYPE_I4 => TryFormatPrimitiveDefault(enumType, Unsafe.As<byte, int>(ref rawData), destination, out charsWritten),
                    CorElementType.ELEMENT_TYPE_U4 => TryFormatPrimitiveDefault(enumType, Unsafe.As<byte, uint>(ref rawData), destination, out charsWritten),
                    CorElementType.ELEMENT_TYPE_I8 => TryFormatPrimitiveDefault(enumType, Unsafe.As<byte, long>(ref rawData), destination, out charsWritten),
                    CorElementType.ELEMENT_TYPE_U8 => TryFormatPrimitiveDefault(enumType, Unsafe.As<byte, ulong>(ref rawData), destination, out charsWritten),
                    CorElementType.ELEMENT_TYPE_R4 => TryFormatPrimitiveDefault(enumType, Unsafe.As<byte, float>(ref rawData), destination, out charsWritten),
                    CorElementType.ELEMENT_TYPE_R8 => TryFormatPrimitiveDefault(enumType, Unsafe.As<byte, double>(ref rawData), destination, out charsWritten),
                    CorElementType.ELEMENT_TYPE_I => TryFormatPrimitiveDefault(enumType, Unsafe.As<byte, nint>(ref rawData), destination, out charsWritten),
                    CorElementType.ELEMENT_TYPE_U => TryFormatPrimitiveDefault(enumType, Unsafe.As<byte, nuint>(ref rawData), destination, out charsWritten),
                    CorElementType.ELEMENT_TYPE_CHAR => TryFormatPrimitiveDefault(enumType, Unsafe.As<byte, char>(ref rawData), destination, out charsWritten),
                    CorElementType.ELEMENT_TYPE_BOOLEAN => TryFormatBool(enumType, rawData != 0, destination, out charsWritten, format),
                    _ => throw CreateUnknownEnumTypeException(),
                };
            }
            else
            {
                return corElementType switch
                {
                    CorElementType.ELEMENT_TYPE_I1 => TryFormatPrimitiveNonDefault(enumType, (sbyte)rawData, destination, out charsWritten, format),
                    CorElementType.ELEMENT_TYPE_U1 => TryFormatPrimitiveNonDefault(enumType, rawData, destination, out charsWritten, format),
                    CorElementType.ELEMENT_TYPE_I2 => TryFormatPrimitiveNonDefault(enumType, Unsafe.As<byte, short>(ref rawData), destination, out charsWritten, format),
                    CorElementType.ELEMENT_TYPE_U2 => TryFormatPrimitiveNonDefault(enumType, Unsafe.As<byte, ushort>(ref rawData), destination, out charsWritten, format),
                    CorElementType.ELEMENT_TYPE_I4 => TryFormatPrimitiveNonDefault(enumType, Unsafe.As<byte, int>(ref rawData), destination, out charsWritten, format),
                    CorElementType.ELEMENT_TYPE_U4 => TryFormatPrimitiveNonDefault(enumType, Unsafe.As<byte, uint>(ref rawData), destination, out charsWritten, format),
                    CorElementType.ELEMENT_TYPE_I8 => TryFormatPrimitiveNonDefault(enumType, Unsafe.As<byte, long>(ref rawData), destination, out charsWritten, format),
                    CorElementType.ELEMENT_TYPE_U8 => TryFormatPrimitiveNonDefault(enumType, Unsafe.As<byte, ulong>(ref rawData), destination, out charsWritten, format),
                    CorElementType.ELEMENT_TYPE_R4 => TryFormatPrimitiveNonDefault(enumType, Unsafe.As<byte, float>(ref rawData), destination, out charsWritten, format),
                    CorElementType.ELEMENT_TYPE_R8 => TryFormatPrimitiveNonDefault(enumType, Unsafe.As<byte, double>(ref rawData), destination, out charsWritten, format),
                    CorElementType.ELEMENT_TYPE_I => TryFormatPrimitiveNonDefault(enumType, Unsafe.As<byte, nint>(ref rawData), destination, out charsWritten, format),
                    CorElementType.ELEMENT_TYPE_U => TryFormatPrimitiveNonDefault(enumType, Unsafe.As<byte, nuint>(ref rawData), destination, out charsWritten, format),
                    CorElementType.ELEMENT_TYPE_CHAR => TryFormatPrimitiveNonDefault(enumType, Unsafe.As<byte, char>(ref rawData), destination, out charsWritten, format),
                    CorElementType.ELEMENT_TYPE_BOOLEAN => TryFormatBool(enumType, rawData != 0, destination, out charsWritten, format),
                    _ => throw CreateUnknownEnumTypeException(),
                };
            }
        }

        /// <summary>Tries to format the value of the enumerated type instance into the provided span of characters.</summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="value"></param>
        /// <param name="destination">The span into which to write the instance's value formatted as a span of characters.</param>
        /// <param name="charsWritten">When this method returns, contains the number of characters that were written in <paramref name="destination"/>.</param>
        /// <param name="format">A span containing the character that represents the standard format string that defines the acceptable format of destination. This may be empty, or "g", "d", "f", or "x".</param>
        /// <returns><see langword="true"/> if the formatting was successful; otherwise, <see langword="false"/> if the destination span wasn't large enough to contain the formatted value.</returns>
        /// <exception cref="FormatException">The format parameter contains an invalid value.</exception>
        public static unsafe bool TryFormat<TEnum>(TEnum value, Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.EnumFormat)] ReadOnlySpan<char> format = default) where TEnum : struct, Enum
        {
            RuntimeType rt = (RuntimeType)typeof(TEnum);
            Type underlyingType = typeof(TEnum).GetEnumUnderlyingType();

            // If the format is empty, which is the most common case, delegate to the default implementation that doesn't take a format.
            // That implementation is more optimized. Doing this check here means in the common case where TryFormat is inlined and no format
            // is provided, this whole call can become just a call to the default method.  Even if it's not inlined, this check would still otherwise
            // be necessary for semantics inside of TryFormatPrimitiveNonDefault, so we can just do it here instead.
            if (format.IsEmpty)
            {
                if (underlyingType == typeof(int)) return TryFormatPrimitiveDefault(rt, *(int*)&value, destination, out charsWritten);
                if (underlyingType == typeof(uint)) return TryFormatPrimitiveDefault(rt, *(uint*)&value, destination, out charsWritten);
                if (underlyingType == typeof(long)) return TryFormatPrimitiveDefault(rt, *(long*)&value, destination, out charsWritten);
                if (underlyingType == typeof(ulong)) return TryFormatPrimitiveDefault(rt, *(ulong*)&value, destination, out charsWritten);
                if (underlyingType == typeof(byte)) return TryFormatPrimitiveDefault(rt, *(byte*)&value, destination, out charsWritten);
                if (underlyingType == typeof(sbyte)) return TryFormatPrimitiveDefault(rt, *(sbyte*)&value, destination, out charsWritten);
                if (underlyingType == typeof(short)) return TryFormatPrimitiveDefault(rt, *(short*)&value, destination, out charsWritten);
                if (underlyingType == typeof(ushort)) return TryFormatPrimitiveDefault(rt, *(ushort*)&value, destination, out charsWritten);
                if (underlyingType == typeof(nint)) return TryFormatPrimitiveDefault(rt, *(nint*)&value, destination, out charsWritten);
                if (underlyingType == typeof(nuint)) return TryFormatPrimitiveDefault(rt, *(nuint*)&value, destination, out charsWritten);
                if (underlyingType == typeof(char)) return TryFormatPrimitiveDefault(rt, *(char*)&value, destination, out charsWritten);
                if (underlyingType == typeof(float)) return TryFormatPrimitiveDefault(rt, *(float*)&value, destination, out charsWritten);
                if (underlyingType == typeof(double)) return TryFormatPrimitiveDefault(rt, *(double*)&value, destination, out charsWritten);
                if (underlyingType == typeof(bool)) return TryFormatBool(rt, *(bool*)&value, destination, out charsWritten, format: default);
            }
            else
            {
                if (underlyingType == typeof(int)) return TryFormatPrimitiveNonDefault(rt, *(int*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(uint)) return TryFormatPrimitiveNonDefault(rt, *(uint*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(long)) return TryFormatPrimitiveNonDefault(rt, *(long*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(ulong)) return TryFormatPrimitiveNonDefault(rt, *(ulong*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(byte)) return TryFormatPrimitiveNonDefault(rt, *(byte*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(sbyte)) return TryFormatPrimitiveNonDefault(rt, *(sbyte*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(short)) return TryFormatPrimitiveNonDefault(rt, *(short*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(ushort)) return TryFormatPrimitiveNonDefault(rt, *(ushort*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(nint)) return TryFormatPrimitiveNonDefault(rt, *(nint*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(nuint)) return TryFormatPrimitiveNonDefault(rt, *(nuint*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(char)) return TryFormatPrimitiveNonDefault(rt, *(char*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(float)) return TryFormatPrimitiveNonDefault(rt, *(float*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(double)) return TryFormatPrimitiveNonDefault(rt, *(double*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(bool)) return TryFormatBool(rt, *(bool*)&value, destination, out charsWritten, format);
            }

            throw CreateUnknownEnumTypeException();
        }

        /// <summary>Tries to format the value of the enumerated type instance into the provided span of characters.</summary>
        /// <remarks>
        /// This is same as the implementation for <see cref="TryFormat"/>. It is separated out as <see cref="TryFormat"/> has constrains on the TEnum,
        /// and we internally want to use this method in cases where we dynamically validate a generic T is an enum rather than T implementing
        /// those constraints. It's a manual copy/paste right now to avoid pressure on the JIT's inlining mechanisms.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // format is most frequently a constant, and we want it exposed to the implementation; this should be inlined automatically, anyway
        internal static unsafe bool TryFormatUnconstrained<TEnum>(TEnum value, Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.EnumFormat)] ReadOnlySpan<char> format = default)
        {
            Debug.Assert(typeof(TEnum).IsEnum);
            Debug.Assert(value is not null);

            RuntimeType rt = (RuntimeType)typeof(TEnum);
            Type underlyingType = typeof(TEnum).GetEnumUnderlyingType();

            // If the format is empty, which is the most common case, delegate to the default implementation that doesn't take a format.
            // That implementation is more optimized. Doing this check here means in the common case where TryFormat is inlined and no format
            // is provided, this whole call can become just a call to the default method.  Even if it's not inlined, this check would still otherwise
            // be necessary for semantics inside of TryFormatPrimitiveNonDefault, so we can just do it here instead.
            if (format.IsEmpty)
            {
                if (underlyingType == typeof(int)) return TryFormatPrimitiveDefault(rt, *(int*)&value, destination, out charsWritten);
                if (underlyingType == typeof(uint)) return TryFormatPrimitiveDefault(rt, *(uint*)&value, destination, out charsWritten);
                if (underlyingType == typeof(long)) return TryFormatPrimitiveDefault(rt, *(long*)&value, destination, out charsWritten);
                if (underlyingType == typeof(ulong)) return TryFormatPrimitiveDefault(rt, *(ulong*)&value, destination, out charsWritten);
                if (underlyingType == typeof(byte)) return TryFormatPrimitiveDefault(rt, *(byte*)&value, destination, out charsWritten);
                if (underlyingType == typeof(sbyte)) return TryFormatPrimitiveDefault(rt, *(sbyte*)&value, destination, out charsWritten);
                if (underlyingType == typeof(short)) return TryFormatPrimitiveDefault(rt, *(short*)&value, destination, out charsWritten);
                if (underlyingType == typeof(ushort)) return TryFormatPrimitiveDefault(rt, *(ushort*)&value, destination, out charsWritten);
                if (underlyingType == typeof(nint)) return TryFormatPrimitiveDefault(rt, *(nint*)&value, destination, out charsWritten);
                if (underlyingType == typeof(nuint)) return TryFormatPrimitiveDefault(rt, *(nuint*)&value, destination, out charsWritten);
                if (underlyingType == typeof(char)) return TryFormatPrimitiveDefault(rt, *(char*)&value, destination, out charsWritten);
                if (underlyingType == typeof(float)) return TryFormatPrimitiveDefault(rt, *(float*)&value, destination, out charsWritten);
                if (underlyingType == typeof(double)) return TryFormatPrimitiveDefault(rt, *(double*)&value, destination, out charsWritten);
                if (underlyingType == typeof(bool)) return TryFormatBool(rt, *(bool*)&value, destination, out charsWritten, format: default);
            }
            else
            {
                if (underlyingType == typeof(int)) return TryFormatPrimitiveNonDefault(rt, *(int*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(uint)) return TryFormatPrimitiveNonDefault(rt, *(uint*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(long)) return TryFormatPrimitiveNonDefault(rt, *(long*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(ulong)) return TryFormatPrimitiveNonDefault(rt, *(ulong*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(byte)) return TryFormatPrimitiveNonDefault(rt, *(byte*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(sbyte)) return TryFormatPrimitiveNonDefault(rt, *(sbyte*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(short)) return TryFormatPrimitiveNonDefault(rt, *(short*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(ushort)) return TryFormatPrimitiveNonDefault(rt, *(ushort*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(nint)) return TryFormatPrimitiveNonDefault(rt, *(nint*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(nuint)) return TryFormatPrimitiveNonDefault(rt, *(nuint*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(char)) return TryFormatPrimitiveNonDefault(rt, *(char*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(float)) return TryFormatPrimitiveNonDefault(rt, *(float*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(double)) return TryFormatPrimitiveNonDefault(rt, *(double*)&value, destination, out charsWritten, format);
                if (underlyingType == typeof(bool)) return TryFormatBool(rt, *(bool*)&value, destination, out charsWritten, format);
            }

            throw CreateUnknownEnumTypeException();
        }

        /// <summary>Core implementation for  <see cref="TryFormat"/> when no format specifier was provided.</summary>
        private static bool TryFormatPrimitiveDefault<TUnderlyingValue>(RuntimeType enumType, TUnderlyingValue value, Span<char> destination, out int charsWritten)
            where TUnderlyingValue : struct, INumber<TUnderlyingValue>, IBitwiseOperators<TUnderlyingValue, TUnderlyingValue, TUnderlyingValue>, IMinMaxValue<TUnderlyingValue>
        {
            EnumInfo<TUnderlyingValue> enumInfo = GetEnumInfo<TUnderlyingValue>(enumType);

            if (!enumInfo.HasFlagsAttribute)
            {
                if (GetNameInlined(enumInfo, value) is string enumName)
                {
                    if (enumName.TryCopyTo(destination))
                    {
                        charsWritten = enumName.Length;
                        return true;
                    }

                    charsWritten = 0;
                    return false;
                }
            }
            else
            {
                bool destinationIsTooSmall = false;
                if (TryFormatFlagNames(enumInfo, value, destination, out charsWritten, ref destinationIsTooSmall) || destinationIsTooSmall)
                {
                    return !destinationIsTooSmall;
                }
            }

            return value.TryFormat(destination, out charsWritten, format: default, provider: null);
        }

        /// <summary>Core implementation for  <see cref="TryFormat"/> when a format specifier was provided.</summary>
        private static bool TryFormatPrimitiveNonDefault<TUnderlyingValue>(RuntimeType enumType, TUnderlyingValue value, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format)
            where TUnderlyingValue : struct, INumber<TUnderlyingValue>, IBitwiseOperators<TUnderlyingValue, TUnderlyingValue, TUnderlyingValue>, IMinMaxValue<TUnderlyingValue>
        {
            Debug.Assert(!format.IsEmpty);

            if (format.Length == 1)
            {
                switch (format[0] | 0x20)
                {
                    case 'g':
                        return TryFormatPrimitiveDefault(enumType, value, destination, out charsWritten);

                    case 'd':
                        return value.TryFormat(destination, out charsWritten, format: default, provider: null);

                    case 'x':
                        return TryFormatNumberAsHex<TUnderlyingValue>(ref Unsafe.As<TUnderlyingValue, byte>(ref value), destination, out charsWritten);

                    case 'f':
                        bool destinationIsTooSmall = false;
                        if (TryFormatFlagNames(GetEnumInfo<TUnderlyingValue>(enumType), value, destination, out charsWritten, ref destinationIsTooSmall) ||
                            destinationIsTooSmall)
                        {
                            return !destinationIsTooSmall;
                        }
                        goto case 'd';
                }
            }

            throw CreateInvalidFormatSpecifierException();
        }

        /// <summary>Tries to create a string representation of an enum as either a single constant name or multiple delimited constant names.</summary>
        /// <returns>The formatted string if the value could be fully represented by enum constants, or else null.</returns>
        private static string? FormatFlagNames<TUnderlyingValue>(EnumInfo<TUnderlyingValue> enumInfo, TUnderlyingValue resultValue)
            where TUnderlyingValue : struct, INumber<TUnderlyingValue>, IBitwiseOperators<TUnderlyingValue, TUnderlyingValue, TUnderlyingValue>
        {
            string[] names = enumInfo.Names;
            TUnderlyingValue[] values = enumInfo.Values;
            Debug.Assert(names.Length == values.Length);

            string? result = GetSingleFlagsEnumNameForValue(resultValue, names, values, out int index);
            if (result is null)
            {
                // With a ulong result value, regardless of the enum's base type, the maximum
                // possible number of consistent name/values we could have is 64, since every
                // value is made up of one or more bits, and when we see values and incorporate
                // their names, we effectively switch off those bits.
                Span<int> foundItems = stackalloc int[64];
                if (TryFindFlagsNames(resultValue, names, values, index, foundItems, out int resultLength, out int foundItemsCount))
                {
                    foundItems = foundItems.Slice(0, foundItemsCount);
                    int length = GetMultipleEnumsFlagsFormatResultLength(resultLength, foundItemsCount);

                    result = string.FastAllocateString(length);
                    WriteMultipleFoundFlagsNames(names, foundItems, new Span<char>(ref result.GetRawStringData(), result.Length));
                }
            }

            return result;
        }

        /// <summary>Tries to format into a span a representation of an enum as either a single constant name or multiple delimited constant names.</summary>
        /// <returns>
        /// true if the value could be fully represented by enum constants and if the formatted value could fit into the destination span; otherwise, false.
        /// If false, <paramref name="isDestinationTooSmall"/> is used to disambiguate the reason for the failure.
        /// </returns>
        private static bool TryFormatFlagNames<TUnderlyingValue>(EnumInfo<TUnderlyingValue> enumInfo, TUnderlyingValue resultValue, Span<char> destination, out int charsWritten, ref bool isDestinationTooSmall)
            where TUnderlyingValue : struct, INumber<TUnderlyingValue>, IBitwiseOperators<TUnderlyingValue, TUnderlyingValue, TUnderlyingValue>
        {
            Debug.Assert(!isDestinationTooSmall);

            string[] names = enumInfo.Names;
            TUnderlyingValue[] values = enumInfo.Values;
            Debug.Assert(names.Length == values.Length);

            if (GetSingleFlagsEnumNameForValue(resultValue, names, values, out int index) is string singleEnumFlagsFormat)
            {
                if (singleEnumFlagsFormat.TryCopyTo(destination))
                {
                    charsWritten = singleEnumFlagsFormat.Length;
                    return true;
                }

                isDestinationTooSmall = true;
            }
            else
            {
                // With a ulong result value, regardless of the enum's base type, the maximum
                // possible number of consistent name/values we could have is 64, since every
                // value is made up of one or more bits, and when we see values and incorporate
                // their names, we effectively switch off those bits.
                Span<int> foundItems = stackalloc int[64];
                if (TryFindFlagsNames(resultValue, names, values, index, foundItems, out int resultLength, out int foundItemsCount))
                {
                    foundItems = foundItems.Slice(0, foundItemsCount);
                    int length = GetMultipleEnumsFlagsFormatResultLength(resultLength, foundItemsCount);

                    if (length <= destination.Length)
                    {
                        charsWritten = length;
                        WriteMultipleFoundFlagsNames(names, foundItems, destination);
                        return true;
                    }

                    isDestinationTooSmall = true;
                }
            }

            charsWritten = 0;
            return false;
        }

        /// <summary>
        /// Calculates how many characters will be in a formatted value, where there are <paramref name="foundItemsCount"/>
        /// names whose lengths all sum to <paramref name="resultLength"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // used twice, once from string-based and once from span-based code path
        private static int GetMultipleEnumsFlagsFormatResultLength(int resultLength, int foundItemsCount)
        {
            Debug.Assert(foundItemsCount >= 2 && foundItemsCount <= 64, $"{nameof(foundItemsCount)} == {foundItemsCount}");

            const int SeparatorStringLength = 2; // ", "
            int allSeparatorsLength = SeparatorStringLength * (foundItemsCount - 1); // this can't overflow
            return checked(resultLength + allSeparatorsLength);
        }

        /// <summary>Tries to find the single named constant for the specified value, or else the index where we left off searching after not finding it.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // used twice, once from string-based and once from span-based code path
        private static string? GetSingleFlagsEnumNameForValue<TUnderlyingValue>(TUnderlyingValue resultValue, string[] names, TUnderlyingValue[] values, out int index)
            where TUnderlyingValue : struct, INumber<TUnderlyingValue>
        {
            // Values are sorted, so if the incoming value is 0, we can check to see whether
            // the first entry matches it, in which case we can return its name; otherwise,
            // we can just return "0".
            if (resultValue == TUnderlyingValue.Zero)
            {
                index = 0;
                return values.Length > 0 && values[0] == TUnderlyingValue.Zero ?
                    names[0] :
                    "0";
            }

            // Walk from largest to smallest. It's common to have a flags enum with a single
            // value that matches a single entry, in which case we can just return the existing
            // name string.
            int i;
            for (i = values.Length - 1; (uint)i < (uint)values.Length; i--)
            {
                if (values[i] <= resultValue)
                {
                    if (values[i] == resultValue)
                    {
                        index = i;
                        return names[i];
                    }

                    break;
                }
            }

            index = i;
            return null;
        }

        /// <summary>Tries to compute the indices of all named constants that or together to equal the specified value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // used twice, once from string-based and once from span-based code path
        private static bool TryFindFlagsNames<TUnderlyingValue>(TUnderlyingValue resultValue, string[] names, TUnderlyingValue[] values, int index, Span<int> foundItems, out int resultLength, out int foundItemsCount)
            where TUnderlyingValue : struct, INumber<TUnderlyingValue>, IBitwiseOperators<TUnderlyingValue, TUnderlyingValue, TUnderlyingValue>
        {
            // Now look for multiple matches, storing the indices of the values
            // into our span.
            resultLength = 0;
            foundItemsCount = 0;

            while (true)
            {
                if ((uint)index >= (uint)values.Length)
                {
                    break;
                }

                TUnderlyingValue currentValue = values[index];
                if (index == 0 && currentValue == TUnderlyingValue.Zero)
                {
                    break;
                }

                if ((resultValue & currentValue) == currentValue)
                {
                    resultValue &= ~currentValue;
                    foundItems[foundItemsCount] = index;
                    foundItemsCount++;
                    resultLength = checked(resultLength + names[index].Length);
                    if (resultValue == TUnderlyingValue.Zero)
                    {
                        break;
                    }
                }

                index--;
            }

            // If we exhausted looking through all the values and we still have
            // a non-zero result, we couldn't match the result to only named values.
            // In that case, we return null and let the call site just generate
            // a string for the integral value if it desires.
            return resultValue == TUnderlyingValue.Zero;
        }

        /// <summary>Concatenates the names of the found items into the destination span.</summary>
        /// <remarks>The destination must have already been verified long enough to store the resulting data.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // used twice, once from string-based and once from span-based code path
        private static void WriteMultipleFoundFlagsNames(string[] names, ReadOnlySpan<int> foundItems, Span<char> destination)
        {
            Debug.Assert(foundItems.Length >= 2, $"{nameof(foundItems)} == {foundItems.Length}");

            for (int i = foundItems.Length - 1; i != 0; i--)
            {
                string name = names[foundItems[i]];
                name.CopyTo(destination);
                destination = destination.Slice(name.Length);
                Span<char> afterSeparator = destination.Slice(2); // done before copying ", " to eliminate those two bounds checks
                destination[0] = EnumSeparatorChar;
                destination[1] = ' ';
                destination = afterSeparator;
            }

            names[foundItems[0]].CopyTo(destination);
        }

        private static RuntimeType ValidateRuntimeType(Type enumType)
        {
            ArgumentNullException.ThrowIfNull(enumType);

            RuntimeType? rt = enumType as RuntimeType;
            if (rt is null || !rt.IsActualEnum)
            {
                ThrowInvalidRuntimeType(enumType);
            }

#if NATIVEAOT
            // Check for the unfortunate "typeof(Outer<>.InnerEnum)" corner case.
            // https://github.com/dotnet/runtime/issues/7976
            if (rt.ContainsGenericParameters)
                throw new InvalidOperationException(SR.Format(SR.Arg_OpenType, rt.ToString()));
#endif

            return rt;
        }

        [DoesNotReturn]
        private static void ThrowInvalidRuntimeType(Type enumType) =>
            throw (enumType is not RuntimeType ?
                new ArgumentException(SR.Arg_MustBeType, nameof(enumType)) :
                new ArgumentException(SR.Arg_MustBeEnum, nameof(enumType)));

        private static void ThrowInvalidEmptyParseArgument() =>
            throw new ArgumentException(SR.Arg_MustContainEnumInfo, "value");

        [MethodImpl(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/78300
        private static Exception CreateInvalidFormatSpecifierException() =>
            new FormatException(SR.Format_InvalidEnumFormatSpecification);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception CreateUnknownEnumTypeException() =>
            new InvalidOperationException(SR.InvalidOperation_UnknownEnumType);

        public TypeCode GetTypeCode() =>
            InternalGetCorElementType() switch
            {
                CorElementType.ELEMENT_TYPE_I1 => TypeCode.SByte,
                CorElementType.ELEMENT_TYPE_U1 => TypeCode.Byte,
                CorElementType.ELEMENT_TYPE_I2 => TypeCode.Int16,
                CorElementType.ELEMENT_TYPE_U2 => TypeCode.UInt16,
                CorElementType.ELEMENT_TYPE_I4 => TypeCode.Int32,
                CorElementType.ELEMENT_TYPE_U4 => TypeCode.UInt32,
                CorElementType.ELEMENT_TYPE_I8 => TypeCode.Int64,
                CorElementType.ELEMENT_TYPE_U8 => TypeCode.UInt64,
                CorElementType.ELEMENT_TYPE_CHAR => TypeCode.Char,
                CorElementType.ELEMENT_TYPE_BOOLEAN => TypeCode.Boolean,
                // There's no TypeCode for nint or nuint, and our VB support (or at least
                // tests) needs to be updated in order to include float/double here.
                _ => throw CreateUnknownEnumTypeException(),
            };

        bool IConvertible.ToBoolean(IFormatProvider? provider) => Convert.ToBoolean(GetValue());
        char IConvertible.ToChar(IFormatProvider? provider) => Convert.ToChar(GetValue());
        sbyte IConvertible.ToSByte(IFormatProvider? provider) => Convert.ToSByte(GetValue());
        byte IConvertible.ToByte(IFormatProvider? provider) => Convert.ToByte(GetValue());
        short IConvertible.ToInt16(IFormatProvider? provider) => Convert.ToInt16(GetValue());
        ushort IConvertible.ToUInt16(IFormatProvider? provider) => Convert.ToUInt16(GetValue());
        int IConvertible.ToInt32(IFormatProvider? provider) => Convert.ToInt32(GetValue());
        uint IConvertible.ToUInt32(IFormatProvider? provider) => Convert.ToUInt32(GetValue());
        long IConvertible.ToInt64(IFormatProvider? provider) => Convert.ToInt64(GetValue());
        ulong IConvertible.ToUInt64(IFormatProvider? provider) => Convert.ToUInt64(GetValue());
        float IConvertible.ToSingle(IFormatProvider? provider) => Convert.ToSingle(GetValue());
        double IConvertible.ToDouble(IFormatProvider? provider) => Convert.ToDouble(GetValue());
        decimal IConvertible.ToDecimal(IFormatProvider? provider) => Convert.ToDecimal(GetValue());
        DateTime IConvertible.ToDateTime(IFormatProvider? provider) => throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Enum", "DateTime"));
        object IConvertible.ToType(Type type, IFormatProvider? provider) => Convert.DefaultToType(this, type, provider);

        public static object ToObject(Type enumType, object value)
        {
            ArgumentNullException.ThrowIfNull(value);

            switch (Convert.GetTypeCode(value))
            {
                case TypeCode.Int32: return ToObject(enumType, (int)value);
                case TypeCode.SByte: return ToObject(enumType, (sbyte)value);
                case TypeCode.Int16: return ToObject(enumType, (short)value);
                case TypeCode.Int64: return ToObject(enumType, (long)value);
                case TypeCode.UInt32: return ToObject(enumType, (uint)value);
                case TypeCode.Byte: return ToObject(enumType, (byte)value);
                case TypeCode.UInt16: return ToObject(enumType, (ushort)value);
                case TypeCode.UInt64: return ToObject(enumType, (ulong)value);
                case TypeCode.Char: return ToObject(enumType, (char)value);
                case TypeCode.Boolean: return ToObject(enumType, (bool)value ? 1L : 0L);
                case TypeCode.Single: return ToObject(enumType, BitConverter.SingleToInt32Bits((float)value));
                case TypeCode.Double: return ToObject(enumType, BitConverter.DoubleToInt64Bits((double)value));
            };

            Type valueType = value.GetType();
            if (valueType.IsEnum)
            {
                valueType = valueType.GetEnumUnderlyingType();
            }

            if (valueType == typeof(nint)) ToObject(enumType, (nint)value);
            if (valueType == typeof(nuint)) ToObject(enumType, (nuint)value);

            throw new ArgumentException(SR.Arg_MustBeEnumBaseTypeOrEnum, nameof(value));
        }

        [CLSCompliant(false)]
        public static object ToObject(Type enumType, sbyte value) =>
            InternalBoxEnum(ValidateRuntimeType(enumType), value);

        public static object ToObject(Type enumType, short value) =>
            InternalBoxEnum(ValidateRuntimeType(enumType), value);

        public static object ToObject(Type enumType, int value) =>
            InternalBoxEnum(ValidateRuntimeType(enumType), value);

        public static object ToObject(Type enumType, byte value) =>
            InternalBoxEnum(ValidateRuntimeType(enumType), value);

        [CLSCompliant(false)]
        public static object ToObject(Type enumType, ushort value) =>
            InternalBoxEnum(ValidateRuntimeType(enumType), value);

        [CLSCompliant(false)]
        public static object ToObject(Type enumType, uint value) =>
            InternalBoxEnum(ValidateRuntimeType(enumType), value);

        public static object ToObject(Type enumType, long value) =>
            InternalBoxEnum(ValidateRuntimeType(enumType), value);

        [CLSCompliant(false)]
        public static object ToObject(Type enumType, ulong value) =>
            InternalBoxEnum(ValidateRuntimeType(enumType), unchecked((long)value));

        internal static bool AreSequentialFromZero<TUnderlyingValue>(TUnderlyingValue[] values) where TUnderlyingValue : struct, INumber<TUnderlyingValue>
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (long.CreateTruncating(values[i]) != i)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
