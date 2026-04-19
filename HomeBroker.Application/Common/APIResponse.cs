using System.Net;

namespace HomeBroker.Application
{
    public class APIResponse
    {
        APIResponse() { }
        public APIResponse(object? data = null, List<string>? errors = default, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            StatusCode = statusCode;
            Data = data;
            Errors = errors;
        }
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public object? Data { get; set; } = null;
        public List<string>? Errors { get; set; } = new();
    }
}
