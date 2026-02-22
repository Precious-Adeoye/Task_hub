using System.Security.Cryptography;
using System.Text;

namespace TaskHub.Api.Middleware
{
    public class ETagMiddleware
    {
        private readonly RequestDelegate _next;

        public ETagMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var response = context.Response;
            var originalBody = response.Body;

            using (var memoryStream = new MemoryStream())
            {
                response.Body = memoryStream;

                await _next(context);

                if (response.StatusCode == 200 &&
                    context.Request.Method == HttpMethods.Get &&
                    response.ContentType?.StartsWith("application/json") == true)
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();

                    // Generate ETag from response body
                    var etag = GenerateETag(responseBody);

                    // Check If-None-Match for conditional GET
                    var ifNoneMatch = context.Request.Headers["If-None-Match"].ToString();
                    if (!string.IsNullOrEmpty(ifNoneMatch) && ifNoneMatch.Trim('"') == etag)
                    {
                        response.StatusCode = 304;
                        response.ContentLength = 0;
                        response.Body = originalBody;
                        return;
                    }

                    response.Headers.ETag = $"\"{etag}\"";

                    // Write the response back to original stream
                    response.Body = originalBody;
                    await response.WriteAsync(responseBody);
                }
                else
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    await memoryStream.CopyToAsync(originalBody);
                }
            }
        }

        private string GenerateETag(string content)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
