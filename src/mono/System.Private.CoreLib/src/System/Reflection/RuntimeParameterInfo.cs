// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

namespace System.Reflection
{
    internal sealed class RuntimeParameterInfo : ParameterInfo
    {
        internal MarshalAsAttribute? marshalAs;

        // Called by the runtime
        internal RuntimeParameterInfo(string name, Type type, int position, int attrs, object defaultValue, MemberInfo member, MarshalAsAttribute marshalAs)
        {
            NameImpl = name;
            ClassImpl = type;
            PositionImpl = position;
            AttrsImpl = (ParameterAttributes)attrs;
            DefaultValueImpl = defaultValue;
            MemberImpl = member;
            this.marshalAs = marshalAs;
        }

        internal static void FormatParameters(StringBuilder sb, ParameterInfo[] p, CallingConventions callingConvention)
        {
            for (int i = 0; i < p.Length; ++i)
            {
                if (i > 0)
                    sb.Append(", ");

                Type t = p[i].ParameterType;

                string typeName = t.FormatTypeName();

                // Legacy: Why use "ByRef" for by ref parameters? What language is this?
                // VB uses "ByRef" but it should precede (not follow) the parameter name.
                // Why don't we just use "&"?
                if (t.IsByRef)
                {
                    sb.Append(typeName.TrimEnd(new char[] { '&' }));
                    sb.Append(" ByRef");
                }
                else
                {
                    sb.Append(typeName);
                }
            }

            if ((callingConvention & CallingConventions.VarArgs) != 0)
            {
                if (p.Length > 0)
                    sb.Append(", ");
                sb.Append("...");
            }
        }

        internal RuntimeParameterInfo(ParameterBuilder? pb, Type? type, MemberInfo member, int position)
        {
            this.ClassImpl = type;
            this.MemberImpl = member;
            if (pb != null)
            {
                this.NameImpl = pb.Name;
                this.PositionImpl = pb.Position - 1;    // ParameterInfo.Position is zero-based
                this.AttrsImpl = (ParameterAttributes)pb.Attributes;
            }
            else
            {
                this.NameImpl = null;
                this.PositionImpl = position - 1;
                this.AttrsImpl = ParameterAttributes.None;
            }
        }

        internal static ParameterInfo New(ParameterBuilder? pb, Type? type, MemberInfo member, int position)
        {
            return new RuntimeParameterInfo(pb, type, member, position);
        }

        /*FIXME this constructor looks very broken in the position parameter*/
        internal RuntimeParameterInfo(ParameterInfo? pinfo, Type? type, MemberInfo member, int position)
        {
            this.ClassImpl = type;
            this.MemberImpl = member;
            if (pinfo != null)
            {
                this.NameImpl = pinfo.Name;
                this.PositionImpl = pinfo.Position - 1; // ParameterInfo.Position is zero-based
                this.AttrsImpl = (ParameterAttributes)pinfo.Attributes;
            }
            else
            {
                this.NameImpl = null;
                this.PositionImpl = position - 1;
                this.AttrsImpl = ParameterAttributes.None;
            }
        }

        internal RuntimeParameterInfo(ParameterInfo pinfo, MemberInfo member)
        {
            this.ClassImpl = pinfo.ParameterType;
            this.MemberImpl = member;
            this.NameImpl = pinfo.Name;
            this.PositionImpl = pinfo.Position;
            this.AttrsImpl = pinfo.Attributes;
            this.DefaultValueImpl = GetDefaultValueImpl(pinfo);
        }

        /* to build a ParameterInfo for the return type of a method */
        internal RuntimeParameterInfo(Type type, MemberInfo member, MarshalAsAttribute marshalAs)
        {
            this.ClassImpl = type;
            this.MemberImpl = member;
            this.NameImpl = null;
            this.PositionImpl = -1; // since parameter positions are zero-based, return type pos is -1
            this.AttrsImpl = ParameterAttributes.Retval;
            this.marshalAs = marshalAs;
        }

        // ctor for no metadata MethodInfo in the DynamicMethod and RuntimeMethodInfo cases
        internal RuntimeParameterInfo(MethodInfo owner, string? name, Type parameterType, int position)
        {
            MemberImpl = owner;
            NameImpl = name;
            ClassImpl = parameterType;
            PositionImpl = position;
            AttrsImpl = ParameterAttributes.None;
        }

