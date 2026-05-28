using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using JetBrains.Annotations;

namespace WorldXaml.UI.Base;

public interface IValueConverter
{
    object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture);
    object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture);
}

file class DelegateTypeConverter<TFrom, TTo>(Func<TFrom, CultureInfo?, TTo> convert, Func<TTo, CultureInfo?, TFrom> convertBack) : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(TFrom) || base.CanConvertFrom(context, sourceType);
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(TTo) || base.CanConvertTo(context, destinationType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is TFrom from)
            return convert(from, culture);
        return base.ConvertFrom(context, culture, value);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (value is TTo to && destinationType == typeof(TFrom))
            return convertBack(to, culture);
        return base.ConvertTo(context, culture, value, destinationType);
    }
}

public static class TypeConverterRegistry
{
    internal static readonly Dictionary<Type, TypeConverter> Converters = new()
    {
        [typeof(int)] = new DelegateTypeConverter<int, string>((v, culture) => v.ToString(culture), int.Parse),
        [typeof(long)] = new DelegateTypeConverter<long, string>((v, culture) => v.ToString(culture), long.Parse),
        [typeof(float)] = new DelegateTypeConverter<float, string>((v, culture) => v.ToString(culture), float.Parse),
        [typeof(double)] = new DelegateTypeConverter<double, string>((v, culture) => v.ToString(culture), double.Parse),
        [typeof(decimal)] = new DelegateTypeConverter<decimal, string>((v, culture) => v.ToString(culture), decimal.Parse),
        [typeof(bool)] = new DelegateTypeConverter<bool, string>((v, culture) => v.ToString(), (s, culture) => bool.Parse(s)),
        [typeof(char)] = new DelegateTypeConverter<char, string>((v, culture) => v.ToString(), (s, culture) => s[0]),
        [typeof(byte)] = new DelegateTypeConverter<byte, string>((v, culture) => v.ToString(culture), byte.Parse),
        [typeof(sbyte)] = new DelegateTypeConverter<sbyte, string>((v, culture) => v.ToString(culture), sbyte.Parse),
        [typeof(short)] = new DelegateTypeConverter<short, string>((v, culture) => v.ToString(culture), short.Parse),
        [typeof(ushort)] = new DelegateTypeConverter<ushort, string>((v, culture) => v.ToString(culture), ushort.Parse),
        [typeof(uint)] = new DelegateTypeConverter<uint, string>((v, culture) => v.ToString(culture), uint.Parse),
        [typeof(ulong)] = new DelegateTypeConverter<ulong, string>((v, culture) => v.ToString(culture), ulong.Parse),
        [typeof(Guid)] = new DelegateTypeConverter<Guid, string>((v, culture) => v.ToString(), (s, culture) => Guid.Parse(s)),
        [typeof(TimeSpan)] = new DelegateTypeConverter<TimeSpan, string>((v, culture) => v.ToString(), TimeSpan.Parse),
        [typeof(DateTime)] = new DelegateTypeConverter<DateTime, string>((v, culture) => v.ToString(culture), DateTime.Parse),
        [typeof(int?)] = new DelegateTypeConverter<int?, string>((v, culture) => v?.ToString(culture) ?? "", (s, culture) => int.Parse(s, culture)),
        [typeof(long?)] = new DelegateTypeConverter<long?, string>((v, culture) => v?.ToString(culture) ?? "", (s, culture) => long.Parse(s, culture)),
        [typeof(float?)] = new DelegateTypeConverter<float?, string>((v, culture) => v?.ToString(culture) ?? "", (s, culture) => float.Parse(s, culture)),
        [typeof(double?)] = new DelegateTypeConverter<double?, string>((v, culture) => v?.ToString(culture) ?? "", (s, culture) => double.Parse(s, culture)),
        [typeof(decimal?)] = new DelegateTypeConverter<decimal?, string>((v, culture) => v?.ToString(culture) ?? "", (s, culture) => decimal.Parse(s, culture)),
        [typeof(bool?)] = new DelegateTypeConverter<bool?, string>((v, culture) => v?.ToString() ?? "", (s, culture) => bool.Parse(s)),
        [typeof(char?)] = new DelegateTypeConverter<char?, string>((v, culture) => v?.ToString() ?? "", (s, culture) => s[0]),
        [typeof(byte?)] = new DelegateTypeConverter<byte?, string>((v, culture) => v?.ToString(culture) ?? "", (s, culture) => byte.Parse(s, culture)),
        [typeof(sbyte?)] = new DelegateTypeConverter<sbyte?, string>((v, culture) => v?.ToString(culture) ?? "", (s, culture) => sbyte.Parse(s, culture)),
        [typeof(short?)] = new DelegateTypeConverter<short?, string>((v, culture) => v?.ToString(culture) ?? "", (s, culture) => short.Parse(s, culture)),
        [typeof(ushort?)] = new DelegateTypeConverter<ushort?, string>((v, culture) => v?.ToString(culture) ?? "", (s, culture) => ushort.Parse(s, culture)),
        [typeof(uint?)] = new DelegateTypeConverter<uint?, string>((v, culture) => v?.ToString(culture) ?? "", (s, culture) => uint.Parse(s, culture)),
        [typeof(ulong?)] = new DelegateTypeConverter<ulong?, string>((v, culture) => v?.ToString(culture) ?? "", (s, culture) => ulong.Parse(s, culture)),
        [typeof(Guid?)] = new DelegateTypeConverter<Guid?, string>((v, culture) => v?.ToString() ?? "", (s, culture) => Guid.Parse(s)),
        [typeof(TimeSpan?)] = new DelegateTypeConverter<TimeSpan?, string>((v, culture) => v?.ToString() ?? "", (s, culture) => TimeSpan.Parse(s, culture)),
        [typeof(DateTime?)] = new DelegateTypeConverter<DateTime?, string>((v, culture) => v?.ToString(culture) ?? "", (s, culture) => DateTime.Parse(s, culture)),
    };
    
