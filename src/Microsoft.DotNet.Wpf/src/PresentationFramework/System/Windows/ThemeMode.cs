using System;

public readonly struct ThemeMode : IEquatable<ThemeMode>
{
    public static ThemeMode None => new ThemeMode();
    public static ThemeMode Light => new ThemeMode();
    public static ThemeMode Dark { get; }
    public static ThemeMode System { get; }


    public string Value => _value ?? "None";

    public ThemeMode(string value) => _value = value;
    
    public bool Equals(ThemeMode other)
    {
        return string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return obj is ThemeMode other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _value != null ? StringComparer.Ordinal.GetHashCode(_value) : 0;
    }

    public static bool operator ==(ThemeMode left, ThemeMode right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ThemeMode left, ThemeMode right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return Value;
    }

    private readonly string _value;
}