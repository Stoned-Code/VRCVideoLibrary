using MelonLoader;
using RubyButtonAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UIExpansionKit.API;
using UnityEngine;
using UnityEngine.UI;
using VRC;
using VRC.Core;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.Base;
using VRCSDK2;

namespace VideoLibrary
{
    internal static class LibraryBuildInfo
    {
        public const string modName = "VRCVideoLibrary";
        public const string modVersion = "1.1.5";
        public const string modAuthor = "UHModz";
        public const string modDownload = "https://github.com/UshioHiko/VRCVideoLibrary/releases";
    }

    public class VideoLib : MelonMod
    {
        protected List<ModVideo> videoList;
 
        private int indexNumber = 0;
        private int currentMenuIndex;
        private bool onCooldown = false;
        private bool getLink = false;

        private bool libraryInitialized = false;

        private string videoDirectory;

        private QMNestedButton videoLibrary;

        private QMSingleButton previousButton;
        private QMSingleButton nextButton;

        //////////////////////
        //  VRChat Methods  //
        //////////////////////

        public override void OnApplicationStart()
        {
            videoList = new List<ModVideo>();
            MelonCoroutines.Start(InitializeLibrary());
        }

        public override void VRChat_OnUiManagerInit()
        {
            videoLibrary = new QMNestedButton("ShortcutMenu", -10, 0, "", "", null, null, null, null);
            videoLibrary.getMainButton().getGameObject().GetComponentInChildren<Image>().enabled = false;
            videoLibrary.getMainButton().getGameObject().GetComponentInChildren<Text>().enabled = false;
            
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.QuickMenu).AddSimpleButton("Video\nLibrary", delegate
            {
                videoLibrary.getMainButton().getGameObject().GetComponent<Button>().onClick.Invoke();
            });

            MelonCoroutines.Start(LoadMenu());
        }

        ///////////////////////
        //  Library Methods  //
        ///////////////////////

        public IEnumerator InitializeLibrary()
        {
            while (NetworkManager.field_Internal_Static_NetworkManager_0 == null) yield return null;
            while (VRCUiManager.prop_VRCUiManager_0 == null) yield return null;

            string exampleVideo = "Example Name|https://youtu.be/pKO9UjSeLew";

            var rootDirectory = Environment.CurrentDirectory;
            var oldSubDirectory = Path.Combine(rootDirectory, "UHModz"); // Remove in future.
            var subDirectory = Path.Combine(rootDirectory, "UserData", "UHModz");

            
            // Remove in future.
            if (Directory.Exists(oldSubDirectory))
                Directory.Move(oldSubDirectory, subDirectory);

            videoDirectory = Path.Combine(subDirectory, "Videos.txt");
            string msg = "Created UHModz Directory!";
            if (!Directory.Exists(subDirectory))
            {
                Directory.CreateDirectory(subDirectory);
                MelonLogger.Msg(msg);
            }

            if (!File.Exists(videoDirectory))
            {
                using (StreamWriter sw = File.CreateText(videoDirectory))
                {
                    sw.WriteLine(exampleVideo);
                    sw.Close();
                }
            }

            GetVideoLibrary();

            libraryInitialized = true;
        }

