using System;

namespace Installer;

public interface ITracer
{
    ISpan NewSpan(String name, Object tag = null);
}

public interface ISpan : IDisposable
{
    void SetTag(Object tag);
    void SetError(Exception ex, Object tag = null);
}
