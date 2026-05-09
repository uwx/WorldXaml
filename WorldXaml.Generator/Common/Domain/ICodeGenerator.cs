using System.Collections.Generic;

namespace WorldXaml.Generator.Common.Domain;

internal interface ICodeGenerator
{
    string GenerateCode(string className, string nameSpace, IEnumerable<ResolvedName> names);
}
