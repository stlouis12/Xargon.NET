using System;
using System.Collections.Concurrent;
using System.IO;
using OpenTK.Audio.OpenAL;
using static OpenTK.Audio.OpenAL.AL;
using static OpenTK.Audio.OpenAL.ALC;
using Xargon.NET.Core;

namespace Xargon.NET.Audio;

public class SoundManager : IDisposable
{
    private readonly ConcurrentDictionary<string, int> _bufferCache = new();
    private readonly ConcurrentDictionary<string, int> _sourceCache = new();

    public SoundManager(Core.ConfigManager config)
    {
        try
        {
            //string device = GetString(ALDevice.);
            //Console.WriteLine($"OpenAL Info: Device = {device}");
            //AlcError alcError = GetError();
            //if (alcError != AlcError.NoError)
            //{
            //    Console.WriteLine($"ALC Error: {alcError}");
            //}

            // Set initial volume from config
            SetVolume(config.MusicVolume);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to initialize OpenAL: {ex.Message}");
        }
    }

    public void LoadSoundFromVoc(string name, string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"VOC file not found: {filePath}");
            return;
        }

        try
        {
            byte[] vocBytes = File.ReadAllBytes(filePath);
            (byte[] pcmData, int sampleRate) = ParseVocData(vocBytes);

            if (pcmData.Length == 0)
            {
                Console.Error.WriteLine($"Could not parse PCM data from VOC file: {name}");
                return;
            }

            int buffer = GenBuffer();
            BufferData(buffer, GetSoundFormat(1, 8), pcmData, sampleRate);

            _bufferCache[name] = buffer;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error loading sound '{name}': {ex.Message}");
        }
    }

    private static ALFormat GetSoundFormat(int channels, int bits)
    {
        switch (channels)
        {
            case 1: return bits == 8 ? ALFormat.Mono8 : ALFormat.Mono16;
            case 2: return bits == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16;
            default: throw new Exception("Invalid channel count");
        }
    }

    private (byte[] pcmData, int sampleRate) ParseVocData(byte[] vocBytes)
    {
        if (vocBytes.Length < 26 || !System.Text.Encoding.ASCII.GetString(vocBytes, 0, 19).Equals("Creative Voice File"))
        {
            return (Array.Empty<byte>(), 0);
        }

        using var stream = new MemoryStream(vocBytes);
        using var reader = new BinaryReader(stream);

        stream.Position = 26;
        ushort dataOffset = reader.ReadUInt16();
        stream.Position = dataOffset;

        while (stream.Position < vocBytes.Length)
        {
            byte blockType = reader.ReadByte();
            if (blockType == 0) break;
            byte[] sizeBytes = reader.ReadBytes(3);
            int blockSize = sizeBytes[0] | (sizeBytes[1] << 8) | (sizeBytes[2] << 16);

            if (blockType == 1)
            {
                byte timeConstant = reader.ReadByte();
                byte packMethod = reader.ReadByte();

                if (packMethod == 0)
                {
                    int sampleRate = 1000000 / (256 - timeConstant);
                    byte[] pcmData = reader.ReadBytes(blockSize - 2);
                    return (pcmData, sampleRate);
                }
            }
            stream.Seek(blockSize, SeekOrigin.Current);
        }
        return (Array.Empty<byte>(), 0);
    }

    public void PlaySound(string name)
    {
        if (!_bufferCache.TryGetValue(name, out int buffer))
        {
            Console.Error.WriteLine($"Sound effect '{name}' not loaded.");
            return;
        }

        if (!_sourceCache.TryGetValue(name, out int source))
        {
            source = GenSource();
            Source(source, ALSourcei.Buffer, buffer);
            _sourceCache[name] = source;
        }

        Source(source, ALSourceb.Looping, false);
        SourcePlay(source);
    }

    public void PlayTune(string filename)
    {
        // TODO: Implement music playback using OpenAL or a separate library.
    }

    public void StopMusic()
    {
        // TODO: Implement music stopping logic.
    }

    public void SetVolume(float volume)
    {
        Listener(ALListenerf.Gain, volume);
    }

    public void Dispose()
    {
        // Delete all OpenAL resources
        foreach (int source in _sourceCache.Values)
        {
            SourceStop(source);
            DeleteSource(source);
        }
        _sourceCache.Clear();

        foreach (int buffer in _bufferCache.Values)
        {
            DeleteBuffer(buffer);
        }
        _bufferCache.Clear();

        // Clean up the OpenAL context
        //Add cleanup if needed
    }
}
