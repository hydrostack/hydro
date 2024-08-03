using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Hydro.Services
{

    /// <summary>
    /// Allows to store/read complex objects in cookies
    /// </summary>
    public interface ICookieManager
    {
        /// <summary>
        /// Returns a value type or a specific class stored in cookies.
        /// If cookie was previously saved as `secure` or  you need to decrypt it by setting this method `secure` parameter also to `true`.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="secure"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T Get<T>(string key, bool secure = false, T defaultValue = default);

        /// <summary>
        /// Stores in cookies a value type or a specific class. If `secure` parameter is `true` will also be encrypted.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="secure"></param>
        /// <param name="expires"></param>
        public void Set<T>(string key, T value, bool secure = false, TimeSpan? expires = null);

        /// <summary>
        /// Stores in cookies a value type or a specific class with exact options to be used. Will also be encrypted is `Secure` is enabled in options.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public void Set<T>(string key, T value, CookieOptions options);

        /// <summary>
        /// Deletes a cookie record
        /// </summary>
        /// <param name="key"></param>
        public void Delete(string key);
    }

    /// <summary>
    /// Provides a standard implementation for ICookieManager interface, allowing to store/read complex objects in cookies
    /// </summary>
    public class CookieManager : ICookieManager
    {

        /// <summary>
        /// Primary constructor
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        /// <param name="dataProtectionProvider"></param>
        public CookieManager(IHttpContextAccessor httpContextAccessor,
            IDataProtectionProvider dataProtectionProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _dataProtectionProvider = dataProtectionProvider;
        }

        /// <summary>
        /// Caching protectors for quick access
        /// </summary>
        protected Dictionary<Type, IDataProtector> CachedProtectors = new();

        /// <summary>
        /// Returns cached protector for a specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual IDataProtector GetProtector<T>(T value)
        {
            CachedProtectors.TryGetValue(typeof(T), out var protector);
            if (protector == null)
            {
                protector = _dataProtectionProvider.CreateProtector(typeof(T).Name);
                CachedProtectors.TryAdd(typeof(T), protector);
            }
            return protector;
        }

        /// <summary>
        /// Returns a value type or a specific class stored in cookies
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="secure"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T Get<T>(string key, bool secure = false, T defaultValue = default)
        {
            try
            {
                _httpContextAccessor.HttpContext!.Request.Cookies.TryGetValue(key, out var storage);
                if (storage != null)
                {
                    if (secure)
                    {
                        return JsonConvert.DeserializeObject<T>(GetProtector(defaultValue).Unprotect(storage));
                    }
                    return JsonConvert.DeserializeObject<T>(storage);
                }
            }
            catch
            {
                //ignored
            }

            return defaultValue;
        }

        /// <summary>
        /// Customizable default time to be used when `expires` parameter is omitted upon calling the `Set` method. Default is 30 days.
        /// </summary>
        public static TimeSpan ExpiresDefault = TimeSpan.FromDays(30);

        /// <summary>
        /// Whether it will use data protection for secure cookies. You might sometimes want to turn this off for debugging purposes to inspect browser storage.
        /// </summary>
        public static bool UseDataProtection = true;

        /// <summary>
        /// Customizable default JsonSerializerSettings used for complex objects
        /// </summary>
        public static JsonSerializerSettings JsonSettings = new()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDataProtectionProvider _dataProtectionProvider;

        /// <summary>
        /// Stores in cookies a value type or a specific class. If `secure` parameter is `true` will also be encrypted.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="secure"></param>
        /// <param name="expires"></param>
        public void Set<T>(string key, T value, bool secure = false, TimeSpan? expires = null)
        {
            if (expires == null)
            {
                expires = ExpiresDefault;
            }

            var options = new CookieOptions
            {
                Secure = secure,
                MaxAge = expires.Value
            };

            Set(key, value, options);
        }

        /// <summary>
        /// Stores in cookies a value type or a specific class with exact options to be used. Will also be encrypted is `Secure` is enabled in options.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public void Set<T>(string key, T value, CookieOptions options)
        {
            var response = _httpContextAccessor.HttpContext!.Response;

            if (value != null)
            {
                string serializeObject;
                if (options.Secure && UseDataProtection)
                {
                    serializeObject = GetProtector(value).Protect(JsonConvert.SerializeObject(value));
                }
                else
                {
                    serializeObject = JsonConvert.SerializeObject(value, JsonSettings);
                }
                response.Cookies.Append(key, serializeObject, options);
            }
            else
            {
                response.Cookies.Delete(key);
            }
        }

        /// <summary>
        /// Deletes a cookie record
        /// </summary>
        /// <param name="key"></param>
        public void Delete(string key)
        {
            var response = _httpContextAccessor.HttpContext!.Response;
            response.Cookies.Delete(key);
        }
    }
}