        private object? GetDefaultValueFromCustomAttributeData()
        {
            foreach (CustomAttributeData attributeData in RuntimeCustomAttributeData.GetCustomAttributes(this))
            {
                Type attributeType = attributeData.AttributeType;
                if (attributeType == typeof(DecimalConstantAttribute))
                {
                    return GetRawDecimalConstant(attributeData);
                }
                else if (attributeType.IsSubclassOf(typeof(CustomConstantAttribute)))
                {
                    if (attributeType == typeof(DateTimeConstantAttribute))
                    {
                        return GetRawDateTimeConstant(attributeData);
                    }
                    return GetRawConstant(attributeData);
                }
            }
            return DBNull.Value;
        }

        private static decimal GetRawDecimalConstant(CustomAttributeData attr)
        {
            System.Collections.Generic.IList<CustomAttributeTypedArgument> args = attr.ConstructorArguments;

            return new decimal(
                lo: GetConstructorArgument(args, 4),
                mid: GetConstructorArgument(args, 3),
                hi: GetConstructorArgument(args, 2),
                isNegative: ((byte)args[1].Value!) != 0,
                scale: (byte)args[0].Value!);

            static int GetConstructorArgument(IList<CustomAttributeTypedArgument> args, int index)
            {
                // The constructor is overloaded to accept both signed and unsigned arguments
                object obj = args[index].Value!;
                return (obj is int value) ? value : (int)(uint)obj;
            }
        }

        private static DateTime GetRawDateTimeConstant(CustomAttributeData attr)
        {
            return new DateTime((long)attr.ConstructorArguments[0].Value!);
        }

        // We are relying only on named arguments for historical reasons
        private static object? GetRawConstant(CustomAttributeData attr)
        {
            foreach (CustomAttributeNamedArgument namedArgument in attr.NamedArguments)
            {
                if (namedArgument.MemberInfo.Name.Equals("Value"))
                    return namedArgument.TypedValue.Value;
            }
            return DBNull.Value;
        }

        private object? GetDefaultValueFromCustomAttributes()
        {
            object[] customAttributes = GetCustomAttributes(typeof(CustomConstantAttribute), false);
            if (customAttributes.Length != 0)
                return ((CustomConstantAttribute)customAttributes[0]).Value;

            customAttributes = GetCustomAttributes(typeof(DecimalConstantAttribute), false);
            if (customAttributes.Length != 0)
                return ((DecimalConstantAttribute)customAttributes[0]).Value;

            return DBNull.Value;
        }

        private object? GetDefaultValue(bool raw)
        {
            // Prioritize metadata constant over custom attribute constant
            object? defaultValue = DefaultValueImpl;
            if (defaultValue != null && (defaultValue.GetType() == typeof(DBNull) || defaultValue.GetType() == typeof(Missing)))
            {
                // If default value is not specified in metadata, look for it in custom attributes
                // The resolution of default value is done by following these rules:
                // 1. For RawDefaultValue, we pick the first custom attribute holding the constant value
                //  in the following order: DecimalConstantAttribute, DateTimeConstantAttribute, CustomConstantAttribute
                // 2. For DefaultValue, we first look for CustomConstantAttribute and pick the first occurrence.
                //  If none is found, then we repeat the same process searching for DecimalConstantAttribute.
                // IMPORTANT: Please note that there is a subtle difference in order custom attributes are inspected for
                //  RawDefaultValue and DefaultValue.
                defaultValue = raw ? GetDefaultValueFromCustomAttributeData() : GetDefaultValueFromCustomAttributes();
            }

            if (defaultValue == DBNull.Value && IsOptional)
            {
                // If the argument is marked as optional then the default value is Missing.Value.
                defaultValue = Type.Missing;
            }

            return defaultValue;
        }

        public override object? DefaultValue { get => GetDefaultValue(false); }

        public override
        object? RawDefaultValue
        {
            get
            {
                object? defaultValue = GetDefaultValue(true);
                if (defaultValue is Enum en)
                    return en.GetValue();
                return defaultValue;
            }
        }

        public
        override
        int MetadataToken
        {
            get
            {
                if (MemberImpl is PropertyInfo prop)
                {
                    MethodInfo mi = prop.GetGetMethod(true) ?? prop.GetSetMethod(true)!;

                    return mi.GetParametersInternal()[PositionImpl].MetadataToken;
                }
                else if (MemberImpl is MethodBase)
                {
                    return GetMetadataToken();
                }
                throw new ArgumentException(SR.NoMetadataTokenAvailable);
            }
        }


        public
        override
        object[] GetCustomAttributes(bool inherit)
        {
            // It is documented that the inherit flag is ignored.
            // Attribute.GetCustomAttributes is to be used to search
            // inheritance chain.
            return CustomAttribute.GetCustomAttributes(this, false);
        }

