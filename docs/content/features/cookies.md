---
outline: deep
---

# Cookies

Hydro components provide a simple way to work with cookies in your application. You can read, write, and delete cookies using the `CookieStorage` property on the `HydroComponent` class:

```c#
// ThemeSwitcher.cshtml.cs

public class ThemeSwitcher : HydroComponent
{
    public string Theme { get; set; }
    
    public override void Mount()
    {
        Theme = CookieStorage.Get<string>("theme", defaultValue: "light");
    }
    
    public void Switch(string theme)
    {
        Theme = theme;
        CookieStorage.Set("theme", theme);
    }
}
```

## Complex objects

You can also store complex objects in cookies. Hydro will serialize and deserialize them for you:

```c#
// UserSettings.cshtml.cs

public class UserSettings : HydroComponent
{
    public UserSettingsStorage Storage { get; set; }
    
    public override void Mount()
    {
        Storage = CookieStorage.Get("settings", defaultValue: new UserSettingsStorage());
    }
    
    public void SwitchTheme(string theme)
    {
        Storage.Theme = theme;
        CookieStorage.Set("settings", Storage);
    }
    
    public class UserSettingsStorage
    {
        public string StartupPage { get; set; }        
        public string Theme { get; set; }
    }
}
```

## Customizing cookies

Default expiration date is 30 days, but can be customized with expiration parameter:

```c#
CookieStorage.Set("theme", "light", expiration: TimeSpan.FromDays(7));
```

You can further customize the cookie settings by passing an instance of `CookieOptions` to the `Set` method:

```c#
CookieStorage.Set("theme", "light", encrypt: false, new CookieOptions { Secure = true });
```

## Encryption

It's possible to encrypt the cookie value by setting the `encryption` parameter to `true`:

```c#
CookieStorage.Set("theme", "light", encryption: true);
```

```c#
CookieStorage.Get<string>("theme", encryption: true);
```