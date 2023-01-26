using System.Threading;
using Ultimate.Language.Jint.Constraints;

// ReSharper disable once CheckNamespace
namespace Ultimate.Language.Jint;

public static class ConstraintsOptionsExtensions
{
    /// <summary>
    /// Limits the allowed statement count that can be run as part of the program.
    /// </summary>
    public static Options MaxStatements(this Options options, int maxStatements = 0)
    {
        options.WithoutConstraint(x => x is MaxStatementsConstraint);

        if (maxStatements > 0 && maxStatements < int.MaxValue)
        {
            options.Constraint(new MaxStatementsConstraint(maxStatements));
        }
        return options;
    }

    public static Options LimitMemory(this Options options, long memoryLimit)
    {
        options.WithoutConstraint(x => x is MemoryLimitConstraint);

        if (memoryLimit > 0 && memoryLimit < int.MaxValue)
        {
            options.Constraint(new MemoryLimitConstraint(memoryLimit));
        }
        return options;
    }

    public static Options TimeoutInterval(this Options options, TimeSpan timeoutInterval)
    {
        if (timeoutInterval > TimeSpan.Zero && timeoutInterval < TimeSpan.MaxValue)
        {
            options.Constraint(new TimeConstraint(timeoutInterval));
        }
        return options;
    }

    public static Options CancellationToken(this Options options, CancellationToken cancellationToken)
    {
        options.WithoutConstraint(x => x is CancellationConstraint);

        if (cancellationToken != default)
        {
            options.Constraint(new CancellationConstraint(cancellationToken));
        }
        return options;
    }
}
