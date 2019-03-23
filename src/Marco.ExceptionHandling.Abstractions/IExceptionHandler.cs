using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Marco.ExceptionHandling.Abstractions
{
    public interface IExceptionHandler
    {
        Task<int> HandleAsync(Exception exception, HttpContext httpContext);
    }
}