using NAudio.Wave;

namespace SpaceInvaders.Services
{
    public class SoundManager : IDisposable
    {
        private readonly Dictionary<string, string> _soundPathCache = new();
        private IWavePlayer? _musicPlayer;
        private AudioFileReader? _musicStream;

        public void PreloadSound(string soundName, string filePath)
        {
            _soundPathCache[soundName] = filePath;
        }

        public void PlaySound(string soundName)
        {
            if (_soundPathCache.TryGetValue(soundName, out var filePath))
            {
                try
                {
                    if (!File.Exists(filePath)) return;

                    var audioReader = new AudioFileReader(filePath);
                    var waveOut = new WaveOutEvent();
                    
                    waveOut.PlaybackStopped += (sender, args) =>
                    {
                        audioReader.Dispose();
                        waveOut.Dispose();
                    };

                    waveOut.Init(audioReader);
                    waveOut.Play();
                }
                catch
                {
                    // Ignora erros de áudio em produção para não quebrar o jogo.
                }
            }
        }

        public void PlayMusic(string musicName, bool loop = true)
        {
            StopMusic(); 

            if (_soundPathCache.TryGetValue(musicName, out var filePath))
            {
                try
                {
                    if (!File.Exists(filePath)) return;
                    
                    _musicPlayer = new WaveOutEvent();
                    _musicStream = new AudioFileReader(filePath);

                    if (loop)
                    {
                        _musicPlayer.PlaybackStopped += (s, e) =>
                        {
                            _musicStream?.Seek(0, SeekOrigin.Begin);
                            _musicPlayer?.Play();
                        };
                    }

                    _musicPlayer.Init(_musicStream);
                    _musicPlayer.Play();
                }
                catch
                {
                    // Ignora erros de áudio
                }
            }
        }

        public void StopMusic()
        {
            _musicPlayer?.Stop();
            _musicPlayer?.Dispose();
            _musicPlayer = null;
            
            _musicStream?.Dispose();
            _musicStream = null;
        }

        public void Dispose()
        {
            StopMusic();
        }
    }
}
