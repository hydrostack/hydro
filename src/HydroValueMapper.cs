namespace Hydro;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public class HydroValueMapper<T> : IHydroValueMapper
{
    private Func<T, Task<T>> MapperAsync { get; set; }
    private Func<T, T> Mapper { get; set; }

    /// <summary />
    public Type MappedType => typeof(T);

    /// <summary/>
    public HydroValueMapper(Func<T, Task<T>> mapperAsync) => MapperAsync = mapperAsync;

    /// <summary/>
    public HydroValueMapper(Func<T, T> mapper) => Mapper = mapper;

    /// <summary />
    public async Task<object> Map(object value) =>
        MapperAsync != null
            ? await MapperAsync((T)value)
            : Mapper((T)value);
}

/// <summary/>
public interface IHydroValueMapper
{
    /// <summary />
    Task<object> Map(object value);

    /// <summary />
    Type MappedType { get; }
}