        private IEnumerator LoadMenu()
        {
            while (!libraryInitialized) yield return null;
            var indexButton = new QMSingleButton(videoLibrary, 4, 1, "Page:\n" + (currentMenuIndex + 1).ToString() + " of " + (indexNumber + 1).ToString(), delegate { }, "", Color.clear, Color.yellow);
            indexButton.getGameObject().GetComponentInChildren<Button>().enabled = false;
            indexButton.getGameObject().GetComponentInChildren<Image>().enabled = false;

            previousButton = new QMSingleButton(videoLibrary, 4, 0, "Previous\nPage", delegate
            {
                if (currentMenuIndex != 0)
                {
                    currentMenuIndex--;
                }

                foreach (ModVideo videoButton in videoList)
                {
                    if (videoButton.IndexNumber != currentMenuIndex)
                    {
                        videoButton.VideoButton.setActive(false);
                    }

                    else
                    {
                        videoButton.VideoButton.setActive(true);
                    }
                }
                indexButton.setButtonText("Page:\n" + (currentMenuIndex + 1).ToString() + " of " + (indexNumber + 1).ToString());
            }, "Previous video page", null, null);

            nextButton = new QMSingleButton(videoLibrary, 4, 2, "Next\nPage", delegate
            {
                if (currentMenuIndex != indexNumber)
                {
                    currentMenuIndex++;
                }

                foreach (ModVideo videoButton in videoList)
                {
                    if (videoButton.IndexNumber != currentMenuIndex)
                    {
                        videoButton.VideoButton.setActive(false);
                    }

                    else
                    {
                        videoButton.VideoButton.setActive(true);
                    }
                }
                indexButton.setButtonText("Page:\n" + (currentMenuIndex + 1).ToString() + " of " + (indexNumber + 1).ToString());
            }, "Previous video page", null, null);

            var videoFromClipboard = new QMSingleButton(videoLibrary, 1, -2, "Video From\nClipboard", delegate
            {
                MelonCoroutines.Start(ModVideo.VideoFromClipboard(onCooldown));
            }, "Puts the link in your system clipboard into the world's video player");

            var openListButton = new QMSingleButton(videoLibrary, 2, -2, "Open\nLibrary\nDocument", delegate
            {
                OpenVideoLibrary();
            }, "Opens the Video Library text document\nLibrary Format: \"Button Name|Video Url\"", null, null);

            var openReadMe = new QMSingleButton(videoLibrary, 3, -2, "Read\nMe", delegate
            {
                Process.Start("https://github.com/UshioHiko/VRCVideoLibrary/blob/master/README.md");
            }, "Opens a link to the mod's \"Read Me\"");

            var getLinkToggle = new QMToggleButton(videoLibrary, 4, -2, "Buttons Copy\nVideo Link", delegate
            {
                getLink = true;
            }, "Disabled", delegate
            {
                getLink = false;
            }, "Makes video library buttons copy video url to your system clipboard", null, null, false, false);

            for (int i = 0; i < videoList.Count; i++)
            {
                ModVideo video = videoList[i];
                switch (video.VideoNumber)
                {
                    case 0:
                        {
                            var vidButton = new QMSingleButton(videoLibrary, 1, 0, video.VideoName, delegate
                            {
                                if (getLink)
                                {
                                    video.GetLink();
                                }

                                else
                                {
                                    video.AddVideo(onCooldown);

                                    if (!onCooldown)
                                    {
                                        MelonCoroutines.Start(CoolDown());
                                    }
                                }
                            }, $"Puts {video.VideoName} on the video player", null, null);

                            video.VideoButton = vidButton;
                            vidButton.getGameObject().GetComponentInChildren<Text>().resizeTextForBestFit = true;
                            break;
                        }

                    case 1:
                        {
                            var vidButton = new QMSingleButton(videoLibrary, 2, 0, video.VideoName, delegate
                            {
                                if (getLink)
                                {
                                    video.GetLink();
                                }

                                else
                                {
                                    video.AddVideo(onCooldown);

                                    if (!onCooldown)
                                    {
                                        MelonCoroutines.Start(CoolDown());
                                    }
                                }
                            }, $"Puts {video.VideoName} on the video player", null, null);

                            video.VideoButton = vidButton;
                            vidButton.getGameObject().GetComponentInChildren<Text>().resizeTextForBestFit = true;
                            break;
                        }

                    case 2:
                        {
                            var vidButton = new QMSingleButton(videoLibrary, 3, 0, video.VideoName, delegate
                            {
                                if (getLink)
                                {
                                    video.GetLink();
                                }

                                else
                                {
                                    video.AddVideo(onCooldown);

                                    if (!onCooldown)
                                    {
                                        MelonCoroutines.Start(CoolDown());
                                    }
                                }
                            }, $"Puts {video.VideoName} on the video player", null, null);

                            video.VideoButton = vidButton;
                            vidButton.getGameObject().GetComponentInChildren<Text>().resizeTextForBestFit = true;
                            break;
                        }

                    case 3:
                        {
                            var vidButton = new QMSingleButton(videoLibrary, 1, 1, video.VideoName, delegate
                            {
                                if (getLink)
                                {
                                    video.GetLink();
                                }

                                else
                                {
                                    video.AddVideo(onCooldown);

                                    if (!onCooldown)
                                    {
                                        MelonCoroutines.Start(CoolDown());
                                    }
                                }
                            }, $"Puts {video.VideoName} on the video player", null, null);

                            video.VideoButton = vidButton;
                            vidButton.getGameObject().GetComponentInChildren<Text>().resizeTextForBestFit = true;
                            break;
                        }

                    case 4:
                        {
                            var vidButton = new QMSingleButton(videoLibrary, 2, 1, video.VideoName, delegate
                            {
                                if (getLink)
                                {
                                    video.GetLink();
                                }

                                else
                                {
                                    video.AddVideo(onCooldown);

                                    if (!onCooldown)
                                    {
                                        MelonCoroutines.Start(CoolDown());
                                    }
                                }
                            }, $"Puts {video.VideoName} on the video player", null, null);

                            video.VideoButton = vidButton;
                            vidButton.getGameObject().GetComponentInChildren<Text>().resizeTextForBestFit = true;
                            break;
                        }

                    case 5:
                        {
                            var vidButton = new QMSingleButton(videoLibrary, 3, 1, video.VideoName, delegate
                            {
                                if (getLink)
                                {
                                    video.GetLink();
                                }

                                else
                                {
                                    video.AddVideo(onCooldown);

                                    if (!onCooldown)
                                    {
                                        MelonCoroutines.Start(CoolDown());
                                    }
                                }
                            }, $"Puts {video.VideoName} on the video player", null, null);

                            video.VideoButton = vidButton;
                            vidButton.getGameObject().GetComponentInChildren<Text>().resizeTextForBestFit = true;
                            break;
                        }

                    case 6:
                        {
                            var vidButton = new QMSingleButton(videoLibrary, 1, 2, video.VideoName, delegate
                            {
                                if (getLink)
                                {
                                    video.GetLink();
                                }

                                else
                                {
                                    video.AddVideo(onCooldown);

                                    if (!onCooldown)
                                    {
                                        MelonCoroutines.Start(CoolDown());
                                    }
                                }
                            }, $"Puts {video.VideoName} on the video player", null, null);

                            video.VideoButton = vidButton;
                            vidButton.getGameObject().GetComponentInChildren<Text>().resizeTextForBestFit = true;
                            break;
                        }

                    case 7:
                        {
                            var vidButton = new QMSingleButton(videoLibrary, 2, 2, video.VideoName, delegate
                            {
                                if (getLink)
                                {
                                    video.GetLink();
                                }

                                else
                                {
                                    video.AddVideo(onCooldown);

                                    if (!onCooldown)
                                    {
                                        MelonCoroutines.Start(CoolDown());
                                    }
                                }
                            }, $"Puts {video.VideoName} on the video player", null, null);

                            video.VideoButton = vidButton;
                            vidButton.getGameObject().GetComponentInChildren<Text>().resizeTextForBestFit = true;
                            break;
                        }

                    case 8:
                        {
                            var vidButton = new QMSingleButton(videoLibrary, 3, 2, video.VideoName, delegate
                            {
                                if (getLink)
                                {
                                    video.GetLink();
                                }

                                else
                                {
                                    video.AddVideo(onCooldown);

                                    if (!onCooldown)
                                    {
                                        MelonCoroutines.Start(CoolDown());
                                    }
                                }
                            }, $"Puts {video.VideoName} on the video player", null, null);

                            video.VideoButton = vidButton;
                            vidButton.getGameObject().GetComponentInChildren<Text>().resizeTextForBestFit = true;
                            break;
                        }
                }

                if (video.IndexNumber != currentMenuIndex)
                {
                    video.VideoButton.setActive(false);
                }
            }

            if (videoList.Count <= 9)
            {
                previousButton.setIntractable(false);
                nextButton.setIntractable(false);
            }
        }