        public
        override
        object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            // It is documented that the inherit flag is ignored.
            // Attribute.GetCustomAttributes is to be used to search
            // inheritance chain.
            return CustomAttribute.GetCustomAttributes(this, attributeType, false);
        }

        internal static object? GetDefaultValueImpl(ParameterInfo pinfo)
        {
            FieldInfo field = typeof(ParameterInfo).GetField("DefaultValueImpl", BindingFlags.Instance | BindingFlags.NonPublic)!;
            return field.GetValue(pinfo);
        }

        public
        override
        bool IsDefined(Type attributeType, bool inherit)
        {
            return CustomAttribute.IsDefined(this, attributeType, inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return RuntimeCustomAttributeData.GetCustomAttributesInternal(this);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern int GetMetadataToken();

        public override Type[] GetOptionalCustomModifiers() =>
            MemberImpl is DynamicMethod.RTDynamicMethod ? Type.EmptyTypes : GetCustomModifiers(true);

        internal object[]? GetPseudoCustomAttributes()
        {
            int count = 0;

            if (IsIn)
                count++;
            if (IsOut)
                count++;
            if (IsOptional)
                count++;
            if (marshalAs != null)
                count++;

            if (count == 0)
                return null;
            object[] attrs = new object[count];
            count = 0;

            if (IsIn)
                attrs[count++] = new InAttribute();
            if (IsOut)
                attrs[count++] = new OutAttribute();
            if (IsOptional)
                attrs[count++] = new OptionalAttribute();

            if (marshalAs != null)
            {
                attrs[count++] = (MarshalAsAttribute)marshalAs.CloneInternal();
            }

            return attrs;
        }

        internal CustomAttributeData[]? GetPseudoCustomAttributesData()
        {
            int count = 0;

            if (IsIn)
                count++;
            if (IsOut)
                count++;
            if (IsOptional)
                count++;
            if (marshalAs != null)
                count++;

            if (count == 0)
                return null;
            CustomAttributeData[] attrsData = new CustomAttributeData[count];
            count = 0;

            if (IsIn)
                attrsData[count++] = new RuntimeCustomAttributeData((typeof(InAttribute)).GetConstructor(Type.EmptyTypes)!);
            if (IsOut)
                attrsData[count++] = new RuntimeCustomAttributeData((typeof(OutAttribute)).GetConstructor(Type.EmptyTypes)!);
            if (IsOptional)
                attrsData[count++] = new RuntimeCustomAttributeData((typeof(OptionalAttribute)).GetConstructor(Type.EmptyTypes)!);
            if (marshalAs != null)
            {
                var ctorArgs = new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(UnmanagedType), marshalAs.Value) };
                attrsData[count++] = new RuntimeCustomAttributeData(
                    (typeof(MarshalAsAttribute)).GetConstructor(new[] { typeof(UnmanagedType) })!,
                    ctorArgs,
                    Array.Empty<CustomAttributeNamedArgument>());//FIXME Get named params
            }

            return attrsData;
        }

        public override Type[] GetRequiredCustomModifiers() =>
            MemberImpl is DynamicMethod.RTDynamicMethod ? Type.EmptyTypes : GetCustomModifiers(false);

        public override bool HasDefaultValue
        {
            get
            {
                object? defaultValue = DefaultValue;
                if (defaultValue == null)
                    return true;

                if (defaultValue.GetType() == typeof(DBNull) || defaultValue.GetType() == typeof(Missing))
                    return false;

                return true;
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Type[] GetTypeModifiers(Type type, MemberInfo member, int position, bool optional);

        internal static ParameterInfo New(ParameterInfo pinfo, Type? type, MemberInfo member, int position)
        {
            return new RuntimeParameterInfo(pinfo, type, member, position);
        }

        internal static ParameterInfo New(ParameterInfo pinfo, MemberInfo member)
        {
            return new RuntimeParameterInfo(pinfo, member);
        }

        internal static ParameterInfo New(Type type, MemberInfo member, MarshalAsAttribute marshalAs)
        {
            return new RuntimeParameterInfo(type, member, marshalAs);
        }

        internal void SetName(string? name)
        {
            NameImpl = name;
        }

        internal void SetAttributes(ParameterAttributes attributes)
        {
            AttrsImpl = attributes;
        }

        private Type[] GetCustomModifiers(bool optional) => GetTypeModifiers(ParameterType, Member, Position, optional) ?? Type.EmptyTypes;
    }
}
