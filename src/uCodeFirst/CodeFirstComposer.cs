using uCodeFirst.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace uCodeFirst;

public sealed class CodeFirstComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder) => builder.AddCodeFirst();
}