        public void GetVideoLibrary()
        {
            indexNumber = 0;
            currentMenuIndex = 0;
            StreamReader file = new StreamReader(videoDirectory);


            string line;
            while ((line = file.ReadLine()) != null)
            {
                var lineArray = line.Split('|');
                videoList.Add(new ModVideo(lineArray[0], lineArray[1]));
            }

            file.Close();

            videoList.Sort();

            var videoNumber = 0;

            for (int i = 0; i < videoList.Count; i++)
            {
                var video = videoList[i];

                video.VideoNumber = videoNumber;
                video.IndexNumber = indexNumber;

                videoNumber++;
                if (videoNumber == 9 && i != (videoList.Count - 1))
                {
                    indexNumber++;
                    videoNumber = 0;
                }

                else
                {
                    continue;
                }
            }
        }

        public void OpenVideoLibrary()
        {
            Process.Start(videoDirectory);
        }

        public IEnumerator CoolDown()
        {
            onCooldown = true;
            yield return new WaitForSeconds(30);
            onCooldown = false;
        }

        public IEnumerator LoadIntervalToggle()
        {
            while (APIUser.CurrentUser == null) yield return null;

            var intervalToggle = new QMToggleButton(videoLibrary, 1, 0, "10 Seconds", delegate
            {
                ModVideo.waitIntervalToggle = true;
            }, "30 Seconds", delegate
            {
                ModVideo.waitIntervalToggle = false;
            }, "Changes cooldown interval.", null, null, false, ModVideo.waitIntervalToggle);
        }
    }

    public class ModVideo : IComparable<ModVideo>
    {
        public ModVideo(string videoName, string videoLink, int videoNumber = 0, int indexNumber = 0)
        {
            this.VideoName = videoName;
            this.VideoLink = videoLink;
            this.VideoNumber = videoNumber;
            this.IndexNumber = indexNumber;
        }

        public static bool waitIntervalToggle { get; set; } = false;
        public static int waitInterval => waitIntervalToggle ? 10 : 30;
        public string VideoName { get; set; }
        public string VideoLink { get; set; }
        public int VideoNumber { get; set; }
        public int IndexNumber { get; set; }
        public QMSingleButton VideoButton { get; set; }

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
            var videoPlayerActive = VideoPlayerCheck();
            var isMaster = MasterCheck(APIUser.CurrentUser.id);
            var friendsWithMaster = FriendsWithMaster();
            var friendsWithCreator = IsFriendsWith(InstanceCreatorId);

            if (videoPlayerActive)
            {
                if (isMaster || friendsWithCreator || friendsWithMaster || APIUser.CurrentUser.id == InstanceCreatorId)
                {
                    if (!onCooldown)
                    {
                        var videoPlayer = GameObject.FindObjectOfType<VRC_SyncVideoPlayer>();
                        var syncVideoPlayer = GameObject.FindObjectOfType<SyncVideoPlayer>();
                        var udonPlayer = GameObject.FindObjectOfType<BaseVRCVideoPlayer>();

                        VideoPlayerType playerType = VideoPlayerType.None;

                        if (videoPlayer != null) playerType = VideoPlayerType.ClassicPlayer;
                        else if (udonPlayer != null) playerType = VideoPlayerType.UdonPlayer;
                        else if (syncVideoPlayer != null) playerType = VideoPlayerType.SyncPlayer;


                        if (playerType == VideoPlayerType.ClassicPlayer)
                        {
                            videoPlayer.Clear();
                            videoPlayer.AddURL(VideoLink);

                            VRCUiManager.prop_VRCUiManager_0.field_Private_List_1_String_0.Add($"Wait {waitInterval} seconds\nfor video to play");

                            yield return new WaitForSeconds(waitInterval);

                            videoPlayer.Next();
                        }

                        else if (playerType == VideoPlayerType.UdonPlayer)
                        {
                            VRC.SDKBase.VRCUrl vrcUrl = new VRC.SDKBase.VRCUrl(VideoLink);
                            udonPlayer.LoadURL(vrcUrl);

                            VRCUiManager.prop_VRCUiManager_0.field_Private_List_1_String_0.Add($"Wait {waitInterval} seconds\nfor video to play");

                            yield return new WaitForSeconds(waitInterval);

                            udonPlayer.Play();
                        }

                        else if (playerType == VideoPlayerType.SyncPlayer)
                        {
                            syncVideoPlayer.field_Private_VRC_SyncVideoPlayer_0.Clear();
                            syncVideoPlayer.field_Private_VRC_SyncVideoPlayer_0.AddURL(VideoLink);

                            VRCUiManager.prop_VRCUiManager_0.field_Private_List_1_String_0.Add($"Wait {waitInterval} seconds\nfor video to play");
                            yield return new WaitForSeconds(waitInterval);

                            syncVideoPlayer.field_Private_VRC_SyncVideoPlayer_0.Next();
                        }
                    }

                    else
                    {
                        VRCUiManager.prop_VRCUiManager_0.field_Private_List_1_String_0.Add($"Video Library is on {waitInterval} second cooldown");
                    }
                }

                else
                {
                    VRCUiManager.prop_VRCUiManager_0.field_Private_List_1_String_0.Add("Only the master and their friends can set videos...");
                }
            }
        }

        public void GetLink()
        {
            System.Windows.Forms.Clipboard.SetText(VideoLink);
            VRCUiManager.prop_VRCUiManager_0.field_Private_List_1_String_0.Add("Video link copied to system clipboard");
        }

        private static bool MasterCheck(string UserID)
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

        private static bool FriendsWithMaster()
        {
            var playerManager = PlayerManager.field_Private_Static_PlayerManager_0.prop_ArrayOf_Player_0;

            for (int i = 0; i < playerManager.Length; i++)
            {
                var player = playerManager[i];
                var apiUser = player.field_Private_APIUser_0;
                var isFriends = IsFriendsWith(apiUser.id);

                if (!player.field_Private_VRCPlayerApi_0.isMaster) continue;
                if (isFriends) return true;
            }

            return false;
        }

        private static string InstanceCreatorId
        {
            get
            {
                return RoomManager.field_Internal_Static_ApiWorldInstance_0.GetInstanceCreator();
            }
        }

        public static IEnumerator VideoFromClipboard(bool onCooldown)
        {
            var videoPlayerActive = VideoPlayerCheck();
            var isMaster = MasterCheck(APIUser.CurrentUser.id);

            if (videoPlayerActive)
            {
                if (isMaster)
                {
                    if (!onCooldown)
                    {
                        var videoPlayer = GameObject.FindObjectOfType<VRC_SyncVideoPlayer>();
                        var udonPlayer = GameObject.FindObjectOfType<BaseVRCVideoPlayer>();

                        VideoPlayerType playerType = VideoPlayerType.None;

                        if (videoPlayer != null) playerType = VideoPlayerType.ClassicPlayer;
                        else if (udonPlayer != null) playerType = VideoPlayerType.UdonPlayer;

                        if (playerType == VideoPlayerType.ClassicPlayer)
                        {
                            videoPlayer.Clear();
                            videoPlayer.AddURL(System.Windows.Forms.Clipboard.GetText());

                            VRCUiManager.prop_VRCUiManager_0.field_Private_List_1_String_0.Add($"Wait {waitInterval} seconds\nfor video to play");

                            yield return new WaitForSeconds(waitInterval);

                            videoPlayer.Next();
                        }

                        else if (playerType == VideoPlayerType.UdonPlayer)
                        {
                            VRC.SDKBase.VRCUrl vrcUrl = new VRC.SDKBase.VRCUrl(System.Windows.Forms.Clipboard.GetText());
                            udonPlayer.LoadURL(vrcUrl);

                            VRCUiManager.prop_VRCUiManager_0.field_Private_List_1_String_0.Add($"Wait {waitInterval} seconds\nfor video to play");

                            yield return new WaitForSeconds(waitInterval);

                            udonPlayer.Play(); // Check back
                        }
                    }

                    else
                    {
                        VRCUiManager.prop_VRCUiManager_0.field_Private_List_1_String_0.Add($"Video Library is on {waitInterval} second cooldown");
                    }
                }

                else
                {
                    VRCUiManager.prop_VRCUiManager_0.field_Private_List_1_String_0.Add("Only the master can set videos...");
                }
            }
        }

        private static bool VideoPlayerCheck()
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

        public enum VideoPlayerType
        {
            UdonPlayer,
            ClassicPlayer,
            None,
            SyncPlayer
        }
    }
}
