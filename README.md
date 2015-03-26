# RequireHttpsExceptForLocalHostAttribute
Inherits from MVC6 RequireHttpsAttribute, but won't require HTTPS if running on localhost. 

Currently works with MVC6 Beta3

Use this attribute instead of the RequireHttpsAttribute in startup so youdon't have to mess around with configuring https, which can be a real pain, especially if running as a self hosted console app

Using this attribute in startup (or when decorating a MVC Method or Controller) will require https except if the hostname matches localhost.

Here is an segment from startup.cs
```csharp
    public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().Configure<MvcOptions>(options =>
            {
                  options.Filters.Add(typeof(RequireHttpsExceptForLocalHostAttribute));
            });
        }
```

The easiest way to use this would be to copy the RequireHttpsExceptForLocalHostAttribute.cs file and paste it into your code (change the namespace too)
        
