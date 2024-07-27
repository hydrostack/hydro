﻿using HtmlAgilityPack;
using Hydro.Configuration;
using Hydro.Utils;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Int32Converter = Hydro.Utils.Int32Converter;

namespace Hydro;

/// <summary>
/// Stateful and reactive view component
/// </summary>
public abstract class HydroComponent : ViewComponent
{

    private string _componentId;
    private bool _skipOutput;

    private readonly ConcurrentDictionary<CacheKey, object> _requestCache = new();
    private static readonly ConcurrentDictionary<CacheKey, object> PersistentCache = new();

    private static readonly ConcurrentDictionary<Type, List<HydroPoll>> Polls = new();

    private readonly List<string> _clientScripts = new();
    private readonly List<HydroComponentEvent> _dispatchEvents = new();
    private readonly HashSet<HydroEventSubscription> _subscriptions = new();

    private static readonly MethodInfo InvokeActionMethod = typeof(HydroComponent).GetMethod(nameof(InvokeAction), BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly MethodInfo InvokeActionAsyncMethod = typeof(HydroComponent).GetMethod(nameof(InvokeActionAsync), BindingFlags.Static | BindingFlags.NonPublic);

    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        Converters = new JsonConverter[] { new Int32Converter() }.ToList()
    };

    private static readonly ConcurrentDictionary<Type, IHydroAuthorizationFilter[]> ComponentAuthorizationAttributes = new();
    private HydroOptions _options;

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
    /// Determines if the current execution is related to the component mounting
    /// </summary>
    [Transient]
    public bool IsMount { get; set; }

    /// <summary>
    /// Unique component identifier
    /// </summary>
    public string ComponentId => _componentId;

    /// <summary />
    public HydroComponent()
    {
        Subscribe<HydroBind>(data => SetPropertyValue(data.Name, data.Value));

        ConfigurePolls();
    }

    private void ConfigurePolls()
    {
        var componentType = GetType();

        if (Polls.ContainsKey(componentType))
        {
            return;
        }

        var methods = componentType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.DeclaringType != typeof(HydroComponent))
            .Select(m => (Method: m, Attribute: m.GetCustomAttribute<PollAttribute>()))
            .Where(m => m.Attribute != null)
            .Select(m => (m.Method, m.Attribute, ParametersCount: m.Method.GetParameters().Length))
            .ToList();

        if (methods.Any(p => p.Method.GetBaseDefinition() != p.Method))
        {
            throw new InvalidOperationException("Poll can be defined only on custom actions");
        }

        if (methods.Any(p => p.ParametersCount != 0))
        {
            throw new InvalidOperationException("Poll can be defined only on actions without parameters");
        }

        if (methods.Any(p => p.Attribute.Interval <= 0))
        {
            throw new InvalidOperationException("Polling's interval is invalid");
        }

        var polls = methods
            .Select(p => new HydroPoll(p.Method.Name, TimeSpan.FromMilliseconds(p.Attribute.Interval)))
            .ToList();

