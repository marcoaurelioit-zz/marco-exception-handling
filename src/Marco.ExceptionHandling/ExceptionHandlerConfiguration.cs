using Marco.ExceptionHandling.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marco.ExceptionHandling
{
    public class ExceptionHandlerConfiguration : IExceptionHandlerConfiguration
    {
        private Dictionary<Type, ForExceptionBehavior> _behaviors;
        public List<IExceptionHandlerEvent> Events { get; }
        public bool HasBehaviors => _behaviors.Any();

        public ExceptionHandlerConfiguration(Action<IExceptionHandlerConfigurationExpression> action)
        {
            Events = new List<IExceptionHandlerEvent>();
            _behaviors = new Dictionary<Type, ForExceptionBehavior>();
            Build(action);
        }      

        public ForExceptionBehavior ValidateBehavior(Exception ex)
        {
            foreach (var behavior in _behaviors)
            {
                if (behavior.Key.IsAssignableFrom(ex.GetType()))
                    return behavior.Value;
            }

            return null;
        }

        private void Build(Action<IExceptionHandlerConfigurationExpression> action)
        {
            var configurationExpression = new ExceptionHandlerConfigurationExpression();

            action(configurationExpression);

            Events.AddRange(configurationExpression.Events);

            _behaviors = new Dictionary<Type, ForExceptionBehavior>(configurationExpression.Behaviors);
        }
    }
}