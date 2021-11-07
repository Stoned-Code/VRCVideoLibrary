using HarmonyLib;
using VRCSDK2;
using VRC.SDK3.Video.Components.Base;
using MelonLoader;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Video.Components;

namespace VideoLibrary
{
    internal class Patches
    {
        private static VRC_SyncVideoPlayer sdk2Player;
        private static VRCUnityVideoPlayer sdk3Player;

        public static VRC_SyncVideoPlayer m_sdk2Player => sdk2Player;
        public static VRCUnityVideoPlayer m_sdk3Player => sdk3Player;

        public static void SetSDK2Player(VRC_SyncVideoPlayer player) => sdk2Player = player;
        public static void SetBaseVRCVideoPlayer(VRCUnityVideoPlayer player) => sdk3Player = player;

        [HarmonyPatch(typeof(VRC_SyncVideoPlayer), "Awake")]
        internal class VRCVideoPlayerPatch
        {
            private static void Postfix(VRC_SyncVideoPlayer __instance)
            {
                Patches.SetSDK2Player(__instance);
            }
        }

        [HarmonyPatch(typeof(VRCUnityVideoPlayer), "Start")]
        internal class BaseVRCVideoPlayerPatch
        {
            private static void Postfix(VRCUnityVideoPlayer __instance)
            {
                Patches.SetBaseVRCVideoPlayer(__instance);
            }
        }
    }
}