        Polls.TryAdd(componentType, polls);
    }

    /// <summary>
    /// Implementation of ViewComponent's InvokeAsync method
    /// </summary>
    /// <param name="parameters">Parameters</param>
    /// <param name="key">Key</param>
    public async Task<IHtmlContent> InvokeAsync(object parameters = null, string key = null)
    {
        ApplyParameters(parameters);

        ViewContext.ViewData.TemplateInfo.HtmlFieldPrefix = null;
        Key = key;

        var persistentState = HttpContext.RequestServices.GetService<IPersistentState>();
        _options = HttpContext.RequestServices.GetService<HydroOptions>();

        var componentHtml = HttpContext.IsHydro(excludeBoosted: true)
            ? await RenderOnlineComponent(persistentState)
            : await RenderStaticComponent(persistentState);

        return new HtmlString(componentHtml);
    }

    /// <summary>
    /// Subscribes to a Hydro event
    /// </summary>
    /// <typeparam name="TEvent">Event type</typeparam>
    public void Subscribe<TEvent>(Func<string> subject = null) =>
        _subscriptions.Add(new HydroEventSubscription
        {
            EventName = GetFullTypeName(typeof(TEvent)),
            SubjectRetriever = subject,
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

    /// <summary>
    /// Subscribes to a Hydro event
    /// </summary>
    /// <param name="subject">Subject</param>
    /// <param name="action">Action to execute when event occurs</param>
    /// <typeparam name="TEvent">Event type</typeparam>
    public void Subscribe<TEvent>(Func<string> subject, Action<TEvent> action) =>
        _subscriptions.Add(new HydroEventSubscription
        {
            EventName = GetFullTypeName(typeof(TEvent)),
            SubjectRetriever = subject,
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
    /// Subscribes to a Hydro event
    /// </summary>
    /// <param name="subject">Subject</param>
    /// <param name="action">Action to execute when event occurs</param>
    /// <typeparam name="TEvent">Event type</typeparam>
    public void Subscribe<TEvent>(Func<string> subject, Func<TEvent, Task> action) =>
        _subscriptions.Add(new HydroEventSubscription
        {
            EventName = GetFullTypeName(typeof(TEvent)),
            Action = action,
            SubjectRetriever = subject
        });

    /// <summary>
    /// Triggers a Hydro event
    /// </summary>
    /// <param name="data">Data to pass</param>
    /// <param name="scope">Scope of the event</param>
    /// <param name="asynchronous">Do not chain the execution of handlers and run them separately</param>
    /// <typeparam name="TEvent">Event type</typeparam>
    public void Dispatch<TEvent>(TEvent data, Scope scope = Scope.Parent, bool asynchronous = false) =>
        Dispatch(GetFullTypeName(typeof(TEvent)), data, scope, asynchronous);

    /// <summary>
    /// Triggers a Hydro event
    /// </summary>
    /// <param name="name">Name of the event</param>
    /// <param name="data">Data to pass</param>
    /// <param name="scope">Scope of the event</param>
    /// <param name="subject">Subject</param>
    /// <param name="asynchronous">Do not chain the execution of handlers and run them separately</param>
    /// <typeparam name="TEvent">Event type</typeparam>
    public void Dispatch<TEvent>(string name, TEvent data, Scope scope = Scope.Parent, bool asynchronous = false, string subject = null)
    {
        var operationId = !asynchronous && HttpContext.Request.Headers.TryGetValue(HydroConsts.RequestHeaders.OperationId, out var incomingOperationId)
            ? incomingOperationId.First()
            : Guid.NewGuid().ToString("N");

        _dispatchEvents.Add(new HydroComponentEvent
        {
            Name = name,
            Data = data,
            Scope = scope.ToString().ToLower(),
            Subject = subject,
            OperationId = operationId
        });
    }

    /// <summary>
    /// Triggers a Hydro event in a global scope
    /// </summary>
    /// <param name="data">Data to pass</param>
    /// <param name="subject">Subject</param>
    /// <param name="asynchronous">Do not chain the execution of handlers and run them separately</param>
    /// <typeparam name="TEvent">Event type</typeparam>
    public void DispatchGlobal<TEvent>(TEvent data, string subject = null, bool asynchronous = false) =>
        Dispatch(GetFullTypeName(typeof(TEvent)), data, Scope.Global, asynchronous, subject);

    /// <summary>
    /// Provides actions that can be executed on client side
    /// </summary>
    public HydroClientActions Client { get; } = new();

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

        var cacheKey = new CacheKey(_componentId, func);
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

        var cacheValue = new Cache<T>(func);
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
        _componentId = componentId;

        PopulateBaseModel(persistentState);
        await PopulateRequestModel();
        if (!await AuthorizeAsync())
        {
            return string.Empty;
        }

        await TriggerEvent();

        ValidateTouched();

        await TriggerMethod();

        if (!_skipOutput)
        {
            await RenderAsync();
        }

        PopulateDispatchers();

        PopulateClientScripts();

        return !_skipOutput
            ? await GenerateComponentHtml(componentId, persistentState, false)
            : string.Empty;
    }

    private async Task<string> RenderOnlineNestedComponent(IPersistentState persistentState)
    {
        var componentId = GenerateComponentId(Key);
        _componentId = componentId;

        if (IsComponentIdRendered(componentId))
        {
            return GetComponentPlaceholderTemplate(componentId);
        }

        if (!await AuthorizeAsync())
        {
            return string.Empty;
        }

        IsMount = true;
        await MountAsync();
        await RenderAsync();
        return await GenerateComponentHtml(componentId, persistentState, true);
    }

    private static string GetComponentPlaceholderTemplate(string componentId) =>
        $"<div id=\"{componentId}\" key=\"{componentId}\" hydro hydro-placeholder></div>";

    private async Task<string> RenderStaticComponent(IPersistentState persistentState)
    {
        var componentId = GenerateComponentId(Key);
        _componentId = componentId;

        if (!await AuthorizeAsync())
        {
            return string.Empty;
        }

        IsMount = true;
        await MountAsync();
        await RenderAsync();
        return await GenerateComponentHtml(componentId, persistentState, true);
    }

    private string GenerateComponentId(string key)
    {
        var parentComponentId = HttpContext.Items[HydroConsts.Component.ParentComponentId];
        var mainId = parentComponentId ?? $"{Guid.NewGuid():N}";
        var typeName = GetType().Name;


        var generateComponentId = Hash($"{mainId}-{typeName}-{key}");
        return generateComponentId;
    }

    private async Task<string> GenerateComponentHtml(string componentId, IPersistentState persistentState, bool isStatic)
    {
        var previousParentComponentId = HttpContext.Items[HydroConsts.Component.ParentComponentId];
        HttpContext.Items[HydroConsts.Component.ParentComponentId] = componentId;

        var componentHtmlDocument = await GetComponentHtml();

        HttpContext.Items[HydroConsts.Component.ParentComponentId] = previousParentComponentId;

        var root = componentHtmlDocument.DocumentNode;

        if (root.ChildNodes.Count(n => n.NodeType == HtmlNodeType.Element) != 1)
        {
            throw new InvalidOperationException("The wire component must have only one root element.");
        }

        var rootElement = root.ChildNodes.First(n => n.NodeType == HtmlNodeType.Element);

        rootElement.SetAttributeValue("id", componentId);
        rootElement.SetAttributeValue("key", componentId);
        rootElement.SetAttributeValue("hydro-name", GetType().Name);
        rootElement.SetAttributeValue("x-data", "hydro");
        var hydroAttribute = rootElement.SetAttributeValue("hydro", null);
        hydroAttribute.QuoteType = AttributeValueQuote.WithoutValue;

        if (Polls.TryGetValue(GetType(), out var polls))
        {
            for (var i = 0; i < polls.Count; i++)
            {
                rootElement.AppendChild(GetPollScript(componentHtmlDocument, polls[i], i));
            }
        }

        rootElement.AppendChild(GetModelScript(componentHtmlDocument, componentId, persistentState));

        foreach (var subscription in _subscriptions)
        {
            rootElement.AppendChild(GetEventSubscriptionScript(componentHtmlDocument, subscription));
        }

        if (isStatic)
        {
            foreach (var script in _clientScripts)
            {
                rootElement.AppendChild(GetStaticScript(componentHtmlDocument, script));
            }
        }

        return rootElement.OuterHtml;
    }

    private async Task BindModel(IFormCollection formCollection)
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
            var setter = PropertyInjector.GetPropertySetter(this, pair.Key, pair.Value);
            var propertyPath = PropertyPath.ExtractPropertyPath(pair.Key);

            if (setter != null)
            {
                var value = setter.Value.Value == null
                    ? null
                    : _options.ValueMappersDictionary.TryGetValue(setter.Value.Value.GetType(), out var mapper)
                        ? await mapper.Map(setter.Value.Value)
                        : setter.Value.Value;

                setter.Value.Setter(value);
                await BindAsync(propertyPath, value);
            }
            else
            {
                await BindAsync(propertyPath, null);
            }
        }

        foreach (var file in formCollection.Files)
        {
            var setter = PropertyInjector.GetPropertySetter(this, file.Name, file);
            var propertyPath = PropertyPath.ExtractPropertyPath(file.Name);

            if (setter != null)
            {
                setter.Value.Setter(file);
                await BindAsync(propertyPath, file);
            }
            else
            {
                await BindAsync(propertyPath, null);
            }
        }
    }

    /// <summary>
    /// Applies value to a component
    /// </summary>
    /// <param name="path">Path to the value</param>
    /// <param name="value">Value</param>
    public async Task SetPropertyValue(string path, object value)
    {
        PropertyInjector.SetPropertyValue(this, path, value);
        TouchedProperties.Add(path);
        await BindAsync(PropertyPath.ExtractPropertyPath(path), value);
    }

    /// <summary>
    /// Triggered when a property is updated from the client
    /// </summary>
    /// <param name="property">Property path</param>
    /// <param name="value">New value</param>
    public virtual void Bind(PropertyPath property, object value)
    {
    }

    /// <summary>
    /// Triggered when a property is updated from the client
    /// </summary>
    /// <param name="property">Property path</param>
    /// <param name="value">New value</param>
    public virtual Task BindAsync(PropertyPath property, object value)
    {
        Bind(property, value);
        return Task.CompletedTask;
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
            subject = subscription.SubjectRetriever?.Invoke(),
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

    private HtmlNode GetPollScript(HtmlDocument document, HydroPoll poll, int index)
    {
        var scriptNode = document.CreateElement("script");
        scriptNode.SetAttributeValue($"x-hydro-polling.{poll.Interval.TotalMilliseconds}ms._{index}", poll.Action);
        scriptNode.SetAttributeValue("type", "text/hydro");
        return scriptNode;
    }

    private HtmlNode GetStaticScript(HtmlDocument document, string script)
    {
        var scriptNode = document.CreateElement("script");
        scriptNode.SetAttributeValue("hydro-js", "true");
        scriptNode.SetAttributeValue("type", "text/hydro");
        scriptNode.InnerHtml = script;
        return scriptNode;
    }

    private void PopulateDispatchers()
    {
        if (!_dispatchEvents.Any())
        {
            return;
        }

        var data = _dispatchEvents
            .Select(e => new
            {
                name = e.Name,
                data = Base64.Serialize(e.Data),
                scope = e.Scope,
                subject = e.Subject,
                operationId = e.OperationId
            })
            .ToList();

        HttpContext.Response.Headers.TryAdd(HydroConsts.ResponseHeaders.Trigger, JsonConvert.SerializeObject(data));
    }

    private void PopulateClientScripts()
    {
        if (!_clientScripts.Any())
        {
            return;
        }

        HttpContext.Response.Headers.TryAdd(HydroConsts.ResponseHeaders.Scripts, JsonConvert.SerializeObject(_clientScripts));

        _clientScripts.Clear();
    }

    private bool IsComponentIdRendered(string componentId)
    {
        var renderedComponentIds = (string[])HttpContext.Items[HydroConsts.ContextItems.RenderedComponentIds];
        return renderedComponentIds.Contains(componentId);
    }

    private async Task TriggerMethod()
    {
        if (!HttpContext.Items.TryGetValue(HydroConsts.ContextItems.MethodName, out var method)
            || method is not string methodValue || string.IsNullOrWhiteSpace(methodValue))
        {
            return;
        }

        var methodInfos = GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance);

        var requestParameters = (IDictionary<string, object>)HttpContext.Items[HydroConsts.ContextItems.Parameters];

        var methodInfo = methodInfos.FirstOrDefault(m =>
            string.Equals(m.Name, methodValue, StringComparison.OrdinalIgnoreCase)
            && m.GetParameters().Select(p => p.Name).SequenceEqual(requestParameters.Select(p => p.Key))
        );

        if (methodInfo == null)
        {
            return;
        }

        var methodParameters = methodInfo.GetParameters();
        var methodAttributes = methodInfo.GetCustomAttributes();

        if (requestParameters.Count != methodParameters.Length
            || requestParameters.Any(rp => !methodParameters.Any(mp => rp.Key == mp.Name)))
        {
            throw new InvalidOperationException("Wrong action parameters");
        }

        if (methodAttributes.Any(a => a.GetType() == typeof(SkipOutputAttribute)))
        {
            _skipOutput = true;
            HttpContext.Response.Headers.TryAdd(HydroConsts.ResponseHeaders.SkipOutput, "True");
        }

        var operationId = HttpContext.Request.Headers.TryGetValue(HydroConsts.RequestHeaders.OperationId, out var incomingOperationId)
            ? incomingOperationId.First()
            : Guid.NewGuid().ToString("N");

        HttpContext.Response.Headers.TryAdd(HydroConsts.ResponseHeaders.OperationId, operationId);

        var orderedParameters = methodParameters
            .Select(p =>
            {
                var requestParameter = requestParameters[p.Name!];

                if (requestParameter == null)
                {
                    return null;
                }

                var sourceType = requestParameter.GetType();

                if (p.ParameterType == sourceType)
                {
                    return requestParameter;
                }

                if (p.ParameterType.IsEnum)
                {
                    return Enum.ToObject(p.ParameterType, requestParameter);
                }

                return TypeDescriptor.GetConverter(p.ParameterType).ConvertFrom(requestParameter);
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
        if (!HttpContext.Items.TryGetValue(HydroConsts.ContextItems.EventName, out var eventName)
            || eventName is not string eventNameValue || string.IsNullOrWhiteSpace(eventNameValue))
        {
            return;
        }

        var eventSubject = (string)HttpContext.Items[HydroConsts.ContextItems.EventSubject];
        var subscriptions = _subscriptions
            .Where(s => s.EventName == eventNameValue && (s.SubjectRetriever == null || s.SubjectRetriever() == eventSubject))
            .ToList();

        foreach (var subscription in subscriptions)
        {
            var methodInfo = subscription.Action.Method;
            var parameters = methodInfo.GetParameters();
            var parameterType = parameters.First().ParameterType;
            var model = Base64.Deserialize((string)HttpContext.Items[HydroConsts.ContextItems.EventData], parameterType);

            var operationId = HttpContext.Request.Headers.TryGetValue(HydroConsts.RequestHeaders.OperationId, out var incomingOperationId)
                ? incomingOperationId.First()
                : Guid.NewGuid().ToString("N");

            HttpContext.Response.Headers.TryAdd(HydroConsts.ResponseHeaders.OperationId, operationId);

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

    private async Task PopulateRequestModel()
    {
        if (HttpContext.Items.TryGetValue(HydroConsts.ContextItems.RequestForm, out var requestForm))
        {
            await BindModel((IFormCollection)requestForm);
        }
    }

    private string GetRootComponentId() =>
        ((string[])HttpContext.Items[HydroConsts.ContextItems.RenderedComponentIds]).First();

    private string GetViewPath() =>
        ViewPath ?? GetViewPath(GetType());

    /// <summary>
    /// Get the view path based on the type
    /// </summary>
    protected string GetViewPath(Type type)
    {
        var assemblyName = type.Assembly.GetName().Name;
        return $"{type.FullName!.Replace(assemblyName!, "~").Replace(".", "/")}.cshtml";
    }

    /// Get the view path based on the view name
    protected string GetViewPath(string viewName)
    {
        var type = GetType();
        return GetViewPath(type).Replace($"{type.Name}.cshtml", $"{viewName}.cshtml");
    }

    /// <summary>
    /// Override this property if you want to use custom view path for the component
    /// </summary>
    public virtual string ViewPath => null;

    private async Task<HtmlDocument> GetComponentHtml()
    {
        using var stream = new MemoryStream();
        await using var writer = new StreamWriter(stream);

        var previousWriter = ViewComponentContext.ViewContext.Writer;
        ViewComponentContext.ViewContext.Writer = writer;
        ViewComponentContext.ViewContext.CheckBoxHiddenInputRenderMode = CheckBoxHiddenInputRenderMode.None;

        var result = View(GetViewPath(), this);

        await result.ExecuteAsync(ViewComponentContext);
        await writer.FlushAsync();

        stream.Position = 0;
        var htmlDocument = new HtmlDocument();
        htmlDocument.Load(stream);
        ViewComponentContext.ViewContext.Writer = previousWriter;
        return htmlDocument;
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

            if (value != null && !targetProperty.PropertyType.IsInstanceOfType(value))
            {
                throw new InvalidCastException($"Type mismatch in {sourceProperty.Key} parameter.");
            }

            targetProperty.SetValue(target, value);
        }
    }

    /// <summary>
    /// Validate the model state
    /// </summary>
    /// <returns>True if the state is valid</returns>
    public bool Validate()
    {
        IsModelTouched = true;
        return ValidateTouched();
    }

    private bool ValidateTouched()
    {
        ModelState.Clear();

        var context = new ValidationContext(this, serviceProvider: null, items: null);
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(this, context, validationResults, true);
        var extractValidationResults = ExtractValidationResults(validationResults);

        foreach (var validationResult in extractValidationResults)
        {
            foreach (var memberName in validationResult.MemberNames)
            {
                if (IsModelTouched || TouchedProperties.Contains(memberName) || TouchedProperties.Any(p => p.StartsWith($"{memberName}.")))
                {
                    ModelState.AddModelError(memberName, validationResult.ErrorMessage!);
                }
            }
        }

        IsValid = ModelState.IsValid;

        return IsValid;
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

    /// <summary>
    /// Queue JS to be executed on client side upon rendering component
    /// </summary>
    /// <param name="script"></param>
    public void ExecuteJs(string script)
    {
        _clientScripts.Add(script);
    }

    private static string Hash(string input) =>
        $"W{Convert.ToHexString(MD5.HashData(Encoding.ASCII.GetBytes(input)))}";
}