using Consid.Umbraco.CodeFirst.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Consid.Umbraco.CodeFirst;

public sealed class CodeFirstComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) => builder.AddCodeFirst();
}
