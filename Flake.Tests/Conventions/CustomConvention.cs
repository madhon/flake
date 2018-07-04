namespace Flake.Tests
{
    using Fixie;

    public class CustomConvention : Discovery
    {
        public CustomConvention()
        {
            Classes.Where(x => x.Has<TestFixtureAttribute>());

            Methods.Where(x=>x.Has<TestAttribute>());
         }
    }

}