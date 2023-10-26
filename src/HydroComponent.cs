using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Int32Converter = Hydro.Utils.Int32Converter;

namespace Hydro;

/// <summary>
/// Stateful and reactive view component
/// </summary>
public abstract class HydroComponent : ViewComponent
{
    private string _id;

    private readonly ConcurrentDictionary<CacheKey, object> _requestCache = new();
    private static readonly ConcurrentDictionary<CacheKey, object> PersistentCache = new();

    private readonly List<HydroComponentEvent> _dispatchEvents = new();
    private readonly HashSet<HydroEventSubscription> _subscriptions = new();

    private static readonly MethodInfo InvokeActionMethod = typeof(HydroComponent).GetMethod(nameof(InvokeAction), BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly MethodInfo InvokeActionAsyncMethod = typeof(HydroComponent).GetMethod(nameof(InvokeActionAsync), BindingFlags.Static | BindingFlags.NonPublic);

    private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
    {
        Converters = new JsonConverter[] { new Int32Converter() }.ToList()
    };

    private static readonly ConcurrentDictionary<Type, IHydroAuthorizationFilter[]> ComponentAuthorizationAttributes = new();

    /// <summary>
    /// Provides indication if ModelState is valid
    /// </summary>
    protected bool IsValid { get; private set; }

    /// <summary>
    /// Provides component's key value
    /// </summary>
    protected string Key { get; private set; }

    /// <summary>
    /// Provides list of already accessed component's properties  
    /// </summary>
    public HashSet<string> TouchedProperties { get; set; } = new();

    /// <summary>
    /// Determines if the whole model was accessed already
    /// </summary>
    public bool IsModelTouched { get; set; }

    /// <summary>
    /// Implementation of ViewComponent's InvokeAsync method
    /// </summary>
    /// <param name="parameters">Parameters</param>
    /// <param name="key">Key</param>
    public async Task<IHtmlContent> InvokeAsync(object parameters = null, string key = null)
    {
        ApplyParameters(parameters);

        Key = key;

        var persistentState = HttpContext.RequestServices.GetService<IPersistentState>();

        var componentHtml = HttpContext.IsHydro(excludeBoosted: true)
            ? await RenderOnlineComponent(persistentState)
            : await RenderStaticComponent(persistentState);

        return new HtmlString(componentHtml);
    }

    /// <summary>
    /// Subscribes to a Hydro event
    /// </summary>
    /// <typeparam name="TEvent">Event type</typeparam>
    public void Subscribe<TEvent>() =>
        _subscriptions.Add(new HydroEventSubscription
        {
            EventName = GetFullTypeName(typeof(TEvent)),
            Action = (TEvent _) => { }
        });

    /// <summary>
    /// Subscribes to a Hydro event
    /// </summary>
    /// <param name="action">Action to execute when event occurs</param>
    /// <typeparam name="TEvent">Event type</typeparam>
    public void Subscribe<TEvent>(Action<TEvent> action) =>
        _subscriptions.Add(new HydroEventSubscription
        {
            EventName = GetFullTypeName(typeof(TEvent)),
            Action = action
        });

    private static string GetFullTypeName(Type type) =>
        type.DeclaringType != null
            ? type.DeclaringType.Name + "+" + type.Name
            : type.Name;

    /// <summary>
    /// Subscribes to a Hydro event
    /// </summary>
    /// <param name="action">Action to execute when event occurs</param>
    /// <typeparam name="TEvent">Event type</typeparam>
    public void Subscribe<TEvent>(Func<TEvent, Task> action) =>
        _subscriptions.Add(new HydroEventSubscription
        {
            EventName = GetFullTypeName(typeof(TEvent)),
            Action = action
        });

    /// <summary>
    /// Triggers a Hydro event
    /// </summary>
    /// <param name="data">Data to pass</param>
    /// <param name="scope">Scope of the event</param>
    /// <typeparam name="TEvent">Event type</typeparam>
    public void Dispatch<TEvent>(TEvent data, Scope scope = Scope.Parent) =>
        Dispatch(GetFullTypeName(typeof(TEvent)), data, scope);

    /// <summary>
    /// Triggers a Hydro event
    /// </summary>
    /// <param name="name">Name of the event</param>
    /// <param name="data">Data to pass</param>
    /// <param name="scope">Scope of the event</param>
    /// <typeparam name="TEvent">Event type</typeparam>
    public void Dispatch<TEvent>(string name, TEvent data, Scope scope = Scope.Parent) =>
        _dispatchEvents.Add(new HydroComponentEvent
        {
            Name = name,
            Data = data,
            Scope = scope.ToString().ToLower()
        });

    /// <summary>
    /// Triggered once the component is mounted
    /// </summary>
    public virtual Task MountAsync()
    {
        Mount();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Triggered once the component is mounted
    /// </summary>
    public virtual void Mount()
    {
    }

    /// <summary>
    /// Triggered before each render
    /// </summary>
    public virtual Task RenderAsync()
    {
        Render();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Triggered before each render
    /// </summary>
    public virtual void Render()
    {
    }

    /// <summary>
    /// Perform a redirect with page reload
    /// </summary>
    /// <param name="url">Destination URL</param>
    public void Redirect(string url) =>
        HttpContext.Response.HydroRedirect(url);

    /// <summary>
    /// Perform a redirect without page reload
    /// </summary>
    /// <param name="url">Destination URL</param>
    /// <param name="payload">Payload for the destination components</param>
    public void Location(string url, object payload = null) =>
        HttpContext.Response.HydroLocation(url, payload);

    /// <summary>
    /// Cache value
    /// </summary>
    /// <param name="func">Value producer</param>
    /// <param name="lifetime">Lifetime of the cached value</param>
    /// <typeparam name="T">Type of the value</typeparam>
    /// <returns>Produced value</returns>
    protected Cache<T> Cache<T>(Func<T> func, CacheLifetime lifetime = CacheLifetime.Request)
    {
        var cache = lifetime == CacheLifetime.Request ? _requestCache : PersistentCache;

        var cacheKey = new CacheKey(_id, func);
        if (cache.TryGetValue(cacheKey, out var dic))
        {
            var value = (Cache<T>)dic;

            if (!value.IsSet)
            {
                cache.Remove(cacheKey, out _);
            }
            else
            {
                return value;
            }
        }

        var cacheValue = new Cache<T>(func());
        cache.TryAdd(cacheKey, cacheValue);
        return cacheValue;
    }

    private async Task<string> RenderOnlineComponent(IPersistentState persistentState) =>
        DetermineRootComponent()
            ? await RenderOnlineRootComponent(persistentState)
            : await RenderOnlineNestedComponent(persistentState);

    private bool DetermineRootComponent()
    {
        if (!HttpContext.Items.TryGetValue(HydroConsts.ContextItems.IsRootRendered, out _))
        {
            HttpContext.Items.TryAdd(HydroConsts.ContextItems.IsRootRendered, true);
            return true;
        }

        return false;
    }

    private async Task<string> RenderOnlineRootComponent(IPersistentState persistentState)
    {
        var componentId = GetRootComponentId();
        _id = componentId;

        PopulateBaseModel(persistentState);
        PopulateRequestModel();
        if (!await AuthorizeAsync())
        {
            return string.Empty;
        }

        await TriggerMethod();
        await TriggerEvent();
        await RenderAsync();
        PopulateDispatchers();

        return await GenerateComponentHtml(componentId, persistentState);
    }

    private async Task<string> RenderOnlineNestedComponent(IPersistentState persistentState)
    {
        var componentId = GenerateComponentId(Key);
        _id = componentId;

        if (IsComponentIdRendered(componentId))
        {
            return GetComponentPlaceholderTemplate(componentId);
        }

        if (!await AuthorizeAsync())
        {
            return string.Empty;
        }

        await MountAsync();
        await RenderAsync();
        return await GenerateComponentHtml(componentId, persistentState);
    }

    private static string GetComponentPlaceholderTemplate(string componentId) =>
        $"<div id=\"{componentId}\" hydro hydro-placeholder></div>";

    private async Task<string> RenderStaticComponent(IPersistentState persistentState)
    {
        var componentId = GenerateComponentId(Key);
        _id = componentId;

        if (!await AuthorizeAsync())
        {
            return string.Empty;
        }

        await MountAsync();
        await RenderAsync();

        return await GenerateComponentHtml(componentId, persistentState);
    }

    private string GenerateComponentId(string key)
    {
        var parentComponentId = HttpContext.Items[HydroConsts.Component.ParentComponentId];
        var mainId = parentComponentId ?? $"{Guid.NewGuid():N}";
        var typeName = GetType().Name;

        return Hash($"{mainId}-{typeName}-{key}");
    }

    private async Task<string> GenerateComponentHtml(string componentId, IPersistentState persistentState)
    {
        HttpContext.Items[HydroConsts.Component.ParentComponentId] = componentId;

        var componentHtml = await GetComponentHtml();
        var componentHtmlDocument = new HtmlDocument();
        componentHtmlDocument.LoadHtml(componentHtml);
        var root = componentHtmlDocument.DocumentNode;

        if (root.ChildNodes.Count(n => n.NodeType == HtmlNodeType.Element) != 1)
        {
            throw new InvalidOperationException("The wire component must have only one root element.");
        }

        var rootElement = root.ChildNodes.First(n => n.NodeType == HtmlNodeType.Element);

        rootElement.SetAttributeValue("id", componentId);
        rootElement.SetAttributeValue("hydro-name", GetType().Name);
        var hydroAttribute = rootElement.SetAttributeValue("hydro", null);
        hydroAttribute.QuoteType = AttributeValueQuote.WithoutValue;

        rootElement.AppendChild(GetModelScript(componentHtmlDocument, componentId, persistentState));

        foreach (var subscription in _subscriptions)
        {
            rootElement.AppendChild(GetEventSubscriptionScript(componentHtmlDocument, subscription));
        }

        return rootElement.OuterHtml;
    }

    private void BindModel(IFormCollection formCollection)
    {
        if (!IsModelTouched)
        {
            if (HttpContext.Items.TryGetValue(HydroConsts.ContextItems.MethodName, out _))
            {
                IsModelTouched = true;
            }
            else
            {
                foreach (var item in formCollection)
                {
                    TouchedProperties.Add(item.Key);
                }
            }
        }

        foreach (var pair in formCollection)
        {
            var value = PropertyInjector.SetPropertyValue(this, pair.Key, pair.Value);
            Bind(pair.Key, value);
        }

        ValidateModel();
    }

    /// <summary>
    /// Triggered when a property is updated from the client
    /// </summary>
    /// <param name="property">Property path</param>
    /// <param name="value">New value</param>
    public virtual void Bind(string property, object value)
    {
    }

    private HtmlNode GetModelScript(HtmlDocument document, string id, IPersistentState persistentState)
    {
        var scriptNode = document.CreateElement("script");
        scriptNode.SetAttributeValue("type", "text/hydro");
        scriptNode.SetAttributeValue("data-id", id);
        var serializeDeclaredProperties = PropertyInjector.SerializeDeclaredProperties(GetType(), this);
        var model = persistentState.Protect(serializeDeclaredProperties);
        scriptNode.AppendChild(document.CreateTextNode(model));
        return scriptNode;
    }

    private HtmlNode GetEventSubscriptionScript(HtmlDocument document, HydroEventSubscription subscription)
    {
        var eventData = new
        {
            name = subscription.EventName,
            path = $"/hydro/{GetType().Name}/event".ToLower()
        };

        var scriptNode = document.CreateElement("script");
        scriptNode.SetAttributeValue("key", $"R{Guid.NewGuid():N}");
        scriptNode.SetAttributeValue("type", "text/hydro");
        scriptNode.SetAttributeValue("hydro-event", "true");
        scriptNode.SetAttributeValue("x-data", "");
        scriptNode.SetAttributeValue("x-on-hydro-event", JsonConvert.SerializeObject(eventData));
        return scriptNode;
    }

    private void PopulateDispatchers()
    {
        if (!_dispatchEvents.Any())
        {
            return;
        }

        var data = _dispatchEvents
            .Select(e => new { name = e.Name, data = e.Data, scope = e.Scope })
            .ToList();

        HttpContext.Response.Headers.TryAdd(HydroConsts.ResponseHeaders.Trigger, JsonConvert.SerializeObject(data));
    }

    private bool IsComponentIdRendered(string componentId)
    {
        var renderedComponentIds = (string[])HttpContext.Items[HydroConsts.ContextItems.RenderedComponentIds];
        return renderedComponentIds.Contains(componentId);
    }

    private async Task TriggerMethod()
    {
        if (HttpContext.Items.TryGetValue(HydroConsts.ContextItems.MethodName, out var method) && method is string methodValue && !string.IsNullOrWhiteSpace(methodValue))
        {
            var methodInfo = GetType()
                .GetMethod(methodValue, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (methodInfo != null)
            {
                var requestParameters = GetParameters();
                var methodParameters = methodInfo.GetParameters();

                if (requestParameters.Count != methodParameters.Length || requestParameters.Any(rp => !methodParameters.Any(mp => rp.Key == mp.Name)))
                {
                    throw new InvalidOperationException("Wrong action parameters");
                }

                var orderedParameters = methodParameters
                    .Select(p =>
                    {
                        var sourceType = requestParameters[p.Name].GetType();

                        return sourceType == p.ParameterType
                            ? requestParameters[p.Name]
                            : TypeDescriptor.GetConverter(p.ParameterType).ConvertFrom(requestParameters[p.Name]);
                    })
                    .ToArray();

                if (typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
                {
                    await (Task)methodInfo.Invoke(this, orderedParameters)!;
                }
                else
                {
                    methodInfo.Invoke(this, orderedParameters);
                }
            }
        }
    }

    private IDictionary<string, object> GetParameters() =>
        HttpContext.Request.Headers.TryGetValue(HydroConsts.RequestHeaders.Parameters, out var parameters)
            ? JsonConvert.DeserializeObject<Dictionary<string, object>>(parameters, JsonSerializerSettings)
            : new Dictionary<string, object>();

    /// <summary>
    /// Get the payload transferred from previous page's component
    /// </summary>
    /// <typeparam name="T">Payload type</typeparam>
    /// <returns>Payload</returns>
    public T GetPayload<T>() =>
        HttpContext.Request.Headers.TryGetValue(HydroConsts.RequestHeaders.Payload, out var payloadString)
            ? JsonConvert.DeserializeObject<T>(payloadString)
            : default;

    private async Task TriggerEvent()
    {
        if (HttpContext.Items.TryGetValue(HydroConsts.ContextItems.EventName, out var eventName) && eventName is string eventNameValue && !string.IsNullOrWhiteSpace(eventNameValue))
        {
            var subscription = _subscriptions.FirstOrDefault(s => s.EventName == eventNameValue);

            if (subscription != null)
            {
                var methodInfo = subscription.Action.Method;
                var parameters = methodInfo.GetParameters();
                var parameterType = parameters.First().ParameterType;
                var model = HttpContext.Items.TryGetValue(HydroConsts.ContextItems.EventData, out var eventModel) ? JsonConvert.DeserializeObject((string)eventModel, parameterType) : null;

                var isAsync = typeof(Task).IsAssignableFrom(methodInfo.ReturnType);

                if (isAsync)
                {
                    var method = InvokeActionAsyncMethod.MakeGenericMethod(parameterType);
                    await (Task)method.Invoke(null, new[] { subscription.Action, model })!;
                }
                else
                {
                    var method = InvokeActionMethod.MakeGenericMethod(parameterType);
                    method.Invoke(null, new[] { subscription.Action, model });
                }
            }
        }
    }

    private static void InvokeAction<T>(Delegate actionDelegate, T instance)
    {
        var action = actionDelegate as Action<T>;
        action?.Invoke(instance);
    }

    private static Task InvokeActionAsync<T>(Delegate actionDelegate, T instance)
    {
        var action = actionDelegate as Func<T, Task>;
        return action!.Invoke(instance);
    }

    private void PopulateBaseModel(IPersistentState persistentState)
    {
        if (HttpContext.Items.TryGetValue(HydroConsts.ContextItems.BaseModel, out var baseModel))
        {
            var unprotect = persistentState.Unprotect((string)baseModel);
            JsonConvert.PopulateObject(unprotect, this);
        }
    }

    private void PopulateRequestModel()
    {
        if (HttpContext.Items.TryGetValue(HydroConsts.ContextItems.RequestForm, out var requestForm))
        {
            BindModel((IFormCollection)requestForm);
        }
    }

    private string GetRootComponentId() =>
        ((string[])HttpContext.Items[HydroConsts.ContextItems.RenderedComponentIds]).First();

    private string GetViewPath()
    {
        var type = GetType();
        var assemblyName = type.Assembly.GetName().Name;
        return $"{type.FullName.Replace(assemblyName, "~").Replace(".", "/")}.cshtml";
    }

    private async Task<string> GetComponentHtml()
    {
        await using var writer = new StringWriter();
        var previousWriter = ViewComponentContext.ViewContext.Writer;
        ViewComponentContext.ViewContext.Writer = writer;
        ViewComponentContext.ViewContext.CheckBoxHiddenInputRenderMode = CheckBoxHiddenInputRenderMode.None;

        var result = View(GetViewPath(), this);

        await result.ExecuteAsync(ViewComponentContext);
        await writer.FlushAsync();
        var html = writer.ToString();

        ViewComponentContext.ViewContext.Writer = previousWriter;
        return html;
    }

    private void ApplyParameters(object parameters)
    {
        switch (parameters)
        {
            case null:
                return;

            case IDictionary<string, object> dictionary:
                ApplyObjectFromDictionary(this, dictionary);
                break;

            default:
                ApplyObject(this, parameters);
                break;
        }
    }

    private void ApplyObject<T>(T target, object source)
    {
        if (source == null || target == null)
        {
            return;
        }

        var sourceType = source.GetType();
        var targetType = target.GetType();

        foreach (var sourceProperty in sourceType.GetProperties())
        {
            var targetProperty = targetType.GetProperty(sourceProperty.Name);

            if (targetProperty == null || !targetProperty.CanWrite)
            {
                continue;
            }

            object sourceValue;

            if (sourceProperty.PropertyType == targetProperty.PropertyType)
            {
                sourceValue = sourceProperty.GetValue(source);
            }
            else
            {
                try
                {
                    var json = JsonConvert.SerializeObject(sourceProperty.GetValue(source));
                    sourceValue = JsonConvert.DeserializeObject(json, targetProperty.PropertyType);
                }
                catch
                {
                    throw new InvalidCastException($"Type mismatch in {sourceProperty.Name} parameter.");
                }
            }

            targetProperty.SetValue(target, sourceValue);
        }
    }

    private void ApplyObjectFromDictionary<T>(T target, IDictionary<string, object> source)
    {
        if (source == null || target == null)
        {
            return;
        }

        var targetType = target.GetType();

        foreach (var sourceProperty in source)
        {
            var targetProperty = targetType.GetProperty(sourceProperty.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            if (targetProperty == null || !targetProperty.CanWrite)
            {
                continue;
            }

            var value = sourceProperty.Value;

            if (value != null && value.GetType() != targetProperty.PropertyType)
            {
                throw new InvalidCastException($"Type mismatch in {sourceProperty.Key} parameter.");
            }

            targetProperty.SetValue(target, value);
        }
    }

    private void ValidateModel()
    {
        var context = new ValidationContext(this, serviceProvider: null, items: null);
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(this, context, validationResults, true);
        var extractValidationResults = ExtractValidationResults(validationResults);

        foreach (var validationResult in extractValidationResults)
        {
            foreach (var memberName in validationResult.MemberNames)
            {
                if (IsModelTouched || TouchedProperties.Contains(memberName))
                {
                    ModelState.AddModelError(memberName, validationResult.ErrorMessage);
                }
            }
        }

        IsValid = ModelState.IsValid;
    }

    private static IEnumerable<ValidationResult> ExtractValidationResults(IEnumerable<ValidationResult> validationResults)
    {
        foreach (var result in validationResults)
        {
            yield return result;

            if (result is ICompositeValidationResult compositeResult)
            {
                foreach (var innerResult in compositeResult.Results)
                {
                    yield return innerResult;
                }
            }
        }
    }

    private async Task<bool> AuthorizeAsync()
    {
        var type = GetType();
        
        if (!ComponentAuthorizationAttributes.ContainsKey(type))
        {
            ComponentAuthorizationAttributes.TryAdd(type, type.GetCustomAttributes(true)
                .Where(attr => attr is IHydroAuthorizationFilter)
                .Cast<IHydroAuthorizationFilter>()
                .ToArray());
        }
        
        foreach (var authorizationFilter in ComponentAuthorizationAttributes[type])
        {
            if (!await authorizationFilter.AuthorizeAsync(HttpContext, this))
            {
                if (HttpContext.IsHydro(excludeBoosted: true))
                {
                    HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                }
                
                return false;
            }
        }

        return true;
    }

    private static string Hash(string input) =>
        $"W{Convert.ToHexString(MD5.HashData(Encoding.ASCII.GetBytes(input)))}";
}