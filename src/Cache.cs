namespace Hydro;

/// <summary>
/// Cached value provider
/// </summary>
/// <typeparam name="T">Type of cached value</typeparam>
public class Cache<T>
{
    /// <summary>
    /// Instantiates Cache
    /// </summary>
    /// <param name="value">Value to store</param>
    public Cache(T value)
    {
        Value = value;
        IsSet = true;
    }

    /// <summary>
    /// Value
    /// </summary>
    public T Value { get; }
    
    
    /// <summary>
    /// Is value set
    /// </summary>
    public bool IsSet { get; private set; }

    /// <summary>
    /// Reset value
    /// </summary>
    public void Reset()
    {
        IsSet = false;
    }
}