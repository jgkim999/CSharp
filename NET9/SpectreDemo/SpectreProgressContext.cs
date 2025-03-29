using Spectre.Console;

using Unity.Tools;

namespace SpectreDemo
{
    public class SpectreProgressContext : IProgressContext
    {
        private readonly StatusContext _ctx;
        
        public SpectreProgressContext(StatusContext ctx)
        {
            _ctx = ctx;
        }

        public void Status(string message)
        {
            _ctx.Status(message);
        }
    }
}
