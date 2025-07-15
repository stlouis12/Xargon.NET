using System.IO;
using System.Runtime.InteropServices;
using Xargon.NET.Helpers;

namespace Xargon.NET.Core;

public class ConfigManager
{
    // This structure mirrors the config structure from the original config.xr1
    // IMPORTANT: This is a GUESS. You must verify and adjust this against
    // the actual binary file layout of config.xr1.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private unsafe struct ConfigData
    {
        public short SoundFXVolume;
        public short MusicVolume;
        public short ControlType; 
        public short Difficulty;
        public short ScreenSize; 
        public fixed short Reserved[10]; 
    }

    private ConfigData _config;

    public short SoundFXVolume => _config.SoundFXVolume;
    public short MusicVolume => _config.MusicVolume;
    public short ControlType => _config.ControlType;
    public short Difficulty => _config.Difficulty;
    public short ScreenSize => _config.ScreenSize;

    public void LoadConfig(string filename)
    {
        string path = Path.Combine("Assets", filename);
        if (!File.Exists(path))
        {
             Console.WriteLine($"Config file '{path}' not found. Using default settings.");
             LoadDefaultConfig();
             return;
        }

        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);

            _config = reader.ReadStruct<ConfigData>();

            // Validate settings
            _config.SoundFXVolume = Math.Clamp(_config.SoundFXVolume, (short)0, (short)100);
            _config.MusicVolume = Math.Clamp(_config.MusicVolume, (short)0, (short)100);

            Console.WriteLine($"Loaded config from {filename}: SoundFXVolume={_config.SoundFXVolume}, MusicVolume={_config.MusicVolume}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error loading config: {ex.Message}. Using default settings.");
            LoadDefaultConfig();
        }
    }

    private void LoadDefaultConfig()
    {
        _config = new ConfigData
        {
            SoundFXVolume = 80,
            MusicVolume = 60,
            ControlType = 0, // Keyboard
            Difficulty = 1,  // Normal
            ScreenSize = 0   // Windowed
        };
    }

    public void SaveConfig(string filename)
    {
        string path = Path.Combine("Assets", filename);
        try
        {
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(fs);
            writer.WriteStruct(_config);
            Console.WriteLine($"Saved config to {filename}");
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine($"Error saving config: {ex.Message}");
        }
    }
}
