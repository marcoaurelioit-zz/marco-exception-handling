using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Marco.ExceptionHandling
{
    public interface IExceptionHandler
    {
        Task<int> HandleAsync(Exception exception, HttpContext httpContext);
    }
}