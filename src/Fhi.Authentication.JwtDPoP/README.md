# Fhi.Authentication.JwtDPoP


## Documentation

For official documentation, please see the [documentation site](https://fhidev.github.io/Fhi.AuthExtensions/)

## Contributing

See [CONTRIBUTING.md](https://github.com/FHIDev/Fhi.AuthExtensions/blob/main/CONTRIBUTING.md)

## License
See [License.md](https://github.com/FHIDev/Fhi.AuthExtensions/blob/main/License.md)


## Usage

```
builder.Services
.AddAuthentication()
 .AddJwtDpop("DPoP", options =>
    {
        options.Audience = "<audience>";
        options.Authority = "<authority>";
    });
```