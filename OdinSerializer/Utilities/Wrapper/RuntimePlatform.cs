#if GODOT
using System.ComponentModel;

namespace OdinSerializer.Utilities.Wrapper;
public enum RuntimePlatform
{
    OSXEditor = 0,
    OSXPlayer = 1,
    WindowsPlayer = 2,
    [Obsolete("WebPlayer export is no longer supported in Unity 5.4+.", true)] OSXWebPlayer = 3,
    [Obsolete("Dashboard widget on Mac OS X export is no longer supported in Unity 5.4+.", true)] OSXDashboardPlayer = 4,
    [Obsolete("WebPlayer export is no longer supported in Unity 5.4+.", true)] WindowsWebPlayer = 5,
    WindowsEditor = 7,
    IPhonePlayer = 8,
    [Obsolete("PS3 export is no longer supported in Unity >=5.5.")] PS3 = 9,
    [Obsolete("Xbox360 export is no longer supported in Unity 5.5+.")] XBOX360 = 10, // 0x0000000A
    Android = 11, // 0x0000000B
    [Obsolete("NaCl export is no longer supported in Unity 5.0+.")] NaCl = 12, // 0x0000000C
    LinuxPlayer = 13, // 0x0000000D
    [Obsolete("FlashPlayer export is no longer supported in Unity 5.0+.")] FlashPlayer = 15, // 0x0000000F
    LinuxEditor = 16, // 0x00000010
    WebGLPlayer = 17, // 0x00000011
    [Obsolete("Use WSAPlayerX86 instead")] MetroPlayerX86 = 18, // 0x00000012
    WSAPlayerX86 = 18, // 0x00000012
    [Obsolete("Use WSAPlayerX64 instead")] MetroPlayerX64 = 19, // 0x00000013
    WSAPlayerX64 = 19, // 0x00000013
    [Obsolete("Use WSAPlayerARM instead")] MetroPlayerARM = 20, // 0x00000014
    WSAPlayerARM = 20, // 0x00000014
    [Obsolete("Windows Phone 8 was removed in 5.3")] WP8Player = 21, // 0x00000015
    [EditorBrowsable(EditorBrowsableState.Never), Obsolete("BB10Player export is no longer supported in Unity 5.4+.")] BB10Player = 22, // 0x00000016
    [Obsolete("BlackBerryPlayer export is no longer supported in Unity 5.4+.")] BlackBerryPlayer = 22, // 0x00000016
    [Obsolete("TizenPlayer export is no longer supported in Unity 2017.3+.")] TizenPlayer = 23, // 0x00000017
    [Obsolete("PSP2 is no longer supported as of Unity 2018.3")] PSP2 = 24, // 0x00000018
    PS4 = 25, // 0x00000019
    [Obsolete("PSM export is no longer supported in Unity >= 5.3")] PSM = 26, // 0x0000001A
    XboxOne = 27, // 0x0000001B
    [Obsolete("SamsungTVPlayer export is no longer supported in Unity 2017.3+.")] SamsungTVPlayer = 28, // 0x0000001C
    [Obsolete("Wii U is no longer supported in Unity 2018.1+.")] WiiU = 30, // 0x0000001E
    tvOS = 31, // 0x0000001F
    Switch = 32, // 0x00000020
    Lumin = 33, // 0x00000021
    Stadia = 34, // 0x00000022
    CloudRendering = 35, // 0x00000023
    [Obsolete("GameCoreScarlett is deprecated, please use GameCoreXboxSeries (UnityUpgradable) -> GameCoreXboxSeries", false)] GameCoreScarlett = 36, // 0x00000024
    GameCoreXboxSeries = 36, // 0x00000024
    GameCoreXboxOne = 37, // 0x00000025
    PS5 = 38,
}
#endif