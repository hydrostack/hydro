---
outline: deep
---

# Binding

You can bind your component's properties to any input/select/textarea element by using `hydro-bind`. It will synchronize the client value with server value on the chosen event (`change` as default).

Example:

```csharp
// NameForm.cshtml.cs

public class NameForm : HydroComponent
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}
```

```razor
<!-- NameForm.cshtml -->

@model NameForm

<div>
  <input asp-for="FirstName" hydro-bind />
  <input asp-for="LastName" hydro-bind />
  <span>Full name: @Model.FirstName @Model.LastName</span>
</div>
```

## Trigger event

The default event used for binding is `change`. To choose another event, you can specify it:

```razor
<input asp-for="Search" hydro-bind:input />
```

## Debouncing

To debounce the bind event use the `.debounce` attribute expression:
```razor
<input asp-for="Search" hydro-bind:keydown.debounce.500ms />
```

## Handling `bind` event in a component

In order to inject custom logic after `bind` is executed, override the `Bind` method in your Hydro component:

```c#
public string Name { get; set; }

public override void Bind(PropertyPath property, object value)
{
    if (property.Name == nameof(Name))
    {
        var newValue = (string)value;
        // your logic    
    }
}
```

## File upload

You can use `file` inputs to enable file upload. Example:

```razor
<!-- AddAttachment.cshtml -->

@model AddAttachment

<div>
  <input
    asp-for="DocumentFile"
    type="file"
    hydro-bind />
</div>
```

```c#
// AddAttachment.cshtml.cs

public class AddAttachment : HydroComponent
{
    [Transient]
    public IFormFile DocumentFile { get; set; }

    [Required]
    public string DocumentId { get; set; }

    public async Task Save()
    {
        if (!Validate())
        {
            return;
        }

        var tempFilePath = GetTempFileLocation(DocumentId);

        // Move your file at tempFilePath to the final storage
        // and save that information in your domain
    }

    public override async Task BindAsync(PropertyPath property, object value)
    {
        if (property.Name == nameof(DocumentFile))
        {
            // assign the temp file name to the DocumentId
            DocumentId = await GetStoredTempFileId((IFormFile)value);
        }
    }

    private static async Task<string> GetStoredTempFileId(IFormFile file)
    {
        if (file == null)
        {
            return null;
        }

        var tempFileName = Guid.NewGuid().ToString("N");
        var tempFilePath = GetTempFileLocation(tempFileName);

        await using var readStream = file.OpenReadStream();
        await using var writeStream = File.OpenWrite(tempFilePath);
        await readStream.CopyToAsync(writeStream);
        
        return tempFileName;
    }

    private static string GetTempFileLocation(string fileName) =>
        Path.Combine(Path.GetTempPath(), fileName);
}
```

`DocumentFile` property represents the file that is sent by the user. We need to put `[Transient]` attribute on it, to make sure it's not
serialized, kept on the page, and sent back to the server each time - it would be a lot of data to transfer in case of large files.

The place where we interact with the uploaded file is the `BindAsync` method. We store the file in a temporary storage
which we can use later when submitting the form.

> NOTE: Make sure the temporary storage is cleared periodically.

## Styling

`.hydro-request` CSS class is toggled on the elements that are currently in the binding process