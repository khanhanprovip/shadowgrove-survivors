param([int]$TargetProcessId = 0, [int]$WatchSeconds = 0, [switch]$RestoreWindowsOutput)
$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @'
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
[ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class GameAudioEnumerator {}
[ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IGameDevices {
 [PreserveSig] int EnumAudioEndpoints(int f,uint m,out IntPtr d);
 [PreserveSig] int GetDefaultAudioEndpoint(int f,int r,out IGameDevice d);
}
[ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IGameDevice { [PreserveSig] int Activate(ref Guid id,uint ctx,IntPtr p,[MarshalAs(UnmanagedType.IUnknown)] out object o); }
[ComImport,Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IGameSessions {
 [PreserveSig] int GetAudioSessionControl(ref Guid id,uint flags,out IntPtr c);
 [PreserveSig] int GetSimpleAudioVolume(ref Guid id,uint flags,out IntPtr v);
 [PreserveSig] int GetSessionEnumerator(out IGameSessionList list);
}
[ComImport,Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IGameSessionList {
 [PreserveSig] int GetCount(out int count);
 [PreserveSig] int GetSession(int index,out IGameSession session);
}
[ComImport,Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IGameSession {
 [PreserveSig] int GetState(out int state);
 [PreserveSig] int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string name);
 [PreserveSig] int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name,Guid ctx);
 [PreserveSig] int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string path);
 [PreserveSig] int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string path,Guid ctx);
 [PreserveSig] int GetGroupingParam(out Guid id);
 [PreserveSig] int SetGroupingParam(Guid id,Guid ctx);
 [PreserveSig] int RegisterAudioSessionNotification(IntPtr n);
 [PreserveSig] int UnregisterAudioSessionNotification(IntPtr n);
 [PreserveSig] int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string id);
 [PreserveSig] int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string id);
 [PreserveSig] int GetProcessId(out uint id);
}
[ComImport,Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IGameVolume {
 [PreserveSig] int SetMasterVolume(float value,Guid ctx);
 [PreserveSig] int GetMasterVolume(out float value);
 [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute,Guid ctx);
 [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
}
[ComImport,Guid("5CDF2C82-841E-4546-9722-0CF74078229A"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IGameEndpointVolume {
 [PreserveSig] int RegisterControlChangeNotify(IntPtr n);
 [PreserveSig] int UnregisterControlChangeNotify(IntPtr n);
 [PreserveSig] int GetChannelCount(out uint n);
 [PreserveSig] int SetMasterVolumeLevel(float v,Guid c);
 [PreserveSig] int SetMasterVolumeLevelScalar(float v,Guid c);
 [PreserveSig] int GetMasterVolumeLevel(out float v);
 [PreserveSig] int GetMasterVolumeLevelScalar(out float v);
 [PreserveSig] int SetChannelVolumeLevel(uint n,float v,Guid c);
 [PreserveSig] int SetChannelVolumeLevelScalar(uint n,float v,Guid c);
 [PreserveSig] int GetChannelVolumeLevel(uint n,out float v);
 [PreserveSig] int GetChannelVolumeLevelScalar(uint n,out float v);
 [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute,Guid c);
 [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
}
public static class GameAudioOnly {
 public static string Apply(bool restoreWindows,int targetPid) {
  var devices=(IGameDevices)new GameAudioEnumerator(); IGameDevice device;
  Marshal.ThrowExceptionForHR(devices.GetDefaultAudioEndpoint(0,1,out device));
  Guid id=typeof(IGameSessions).GUID; object obj;
  Marshal.ThrowExceptionForHR(device.Activate(ref id,23,IntPtr.Zero,out obj));
  var manager=(IGameSessions)obj; IGameSessionList list; int count; int mutedCount=0;
  Marshal.ThrowExceptionForHR(manager.GetSessionEnumerator(out list));
  Marshal.ThrowExceptionForHR(list.GetCount(out count));
  for(int i=0;i<count;i++) {
   IGameSession session; Marshal.ThrowExceptionForHR(list.GetSession(i,out session)); uint pid;
   if(session.GetProcessId(out pid)!=0 || pid==0) continue;
   bool matches=targetPid>0 ? pid==targetPid : false;
   if(targetPid==0) { try { matches=Process.GetProcessById((int)pid).ProcessName.IndexOf("Shadowgrove",StringComparison.OrdinalIgnoreCase)>=0; } catch(ArgumentException) {} }
   if(!matches) continue;
   var volume=(IGameVolume)session;
   Marshal.ThrowExceptionForHR(volume.SetMute(true,Guid.Empty)); bool mute;
   Marshal.ThrowExceptionForHR(volume.GetMute(out mute)); if(mute) mutedCount++;
  }
  id=typeof(IGameEndpointVolume).GUID;
  Marshal.ThrowExceptionForHR(device.Activate(ref id,23,IntPtr.Zero,out obj));
  var endpoint=(IGameEndpointVolume)obj;
  if(restoreWindows) Marshal.ThrowExceptionForHR(endpoint.SetMute(false,Guid.Empty));
  bool systemMuted; Marshal.ThrowExceptionForHR(endpoint.GetMute(out systemMuted));
  return "{\"mutedGameSessions\":"+mutedCount+",\"windowsOutputMuted\":"+systemMuted.ToString().ToLowerInvariant()+"}";
 }
}
'@
$until = [DateTime]::UtcNow.AddSeconds($WatchSeconds)
do {
    [GameAudioOnly]::Apply($RestoreWindowsOutput.IsPresent, $TargetProcessId)
    $RestoreWindowsOutput = $false
    if ($WatchSeconds -gt 0) { Start-Sleep -Milliseconds 400 }
} while ([DateTime]::UtcNow -lt $until)
