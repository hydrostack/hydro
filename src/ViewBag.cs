using System.Diagnostics;
using System.Dynamic;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Hydro;

[DebuggerDisplay("Count = {ViewData.Count}")]
[DebuggerTypeProxy(typeof(DynamicViewDataView))]
internal sealed class ViewBag : DynamicObject
{
    private readonly Func<ViewDataDictionary> _viewDataFunc;

    public ViewBag(Func<ViewDataDictionary> viewDataFunc) =>
        _viewDataFunc = viewDataFunc;

    private ViewDataDictionary ViewData =>
        _viewDataFunc() ?? throw new InvalidOperationException("Invalid view data");

    public override IEnumerable<string> GetDynamicMemberNames() =>
        ViewData.Keys;

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
        result = ViewData[binder.Name];
        return true;
    }

    public override bool TrySetMember(SetMemberBinder binder, object value)
    {
        ViewData[binder.Name] = value;
        return true;
    }

    private sealed class DynamicViewDataView
    {
        private readonly ViewDataDictionary _viewData;

        public DynamicViewDataView(ViewBag dictionary)
        {
            _viewData = dictionary.ViewData;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<string, object>[] Items => _viewData.ToArray();
    }
}