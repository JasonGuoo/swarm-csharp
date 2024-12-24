using Xunit;

namespace Swarm.CSharp.Tests;

public class SkipIfMockClientFactAttribute : FactAttribute
{
    public SkipIfMockClientFactAttribute()
    {
        // No-op since we don't use mock clients anymore
    }
}
