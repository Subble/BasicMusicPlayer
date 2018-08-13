using Subble.Core;
using Subble.Core.Logger;
using Subble.Core.Player;
using Subble.Core.Plugin;
using System.Collections.Generic;

namespace BasicMusicPlayer
{
    public class Plugin : ISubblePlugin
    {
        public IPluginInfo Info
            => new PluginInfo();

        public SemVersion Version
            => new SemVersion(0, 1, 0);

        public long LoadPriority => 20;

        public IEnumerable<Dependency> Dependencies
            => new[] {
                new Dependency(typeof(ILogger), (0,0,1))
            };

        public bool Initialize(ISubbleHost host)
        {
            if (host is null)
                return false;

            var player = new MusicPlayer(host);
            host.ServiceContainer.RegisterService<IMusicPlayer>(player, Version);

            return true;
        }
    }
}
