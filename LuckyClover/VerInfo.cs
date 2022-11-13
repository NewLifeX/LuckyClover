using System;

namespace LuckyClover;

public class VerInfo
{
    public String Name { get; set; }

    public String Version { get; set; }

    public String Sp { get; set; }

    public override String ToString() => String.IsNullOrEmpty(Sp) ? $"{Name} {Version}" : $"{Name} {Version} Sp{Sp}";
}