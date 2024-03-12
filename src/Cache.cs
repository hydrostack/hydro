namespace Hydro;

/// <summary>
/// Cached value provider
/// </summary>
/// <typeparam name="T">Type of cached value</typeparam>
public class Cache<T>
{
    private T _value;
    private readonly Func<T> _valueFunc;

    /// <summary>
    /// Value
    /// </summary>
    public T Value => IsSet
        ? _value
        : _value = _valueFunc();
    
    /// <summary>
    /// Is value set
    /// </summary>
    public bool IsSet { get; private set; }
    
    /// <summary>
    /// Instantiates Cache
    /// </summary>
    public Cache(Func<T> func)
    {
        _valueFunc = func;
    }

    /// <summary>
    /// Reset value
    /// </summary>
    public void Reset()
    {
        IsSet = false;
    }
}