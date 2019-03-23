using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Marco.ExceptionHandling.Abstractions;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Marco.Exceptions.Core;

namespace Marco.ExceptionHandling
{
    public class ExceptionHandler : IExceptionHandler
    {
        private readonly ILogger _logger;
        private readonly IExceptionHandlerConfiguration _configuration;

        public ExceptionHandler(ILoggerFactory loggerFactory,
            IExceptionHandlerConfiguration configuration)
        {
            _logger = loggerFactory?.CreateLogger<ExceptionHandler>();

            _configuration = configuration;

            if (_logger == null)
                throw new ArgumentNullException(nameof(loggerFactory));
        }

        public async Task<int> HandleAsync(Exception exception, HttpContext httpContext)
        {
            ExceptionHandlerBehavior? behavior = null;
            (httpContext.Response.StatusCode, exception, behavior) = 
                await ValidateConfigurationsAsync(httpContext.Response.StatusCode, exception);

            if (exception is AggregateException)
            {
                var aggregateException = exception as AggregateException;

                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    await HandleAsync(innerException, httpContext);
                }
            }
            else
            {
                if (!behavior.HasValue)
                {
                    if (exception is UnauthorizedAccessException)
                    {
                        httpContext.Response.StatusCode = 403;

                        return await GenerateUnauthorizadeExceptionResponseAsync(httpContext);
                    }

                    behavior = await IdentifyBehaviorAsync(exception, httpContext);
                }

                switch (behavior)
                {
                    case ExceptionHandlerBehavior.ClientError:
                        return await GenerateCoreExceptionResponseAsync(exception, httpContext);
                 
                    case ExceptionHandlerBehavior.ServerError:
                        return await GenerateInternalErrorResponseAsync(exception, httpContext);
                 
                }
            }

            return httpContext.Response.StatusCode;
        }

        private async Task<ExceptionHandlerBehavior> IdentifyBehaviorAsync(Exception exception, HttpContext httpContext)
        {
            ExceptionHandlerBehavior behavior;

            if (exception is CoreException)
            {
                behavior = ExceptionHandlerBehavior.ClientError;

                httpContext.Response.StatusCode = 400;
            }
            else
            {
                behavior = ExceptionHandlerBehavior.ServerError;
                httpContext.Response.StatusCode = 500;
            }

            return await Task.FromResult(behavior);
        }

        private async Task<int> GenerateCoreExceptionResponseAsync(Exception e, HttpContext httpContext)
        {
            _logger.LogInformation(e, "Ocorreu um erro de negócio.");

            await GenerateResponseAsync(e, httpContext);

            return httpContext.Response.StatusCode;
        }

        /// <summary>
        /// Retorna um httpStatusCode 401.
        /// </summary>
        /// <param name="e">Inner Exception</param>
        /// <param name="httpContext">HttpContext</param>
        /// <returns></returns>
        private async Task<int> GenerateUnauthorizadeExceptionResponseAsync(HttpContext httpContext)
        {
            _logger.LogInformation("Ocorreu um acesso não autorizado.");

            var forbidden = new
            {
                Key = "Forbidden",
                Message = "Access to this resource is forbidden."
            };

            await GenerateResponseAsync(forbidden, httpContext);

            return httpContext.Response.StatusCode;
        }

        private bool IsDevelopmentEnvironment()
            => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        private async Task<int> GenerateInternalErrorResponseAsync(Exception e, HttpContext httpContext)
        {
            Exception exception = e;
            Guid logEntryId = Guid.NewGuid();
            
            _logger.LogError(e, "{LogEntryId}: Ocorreu um erro não esperado.", logEntryId);           

            var internalError = new InternalError()
            {
                LogEntryId = logEntryId,
                Exception = (IsDevelopmentEnvironment() ? exception.GetBaseException() : null)
            };

            await GenerateResponseAsync( internalError, httpContext);

            return httpContext.Response.StatusCode;
        }

        private async Task GenerateResponseAsync(object output, HttpContext httpContext)
        {
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                ContractResolver =
                    new JsonContractResolverCoreException()
                    {
                        IgnoreSerializableInterface = true
                    },
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                MaxDepth = 10,
                Formatting = !IsDevelopmentEnvironment() ? Formatting.None : Formatting.Indented
            };

            var message = JsonConvert.SerializeObject(output, jsonSerializerSettings);

            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(message, Encoding.UTF8);
        }

        private Task<(int statusCode, Exception exception, ExceptionHandlerBehavior? behavior)> ValidateConfigurationsAsync(int statusCode, Exception e)
        {
            Exception exception = e;
            int finalStatusCode = statusCode;
            ExceptionHandlerBehavior? behavior = null;
            
            if (_configuration != null)
            {
                if (_configuration.Events.Any())
                {
                    foreach (var @event in _configuration.Events)
                    {
                        if (@event.IsElegible(statusCode, e))
                            (finalStatusCode, exception, behavior) = @event.Intercept(statusCode, e);
                    }
                }

                if (_configuration.HasBehaviors)
                {
                    var behaviorResult = _configuration.ValidateBehavior(e);

                    if (behaviorResult != null)
                    {
                        behavior = behaviorResult.Behavior;                       

                        finalStatusCode = behaviorResult.StatusCode;
                    }
                }
            }

            return Task.FromResult((finalStatusCode, exception, behavior));
        }
    }
}