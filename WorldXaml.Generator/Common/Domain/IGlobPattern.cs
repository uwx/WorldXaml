using System;

namespace WorldXaml.Generator.Common.Domain;

internal interface IGlobPattern : IEquatable<IGlobPattern>
{
    bool Matches(string str);
}
