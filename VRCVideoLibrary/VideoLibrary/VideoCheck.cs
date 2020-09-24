using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using System.IO;
using VRCSDK2;
using VRC;
using VRC.Core;
using System.Collections;
using RubyButtonAPI;

namespace VideoLibrary
{
    class VideoCheck
    {

    }

    public class ModVideo : IComparable<ModVideo>
    {
        public string VideoName { get; set; }
        public string VideoLink { get; set; }
        public int VideoNumber { get; set; }
        public int IndexNumber { get; set; }
        public QMSingleButton VideoButton { get; set; }

        public int CompareTo(ModVideo other)
        {
            return this.VideoName.CompareTo(other.VideoName);
        }

        public IEnumerator AddVideo()
        {
            var videoPlayerActive = VideoPlayerCheck();
            var isMaster = MasterCheck(APIUser.CurrentUser.id);

            if (videoPlayerActive)
            {
                if (isMaster)
                {
                    var videoPlayer = GameObject.FindObjectOfType<VRC_SyncVideoPlayer>();

                    videoPlayer.Clear();
                    videoPlayer.AddURL(VideoLink);

                    VRCUiManager.field_Protected_Static_VRCUiManager_0.field_Private_List_1_String_0.Add("Wait 10 seconds for video to play");

                    yield return new WaitForSeconds(10);

                    videoPlayer.Next();
                }

                else
                {
                    VRCUiManager.field_Protected_Static_VRCUiManager_0.field_Private_List_1_String_0.Add("Only the master can set videos...");
                }
            }
        }

        public static bool MasterCheck(string UserID)
        {
            bool isMaster = false;
            var playerList = PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0;

            foreach (Player player in playerList)
            {
                var playerApi = player.prop_VRCPlayerApi_0;
                var apiUser = player.field_Private_APIUser_0;

                if (playerApi.isMaster)
                {
                    if (apiUser.id == UserID)
                    {
                        isMaster = true;
                        break;
                    }

                    else
                    {
                        isMaster = false;
                        break;
                    }
                }
            }

            return isMaster;
        }

        public static bool VideoPlayerCheck()
        {
            bool videoPlayerActive;
            try
            {
                var videoPlayer = GameObject.FindObjectOfType<VRC_SyncVideoPlayer>();

                if (videoPlayer != null)
                {
                    videoPlayerActive = true;
                    return videoPlayerActive;
                }

                else
                {
                    videoPlayerActive = false;
                    return videoPlayerActive;
                }
            }

            catch (Exception)
            {
                videoPlayerActive = false;
                return videoPlayerActive;
            }
        }
    }
}
