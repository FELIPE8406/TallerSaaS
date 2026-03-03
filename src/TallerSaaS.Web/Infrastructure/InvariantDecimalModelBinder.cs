using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System.Globalization;

namespace TallerSaaS.Web.Infrastructure;

/// <summary>
/// Custom model binder that parses decimal values using InvariantCulture.
/// Needed because HTML &lt;input type="number"&gt; always sends dot-decimals (87500.50),
/// but setting the thread culture to es-CO makes ASP.NET Core's default binder
/// expect comma-decimals — causing silent binding failures (value becomes 0).
/// </summary>
public class InvariantDecimalModelBinder : IModelBinder
{
    private readonly SimpleTypeModelBinder _fallback;

    public InvariantDecimalModelBinder(Type type)
    {
        _fallback = new SimpleTypeModelBinder(type, NullLoggerFactory.Instance);
    }

    public Task BindModelAsync(ModelBindingContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var valueResult = context.ValueProvider.GetValue(context.ModelName);
        if (valueResult == ValueProviderResult.None)
            return _fallback.BindModelAsync(context);

        context.ModelState.SetModelValue(context.ModelName, valueResult);

        var raw = valueResult.FirstValue;
        if (string.IsNullOrWhiteSpace(raw))
        {
            context.Result = ModelBindingResult.Success(0m);
            return Task.CompletedTask;
        }

        // Parse with dot-decimal regardless of thread culture
        if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            context.Result = ModelBindingResult.Success(value);
        }
        else
        {
            context.ModelState.TryAddModelError(context.ModelName,
                $"El valor '{raw}' no es un número válido.");
        }

        return Task.CompletedTask;
    }

    private sealed class NullLoggerFactory : ILoggerFactory
    {
        public static readonly NullLoggerFactory Instance = new();
        public void AddProvider(ILoggerProvider provider) { }
        public ILogger CreateLogger(string categoryName) => NullLogger.Instance;
        public void Dispose() { }
    }

    private sealed class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}

/// <summary>
/// Registers InvariantDecimalModelBinder for all decimal and double properties.
/// </summary>
public class InvariantDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var type = context.Metadata.UnderlyingOrModelType;
        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
            return new InvariantDecimalModelBinder(type);

        return null;
    }
}
