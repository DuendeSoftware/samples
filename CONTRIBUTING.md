# Contributing to the Duende Samples

## Look and feel

If you copy from the samples, you should not have to spend an hour removing things you don't need.
Keep styling minimal: the point of a sample is the code, not the CSS.

- **CSS framework:** use the Bootstrap version from the ASP.NET Core templates. No other CSS frameworks or themes.
- **Duende styling (optional):** copy the Bootstrap overrides for your Bootstrap version into the sample's
  `wwwroot/css` folder, and reference it right after `bootstrap.min.css`:
  - Bootstrap 4.5: [`assets/bootstrap-overrides/bootstrap-4/bootstrap-overrides.css`](assets/bootstrap-overrides/bootstrap-4/bootstrap-overrides.css)
  - Bootstrap 5: [`assets/bootstrap-overrides/bootstrap-5/bootstrap-overrides.css`](assets/bootstrap-overrides/bootstrap-5/bootstrap-overrides.css)

```html
<link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
<link rel="stylesheet" href="~/css/bootstrap-overrides.css" asp-append-version="true" />
```

## Continuous integration

The [Build workflow](.github/workflows/build.yml) compiles every sample on each pull request:

- every `.sln` / `.slnx` (except the root `samples.slnx`) is built in its own job; MAUI and .NET Framework (OWIN) samples build on Windows
- every npm project with a `package-lock.json` runs `npm ci` and `npm run build`

New samples are discovered automatically by [`discover-builds.sh`](.github/scripts/discover-builds.sh).
Make sure a new sample has its own solution file (and a committed `package-lock.json` for npm clients) so it gets built.