    [UsedImplicitly]
    public static void RegisterConverter<T>(TypeConverter converter)
    {
        Converters[typeof(T)] = converter;
    }
}

/// <summary>
/// Tries every available strategy to coerce a value to the target type.
/// </summary>
public static class TypeCoercer
{
    private static readonly Type[] InbuiltTypes =
    [
        typeof(bool),    typeof(char),    typeof(sbyte),  typeof(byte),
        typeof(short),   typeof(ushort),  typeof(int),    typeof(uint),
        typeof(long),    typeof(ulong),   typeof(float),  typeof(double),
        typeof(decimal), typeof(DateTime),typeof(string)
    ];

    private static ReadOnlySpan<int> ConversionBitmasks =>
    [
        0b101111111111101, // bool
        0b100001111111110, // char
        0b101111111111111, // sbyte
        0b101111111111111, // byte
        0b101111111111111, // short
        0b101111111111111, // ushort
        0b101111111111111, // int
        0b101111111111111, // uint
        0b101111111111111, // long
        0b101111111111111, // ulong
        0b101111111111101, // float
        0b101111111111101, // double
        0b101111111111101, // decimal
        0b110000000000000, // DateTime
        0b111111111111111  // string
    ];
    
    public static bool TryCoerce(object? value, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.AllConstructors |
                                                                            DynamicallyAccessedMemberTypes.AllEvents |
                                                                            DynamicallyAccessedMemberTypes.AllFields |
                                                                            DynamicallyAccessedMemberTypes.AllMethods |
                                                                            DynamicallyAccessedMemberTypes.AllNestedTypes |
                                                                            DynamicallyAccessedMemberTypes.AllProperties |
                                                                            DynamicallyAccessedMemberTypes.Interfaces)] Type targetType, out object? result)
    {
        result = null;

        // null → nullable/reference types
        if (value is null)
        {
            result = null;
            return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) is not null;
        }

        var sourceType = value.GetType();

        // Already the right type.
        if (targetType.IsAssignableFrom(sourceType))
        {
            result = value;
            return true;
        }

        // Unwrap Nullable<T> target.
        var underlyingTarget = Nullable.GetUnderlyingType(targetType) ?? targetType;
        
        // Enum from its exact underlying type
        if (underlyingTarget.IsEnum && underlyingTarget.GetEnumUnderlyingType() == value.GetType())
        {
            result = Enum.ToObject(underlyingTarget, value);
            return true;
        }
        
        // Enum from string
        if (underlyingTarget.IsEnum && value is string s)
        {
            if (Enum.TryParse(underlyingTarget, s, ignoreCase: true, out var enumVal))
            {
                result = enumVal;
                return true;
            }
            return false;
        }
        
        // Numeric conversion bitmask
        var fromIdx = Array.IndexOf(InbuiltTypes, value.GetType());
        var toIdx   = Array.IndexOf(InbuiltTypes, underlyingTarget);
        if (fromIdx != -1 && toIdx != -1 && (ConversionBitmasks[fromIdx] & (1 << toIdx)) != 0)
        {
            try
            {
                result = Convert.ChangeType(value, underlyingTarget, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                Logging.Debug($"Failed to convert {value} to {underlyingTarget} via numeric conversion is supposed to always work.");
                result = null;
                return false;
            }
        }

        // IConvertible (covers all numeric↔numeric, numeric↔string, etc.)
        if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(underlyingTarget))
        {
            try
            {
                result = Convert.ChangeType(value, underlyingTarget, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                Logging.Debug($"Failed to convert {value} to {underlyingTarget} via IConvertible.");
            }
        }
        
        // Static (AOT-safe)
        
        // String -> anything via TypeConverter on the target type.
        if (value is string str)
        {
            if (TypeConverterRegistry.Converters.TryGetValue(underlyingTarget, out var converter) && converter.CanConvertFrom(typeof(string)))
            {
                try
                {
                    result = converter.ConvertFromInvariantString(str);
                    return true;
                }
                catch
                {
                    Logging.Debug($"Failed to convert {value} to {underlyingTarget} via TypeConverterRegistry ConvertFrom.");
                }
            }
        }
        
        // Anything → string via TypeConverter on the source type.
        if (underlyingTarget == typeof(string))
        {
            if (TypeConverterRegistry.Converters.TryGetValue(sourceType, out var converter) && converter.CanConvertTo(typeof(string)))
            {
                try
                {
                    result = converter.ConvertToInvariantString(value);
                    return true;
                }
                catch
                {
                    Logging.Debug($"Failed to convert {value} to {underlyingTarget} via ConvertTo.");
                }
            }
        }

        // TypeConverter on source type converting to target.
        {
            if (TypeConverterRegistry.Converters.TryGetValue(sourceType, out var converter) && converter.CanConvertTo(underlyingTarget))
            {
                try
                {
                    result = converter.ConvertTo(value, underlyingTarget);
                    return true;
                }
                catch
                {
                    Logging.Debug($"Failed to convert {value} to {underlyingTarget} via ConvertTo.");
                }
            }
        }

        // TypeConverter on target type converting from source.
        {
            if (TypeConverterRegistry.Converters.TryGetValue(underlyingTarget, out var converter) && converter.CanConvertFrom(sourceType))
            {
                try
                {
                    result = converter.ConvertFrom(value);
                    return true;
                }
                catch
                {
                    Logging.Debug($"Failed to convert {value} to {underlyingTarget} via ConvertFrom.");
                }
            }
        }
        // Reflection-based
        
        // String -> anything via TypeConverter on the target type.
        if (value is string str1)
        {
            var converter = TypeDescriptor.GetConverter(underlyingTarget);
            if (converter.CanConvertFrom(typeof(string)))
            {
                try
                {
                    result = converter.ConvertFromInvariantString(str1);
                    return true;
                }
                catch
                {
                    Logging.Debug($"Failed to convert {value} to {underlyingTarget} via ConvertFrom.");
                }
            }
        }

        // Anything → string via TypeConverter on the source type.
        if (underlyingTarget == typeof(string))
        {
            var converter = TypeDescriptor.GetConverter(sourceType);
            if (converter.CanConvertTo(typeof(string)))
            {
                try
                {
                    result = converter.ConvertToInvariantString(value);
                    return true;
                }
                catch
                {
                    Logging.Debug($"Failed to convert {value} to {underlyingTarget} via ConvertTo.");
                }
            }
        }

        // TypeConverter on source type converting to target.
        {
            var converter = TypeDescriptor.GetConverter(sourceType);
            if (converter.CanConvertTo(underlyingTarget))
            {
                try
                {
                    result = converter.ConvertTo(value, underlyingTarget);
                    return true;
                }
                catch
                {
                    Logging.Debug($"Failed to convert {value} to {underlyingTarget} via ConvertTo.");
                }
            }
        }

        // TypeConverter on target type converting from source.
        {
            var converter = TypeDescriptor.GetConverter(underlyingTarget);
            if (converter.CanConvertFrom(sourceType))
            {
                try
                {
                    result = converter.ConvertFrom(value);
                    return true;
                }
                catch
                {
                    Logging.Debug($"Failed to convert {value} to {underlyingTarget} via ConvertFrom.");
                }
            }
        }

        return false;
    }

    public static TTarget? Coerce<TTarget>(object? value, IValueConverter? converter = null,
        object? converterParameter = null)
    {
        // User-supplied converter takes priority.
        if (converter is not null)
        {
            var converted = converter.Convert(value, typeof(TTarget), converterParameter,
                CultureInfo.CurrentUICulture);
            return converted is TTarget t ? t : default;
        }

        if (value is TTarget direct)
            return direct;

        if (TryCoerce(value, typeof(TTarget), out var result))
            return result is TTarget r ? r : default;

        return default;
    }

    public static object? CoerceBack(object? value, Type targetType, IValueConverter? converter = null,
        object? converterParameter = null)
    {
        if (converter is not null)
            return converter.ConvertBack(value, targetType, converterParameter,
                CultureInfo.CurrentUICulture);

        if (TryCoerce(value, targetType, out var result))
            return result;

        return null;
    }
}