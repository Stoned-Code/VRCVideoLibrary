using MelonLoader;
using RubyButtonAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VRC;
using VRC.Core;
using VRC.SDK3.Video.Components;
using VRCSDK2;

namespace VideoLibrary
{
    public class ModVideo : IComparable<ModVideo>
    {
        public ModVideo(string videoName, string videoLink, int videoNumber = 0, int indexNumber = 0)
        {
            this.VideoName = videoName;
            this.VideoLink = videoLink;
            this.VideoNumber = videoNumber;
            this.IndexNumber = indexNumber;
        }

        public const int waitInterval = 15;
        public const int cooldown = 30;
        public string VideoName { get; set; }
        public string VideoLink { get; set; }
        public int VideoNumber { get; set; }
        public int IndexNumber { get; set; }
        public QMSingleButton VideoButton { get; set; }
        static bool videoPlayerActive
        {
            get
            {
                bool videoPlayerActive;
                try
                {
                    var videoPlayer = GameObject.FindObjectOfType<VRC_SyncVideoPlayer>();
                    var syncVideoPlayer = GameObject.FindObjectOfType<SyncVideoPlayer>();
                    var udonPlayer = GameObject.FindObjectOfType<VRCUnityVideoPlayer>();

                    if (videoPlayer != null || udonPlayer != null || syncVideoPlayer != null)
                    {
                        return true;
                    }

                    else
                    {
                        return false;
                    }
                }

                catch (Exception)
                {
                    videoPlayerActive = false;
                    return videoPlayerActive;
                }
            }
        }
        static bool isMaster
        {
            get
            {
                var playerList = PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0;

                foreach (Player player in playerList)
                {
                    var playerApi = player.prop_VRCPlayerApi_0;
                    var apiUser = player.prop_APIUser_0;

                    if (playerApi.isMaster)
                    {
                        if (apiUser.id == APIUser.CurrentUser.id)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }
        static bool friendsWithMaster
        {
            get
            {
                var playerManager = PlayerManager.field_Private_Static_PlayerManager_0.prop_ArrayOf_Player_0;

                for (int i = 0; i < playerManager.Length; i++)
                {
                    var player = playerManager[i];
                    var apiUser = player.prop_APIUser_0;
                    var isFriends = IsFriendsWith(apiUser.id);

                    if (!player.prop_VRCPlayerApi_0.isMaster) continue;
                    if (isFriends) return true;
                }

                return false;
            }
        }
        private static bool IsFriendsWith(string id)
        {
            return APIUser.CurrentUser.friendIDs.Contains(id);
        }

        public int CompareTo(ModVideo other)
        {
            return this.VideoName.CompareTo(other.VideoName);
        }

        public void DestroyButton()
        {
            VideoButton.DestroyMe();
        }

        public void AddVideo(bool onCooldown)
        {
            MelonCoroutines.Start(AddVid(onCooldown));
        }

        private IEnumerator AddVid(bool onCooldown)
        {
            var friendsWithCreator = true;

            if (videoPlayerActive)
            {
                if (isMaster || friendsWithCreator || friendsWithMaster)
                {
                    if (!onCooldown)
                    {
                        var sdk2Player = Patches.m_sdk2Player;
                        var sdk3Player = Patches.m_sdk3Player;

                        VideoPlayerType playerType = VideoPlayerType.None;

                        if (sdk2Player != null) playerType = VideoPlayerType.ClassicPlayer;
                        else if (sdk3Player != null) playerType = VideoPlayerType.UdonPlayer;


                        if (playerType == VideoPlayerType.ClassicPlayer)
                        {
                            sdk2Player.Clear();
                            sdk2Player.AddURL(VideoLink);

                            VRCUiManager.prop_VRCUiManager_0.field_Private_List_1_String_0.Add($"Wait {waitInterval} seconds\nfor video to play");

                            yield return new WaitForSeconds(waitInterval);

                            sdk2Player.Next();
                        }

                        else if (playerType == VideoPlayerType.UdonPlayer)
                        {
                            VRCUiManager.prop_VRCUiManager_0.field_Private_List_1_String_0.Add($"Wait {waitInterval} seconds\nfor video to play");
                            yield return new WaitForSeconds(waitInterval);
                            sdk3Player.PlayURL(new VRC.SDKBase.VRCUrl(VideoLink));
                        }
                    }

                    else
                    {
                        VRCUiManager.prop_VRCUiManager_0.field_Private_List_1_String_0.Add($"Video Library is on {cooldown} second cooldown");
                    }
                }

                else
                {
                    VRCUiManager.prop_VRCUiManager_0.field_Private_List_1_String_0.Add("Only the master and their friends can set videos...");
                }
            }

            else
                VRCUiManager.prop_VRCUiManager_0.field_Private_List_1_String_0.Add("No active video player...");
        }

        public void GetLink()
        {
            System.Windows.Forms.Clipboard.SetText(VideoLink);
            VRCUiManager.prop_VRCUiManager_0.field_Private_List_1_String_0.Add("Video link copied to system clipboard");
        }

        public static IEnumerator VideoFromClipboard(bool onCooldown)
        {

            if (videoPlayerActive)
            {
                if (isMaster)
                {
                    if (!onCooldown)
                    {
                        var sdk2Player = Patches.m_sdk2Player;
                        var sdk3Player = Patches.m_sdk3Player;

                        VideoPlayerType playerType = VideoPlayerType.None;

                        if (sdk2Player != null) playerType = VideoPlayerType.ClassicPlayer;
                        else if (sdk3Player != null) playerType = VideoPlayerType.UdonPlayer;


                        if (playerType == VideoPlayerType.ClassicPlayer)
                        {
                            sdk2Player.Clear();
                            sdk2Player.AddURL(System.Windows.Forms.Clipboard.GetText());

                            VRCUiManager.prop_VRCUiManager_0.field_Private_List_1_String_0.Add($"Wait {waitInterval} seconds\nfor video to play");

                            yield return new WaitForSeconds(waitInterval);

                            sdk2Player.Next();
                        }

                        else if (playerType == VideoPlayerType.UdonPlayer)
                        {
                            VRCUiManager.prop_VRCUiManager_0.field_Private_List_1_String_0.Add($"Wait {waitInterval} seconds\nfor video to play");
                            yield return new WaitForSeconds(waitInterval);
                            sdk3Player.PlayURL(new VRC.SDKBase.VRCUrl(System.Windows.Forms.Clipboard.GetText()));
                        }
                    }

                    else
                    {
                        VRCUiManager.prop_VRCUiManager_0.field_Private_List_1_String_0.Add($"Video Library is on {cooldown} second cooldown");
                    }
                }

                else
                {
                    VRCUiManager.prop_VRCUiManager_0.field_Private_List_1_String_0.Add("Only the master can set videos...");
                }
            }
        }

        public enum VideoPlayerType
        {
            UdonPlayer,
            ClassicPlayer,
            None,
            SyncPlayer
        }
    }
}
