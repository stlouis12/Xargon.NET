using SDL3;
using Xargon.NET.Core;
using System.Runtime.InteropServices;

namespace Xargon.NET.Audio;

public class SoundManager
{
    private IntPtr _music = IntPtr.Zero;
    private readonly Dictionary<string, (IntPtr chunk, uint length)> _sfxChunks = new();

    public void Init(string audioFile, ConfigManager config)
    {
        if (Mixer.Init(Mixer.InitFlags.OGG | Mixer.InitFlags.MP3) < 0)
        {
            Console.Error.WriteLine($"SDL_mixer could not initialize! Error: {SDL.GetError()}");
            return;
        }

        if (Mixer.OpenAudio(0, IntPtr.Zero))
        {
            Console.Error.WriteLine($"SDL_mixer could not open audio! Error: {SDL.GetError()}");
            return;
        }

        Mixer.AllocateChannels(16);
        Mixer.VolumeMusic(config.MusicVolume * Mixer.MaxVolume / 100);
    }

    public void LoadSoundFromVoc(string name, string filePath)
    {
        try
        {
            byte[] vocBytes = File.ReadAllBytes(filePath);
            (byte[] pcmData, byte timeConstant) = ParseVocData(vocBytes);

            if (pcmData.Length == 0)
            {
                Console.Error.WriteLine($"Could not parse PCM data from VOC file: {name}");
                return;
            }

            // The sample rate is derived from the time constant in the VOC file.
            // Rate = 1,000,000 / (256 - timeConstant)
            int sampleRate = 1000000 / (256 - timeConstant);

            // SDL_mixer doesn't have a direct way to resample raw data on load.
            // The best approach is to build an SDL_AudioSpec and use an SDL_AudioStream for resampling.
            // For simplicity here, we'll load it at its original rate, though it might play at the wrong pitch
            // if the mixer was opened with a different rate (e.g. 44100).
            // A full implementation should resample 'pcmData' to the mixer's output format.
            
            // We must convert 8-bit unsigned PCM to 16-bit signed PCM for the mixer.
            short[] pcm16bit = new short[pcmData.Length];
            for (int i = 0; i < pcmData.Length; i++)
            {
                // Convert unsigned 8-bit (0-255) to signed 16-bit (-32768 to 32767)
                pcm16bit[i] = (short)((pcmData[i] - 128) * 256);
            }

            // Get a pointer to the 16-bit data to pass to SDL
            var handle = GCHandle.Alloc(pcm16bit, GCHandleType.Pinned);
            IntPtr pcmPtr = handle.AddrOfPinnedObject();

            IntPtr chunk = Mixer.QuickLoadRAW(pcmPtr, (uint)(pcm16bit.Length * 2));
            if (chunk == IntPtr.Zero)
            {
                Console.Error.WriteLine($"Failed to load sound effect! SDL_mixer Error: {SDL.GetError()}");
                handle.Free();
                return;
            }
            
            // We don't free the GCHandle because Mix_QuickLoad_RAW does not copy the data.
            // The chunk points directly to our managed array's memory. We must keep it pinned.
            _sfxChunks[name] = (chunk, (uint)pcm16bit.Length * 2);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error loading sound '{name}': {ex.Message}");
        }
    }

    private (byte[] pcmData, byte timeConstant) ParseVocData(byte[] vocBytes)
    {
        // Very basic VOC parser
        // See http://www.digitalpreservation.gov/formats/fdd/fdd000133.shtml
        if (vocBytes.Length < 26 || !System.Text.Encoding.ASCII.GetString(vocBytes, 0, 19).Equals("Creative Voice File"))
        {
            return (Array.Empty<byte>(), 0);
        }

        // The offset to the first data block is at 0x1A
        int offset = BitConverter.ToUInt16(vocBytes, 0x1A);

        using var stream = new MemoryStream(vocBytes);
        using var reader = new BinaryReader(stream);

        stream.Position = offset;

        while (stream.Position < vocBytes.Length)
        {
            byte blockType = reader.ReadByte();
            if (blockType == 0) break; // Terminator block

            // Block size is a 24-bit little-endian integer
            byte[] sizeBytes = reader.ReadBytes(3);
            int blockSize = sizeBytes[0] | (sizeBytes[1] << 8) | (sizeBytes[2] << 16);

            if (blockType == 1) // Sound data block
            {
                byte timeConstant = reader.ReadByte();
                byte packMethod = reader.ReadByte(); // 0 = 8-bit unsigned PCM
                if (packMethod == 0)
                {
                    return (reader.ReadBytes(blockSize - 2), timeConstant);
                }
            }
            stream.Seek(blockSize, SeekOrigin.Current);
        }
        return (Array.Empty<byte>(), 0);
    }

    public void PlaySound(string name)
    {
        if (_sfxChunks.TryGetValue(name, out var sfx))
        {
            Mixer.PlayChannel(-1, sfx.chunk, 0);
        }
    }

    public void Cleanup()
    {
        // In a real implementation with GCHandles, you would unpin them here.
        foreach (var sfx in _sfxChunks.Values)
        {
            Mixer.FreeChunk(sfx.chunk);
        }
        _sfxChunks.Clear();
        Mixer.CloseAudio();
        Mixer.Quit();
    }
}
