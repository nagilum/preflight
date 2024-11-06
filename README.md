# Preflight

Preflight is a simple console application to check an URL if it passes CORS preflight request.

## How to Build

```bash
git clone https://github.com/nagilum/preflight
cd preflight
dotnet build
```

## How to Use

```bash
preflight https://api.example.com/url-to-check --origin https://example.com
```

This will perform an `OPTIONS` request to the `https://api.example.com/url-to-check` URL and add the `Origin` header with the value `https://example.com`. With the `Access-Control-Request-Method` header that is added automatically, and defaults to `GET`, this is all that is required to perform a basic CORS preflight request.

## More Options

### HTTP Method

You can specify a different HTTP method than `GET` by using the `--method` or `-m` option, like so `--method POST`.

### Headers

You can specify which headers to check for by using the `--headers` or `-e` option, like so `--headers content-type,x-pingother`.
This will add the `Access-Control-Request-Headers` header to the request which is answered by the `Access-Control-Allow-Headers` header in the response.

### Help

You can access the help page by using the `--help` or `-h` option, or by adding no paramters at all.

## Tests

The program performs 5 tests after the request is done.

1. Checks if the response status code is either 200 or 204.
2. Checks if the `Access-Control-Allow-Origin` header is present, which is required.
3. Checks if the `Access-Control-Allow-Methods` header is present, which is required.
4. Checks if the `Access-Control-Allow-Headers` header is present.
5. Checks if the `Access-Control-Max-Age` header is present.
