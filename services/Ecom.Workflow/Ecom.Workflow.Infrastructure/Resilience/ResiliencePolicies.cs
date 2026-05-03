using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using RabbitMQ.Client.Exceptions;

namespace Ecom.Workflow.Infrastructure.Resilience;

/// <summary>
/// Centralized Polly resilience policies for RabbitMQ operations
/// Implements retry with exponential backoff and circuit breaker pattern
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Retry policy: 3 retries with exponential backoff (1s, 2s, 4s)
    /// Handles transient RabbitMQ failures
    /// </summary>
    public static ResiliencePipeline<T> CreateRetryPolicy<T>(ILogger logger)
    {
        return new ResiliencePipelineBuilder<T>()
            .AddRetry(new RetryStrategyOptions<T>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "RESILIENCE_RETRY | Attempt: {AttemptNumber} | Delay: {Delay}ms | Exception: {Exception}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? "Unknown");
                    return ValueTask.CompletedTask;
                },
                ShouldHandle = new PredicateBuilder<T>()
                    .Handle<BrokerUnreachableException>()
                    .Handle<AlreadyClosedException>()
                    .Handle<System.Net.Sockets.SocketException>()
                    .Handle<System.IO.IOException>()
            })
            .Build();
    }

    /// <summary>
    /// Circuit breaker policy: Opens after 5 consecutive failures, stays open for 30 seconds
    /// Prevents cascading failures and gives system time to recover
    /// </summary>
    public static ResiliencePipeline<T> CreateCircuitBreakerPolicy<T>(ILogger logger)
    {
        return new ResiliencePipelineBuilder<T>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<T>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(10),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30),
                OnOpened = args =>
                {
                    logger.LogError(
                        "RESILIENCE_CIRCUIT_OPENED | Circuit breaker opened after {FailureCount} failures. Will retry after {BreakDuration}s",
                        5,
                        30);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    logger.LogInformation("RESILIENCE_CIRCUIT_CLOSED | Circuit breaker closed, normal operation resumed");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    logger.LogInformation("RESILIENCE_CIRCUIT_HALF_OPEN | Circuit breaker half-open, testing connection");
                    return ValueTask.CompletedTask;
                },
                ShouldHandle = new PredicateBuilder<T>()
                    .Handle<BrokerUnreachableException>()
                    .Handle<AlreadyClosedException>()
                    .Handle<System.Net.Sockets.SocketException>()
                    .Handle<System.IO.IOException>()
            })
            .Build();
    }

    /// <summary>
    /// Combined policy: Retry + Circuit Breaker
    /// Retry handles transient failures, circuit breaker prevents cascading failures
    /// </summary>
    public static ResiliencePipeline<T> CreateCombinedPolicy<T>(ILogger logger)
    {
        return new ResiliencePipelineBuilder<T>()
            // First: Retry for transient failures
            .AddRetry(new RetryStrategyOptions<T>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "RESILIENCE_RETRY | Attempt: {AttemptNumber}/{MaxAttempts} | Delay: {Delay}ms | Exception: {Exception}",
                        args.AttemptNumber,
                        3,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? "Unknown");
                    return ValueTask.CompletedTask;
                },
                ShouldHandle = new PredicateBuilder<T>()
                    .Handle<BrokerUnreachableException>()
                    .Handle<AlreadyClosedException>()
                    .Handle<System.Net.Sockets.SocketException>()
                    .Handle<System.IO.IOException>()
            })
            // Second: Circuit breaker for persistent failures
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<T>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(10),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30),
                OnOpened = args =>
                {
                    logger.LogError(
                        "RESILIENCE_CIRCUIT_OPENED | Circuit breaker opened. System will not attempt operations for {BreakDuration}s",
                        30);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    logger.LogInformation("RESILIENCE_CIRCUIT_CLOSED | Circuit breaker closed, normal operation resumed");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    logger.LogInformation("RESILIENCE_CIRCUIT_HALF_OPEN | Circuit breaker testing connection");
                    return ValueTask.CompletedTask;
                },
                ShouldHandle = new PredicateBuilder<T>()
                    .Handle<BrokerUnreachableException>()
                    .Handle<AlreadyClosedException>()
                    .Handle<System.Net.Sockets.SocketException>()
                    .Handle<System.IO.IOException>()
            })
            .Build();
    }
}
