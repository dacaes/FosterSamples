using static SDL3.SDL;
using System.Diagnostics;
using TinyLink;

namespace Teca;

public static class Audio
{
    public static void InitAudio()
    {
        if (SDL_Init(SDL_InitFlags.SDL_INIT_AUDIO) != true)
        {
            LogInfo(SDL_GetError());
            return;
        }
    }

    public static void QuitAudio()
    {
        SDL_Quit();
    }

    public static void PlaySound(string name)
    {
        PlaySoundByPath(Assets.Sounds[name].Path);
    }

    public static void PlaySoundByPath(string path)
    {
        SDL_AudioSpec spec;
        IntPtr audioBuf;
        uint audioLen;

        if (SDL_LoadWAV(path, out spec, out audioBuf, out audioLen) != true)
        {
            LogInfo("Failed to load WAV: " + SDL_GetError());
            return;
        }

        // Get default playback device
        int deviceCount;
        IntPtr devices = SDL_GetAudioPlaybackDevices(out deviceCount);

        if (deviceCount == 0)
        {
            LogInfo("No audio devices found");
            SDL_free(audioBuf);
            return;
        }

        uint deviceId;
        unsafe
        {
            uint* devicePtr = (uint*)devices;
            deviceId = devicePtr[0];
        }

        IntPtr stream = SDL_OpenAudioDeviceStream(deviceId, ref spec, null!, IntPtr.Zero);

        if (stream == IntPtr.Zero)
        {
            LogInfo("Failed to open stream: " + SDL_GetError());
            SDL_free(audioBuf);
            SDL_free(devices);
            return;
        }

        SDL_ResumeAudioStreamDevice(stream);

        SDL_PutAudioStreamData(stream, audioBuf, (int)audioLen);
        SDL_FlushAudioStream(stream);

        // LogInfo($"Playing {path}");

        SDL_free(audioBuf);
        SDL_free(devices);
    }

    [Conditional("DEBUG")]
    public static void LogInfo(string message)
    {
        Console.WriteLine(message);
    }
}
