using Ultimate.Language.Jint.Runtime;
using System.Threading;

namespace Ultimate.Language.Jint.Constraints;

internal sealed class TimeConstraint : Constraint
{
    private readonly TimeSpan _timeout;
    private CancellationTokenSource? _cts;

    internal TimeConstraint(TimeSpan timeout)
    {
        _timeout = timeout;
    }

    public override void Check()
    {
        if (_cts?.IsCancellationRequested == true)
        {
            ExceptionHelper.ThrowTimeoutException();
        }
    }

    public override void Reset()
    {
        _cts?.Dispose();

        // This cancellation token source is very likely not disposed property, but it only allocates a timer, so not a big deal.
        // But using the cancellation token source is faster because we do not have to check the current time for each statement,
        // which means less system calls.
        _cts = new CancellationTokenSource(_timeout);
    }
}
