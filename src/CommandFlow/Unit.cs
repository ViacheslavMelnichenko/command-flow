namespace CommandFlow;

/// <summary>
/// Represents a void return type for commands that don't produce a result.
/// </summary>
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>
{
    /// <summary>
    /// The single value of the Unit type.
    /// </summary>
    public static readonly Unit Value = new();

    /// <summary>
    /// A completed task returning <see cref="Value"/>.
    /// </summary>
    public static readonly Task<Unit> Task = System.Threading.Tasks.Task.FromResult(Value);

    /// <inheritdoc />
    public int CompareTo(Unit other) => 0;

    /// <inheritdoc />
    public bool Equals(Unit other) => true;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Unit;

    /// <inheritdoc />
    public override int GetHashCode() => 0;

    /// <inheritdoc />
    public override string ToString() => "()";

    /// <summary>Equality operator. Always returns true.</summary>
    public static bool operator ==(Unit left, Unit right) => true;

    /// <summary>Inequality operator. Always returns false.</summary>
    public static bool operator !=(Unit left, Unit right) => false;
}
