using Subble.Core;
using Subble.Core.Func;
using Subble.Core.Player;
using Subble.Core.Logger;
using Subble.Core.Events;
using System;
using System.IO;
using ManagedBass;

using static Subble.Core.Func.Option;
using System.Reactive.Linq;

namespace BasicMusicPlayer
{
    internal class MusicPlayer : IMusicPlayer
    {
        private const string EVENT_NAME = "BasicMusicPlayer";

        private PlayerStatus _status = PlayerStatus.Stop;
        private FileInfo _file;
        private int _handler = -1;

        private readonly ISubbleHost _host;
        private readonly Option<ILogger> _logger;

        public PlayerStatus Status { get => _status; }

        public Option<FileInfo> CurrentFile => Some(_file);

        public MusicPlayer(ISubbleHost host)
        {
            _host = host;
            _logger = _host.ServiceContainer.GetService<ILogger>();

            ConfigureUserInput(host);
            try
            {
                Bass.Init();
            }
            catch (Exception e)
            {
                Log(e.Message, LogLevel.Error);
            }
        }

        public void Pause()
        {
            if (_status == PlayerStatus.Pause)
            {
                Log("Player is already paused", LogLevel.Warning);
                return;
            }

            Bass.Pause();
            ChangeStatus(PlayerStatus.Pause);
        }

        public void Play(FileInfo file)
        {
            if (!file.Exists)
            {
                var message = $"Can't find file: {file.FullName}";
                Log(message, LogLevel.Error);
                ChangeStatus(PlayerStatus.Stop);
                return;
            }

            _handler = Bass.CreateStream(file.FullName);
            Bass.ChannelPlay(_handler);
            _file = file;
            ChangeStatus(PlayerStatus.Play);
            _host.EmitEvent(EventsType.MediaPlayer.START, EVENT_NAME, file.FullName);

        }

        public void Play()
        {
            if(_file?.Exists != true)
            {
                var message = "Select a file path to play";
                Log(message, LogLevel.Error);
                ChangeStatus(PlayerStatus.Stop);
                return;
            }

            if(_status == PlayerStatus.Play)
            {
                Log("Player is already playing", LogLevel.Warning);
                return;
            }

            Bass.ChannelPlay(_handler);
            ChangeStatus(PlayerStatus.Play);
        }

        public void Stop()
        {
            if (_status == PlayerStatus.Stop)
            {
                Log("Player is already stoped", LogLevel.Warning);
                return;
            }

            Bass.Stop();
            ChangeStatus(PlayerStatus.Stop);
        }

        public bool ValidateFile(FileInfo file)
        {
            throw new NotImplementedException();
        }

        private void Log(string message, LogLevel level = LogLevel.Info)
        {
            _logger.Some(l => l.Log(level, EVENT_NAME, message));
        }

        private void ChangeStatus(PlayerStatus status)
        {
            if (status == _status) return;

            _status = status;

            var fileName = _file?.FullName ?? "";
            var eventType = GetEventType(_status);

            _host.EmitEvent(eventType, EVENT_NAME, fileName);
            _host.EmitEvent(EventsType.MediaPlayer.STATUS_CHANGE, EVENT_NAME, fileName);

        }

        private string GetEventType(PlayerStatus status)
        {
            switch(status)
            {
                case PlayerStatus.Pause:
                    return EventsType.MediaPlayer.PAUSE;

                case PlayerStatus.Play:
                    return EventsType.MediaPlayer.PLAY;

                case PlayerStatus.Stop:
                    return EventsType.MediaPlayer.STOP;

                default:
                    return "BasicMusicPlayer_UNKNOWN";
            }
        }

        private void ConfigureUserInput(ISubbleHost host)
        {
            host
                .Events
                .Where(e => e.Type == EventsType.Core.INPUT)
                .Subscribe(OnInput);
        }

        private void OnInput(ISubbleEvent e)
        {
            e.Payload
                .Some<string>(input =>
                {
                    var cmd = input.Split(' ')[0];

                    switch(cmd)
                    {
                        case "play":
                            var args = "";

                            if (cmd.Length + 1 < input.Length)
                            {
                                args = input.Substring(cmd.Length + 1).Trim();
                                Play(new FileInfo(args));
                            }
                            else
                            {
                                Play();
                            }
                            break;
                        case "pause":
                            Pause();
                            break;

                        case "stop":
                            Stop();
                            break;
                    }
                });
        }
    }
}
