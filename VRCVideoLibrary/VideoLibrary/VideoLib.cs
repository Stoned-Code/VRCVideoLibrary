/*
 * 
 */
using MelonLoader;
using Mono.Cecil;
using RubyButtonAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UIExpansionKit.API;
using UnityEngine;
using UnityEngine.UI;
using VRC;
using VRC.Core;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Video.Components.Base;
using VRCSDK2;

namespace VideoLibrary
{
    internal static class LibraryBuildInfo
    {
        public const string modName = "VRCVideoLibrary";
        public const string modVersion = "1.2.0";
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

        private QMSingleButton indexButton;
        private HarmonyLib.Harmony hInstance;

        private static string modPath = Path.Combine(Environment.CurrentDirectory, "Mods", "MoonriseV2.dll");


        //////////////////////
        //  VRChat Methods  //
        //////////////////////

        public override void OnApplicationStart()
        {
            ExpansionKitApi.OnUiManagerInit += OnUiManagerInit;
            videoList = new List<ModVideo>();
            MelonCoroutines.Start(InitializeLibrary());
            MelonCoroutines.Start(LoadMenu());

            hInstance = new HarmonyLib.Harmony("com.StonedCode.VRCVideoLibrary");
            hInstance.PatchAll();
        }

        private void OnUiManagerInit()
        {
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.QuickMenu).AddSimpleButton("Video\nLibrary", () =>
            {
                videoLibrary.getMainButton().getGameObject().GetComponent<Button>().onClick?.Invoke();
            });
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
            var subDirectory = Path.Combine(rootDirectory, "UserData", "UHModz");

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
            while (APIUser.CurrentUser == null) yield return null;

            videoLibrary = new QMNestedButton("ShortcutMenu", 5, -1, "", "", null, null, null, null);
            videoLibrary.getMainButton().getGameObject().GetComponentInChildren<Image>().enabled = false;
            videoLibrary.getMainButton().getGameObject().GetComponentInChildren<Text>().enabled = false;

            indexButton = new QMSingleButton(videoLibrary, 4, 1, "Page:\n" + (currentMenuIndex + 1).ToString() + " of " + (indexNumber + 1).ToString(), delegate { }, "", Color.clear, Color.yellow);
            indexButton.getGameObject().GetComponentInChildren<Button>().enabled = false;
            indexButton.getGameObject().GetComponentInChildren<Image>().enabled = false;

            previousButton = new QMSingleButton(videoLibrary, 4, 0, "", () =>
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
            MakeArrowButton(previousButton, ArrowDirection.Up);

            nextButton = new QMSingleButton(videoLibrary, 4, 2, "", () =>
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
            MakeArrowButton(nextButton, ArrowDirection.Down);
            var videoFromClipboard = new QMSingleButton(videoLibrary, 1, -2, "Video From\nClipboard", () =>
            {
                MelonCoroutines.Start(ModVideo.VideoFromClipboard(onCooldown));
            }, "Puts the link in your system clipboard into the world's video player");

            var openListButton = new QMSingleButton(videoLibrary, 2, -2, "Open\nLibrary\nDocument", () =>
            {
                OpenVideoLibrary();
            }, "Opens the Video Library text document\nLibrary Format: \"Button Name|Video Url\"", null, null);

            var openReadMe = new QMSingleButton(videoLibrary, 3, -2, "Read\nMe", () =>
            {
                Process.Start("https://github.com/UshioHiko/VRCVideoLibrary/blob/master/README.md");
            }, "Opens a link to the mod's \"Read Me\"");

            var getLinkToggle = new QMToggleButton(videoLibrary, 4, -2, "Buttons Copy\nVideo Link", () =>
            {
                getLink = true;
            }, "Disabled", () =>
            {
                getLink = false;
            }, "Makes video library buttons copy video url to your system clipboard", null, null, false, false);

            var refreshList = new QMSingleButton(videoLibrary, 5, -2, "Refresh\nList", () =>
            {
                DeleteButtons();
                ClearButtons();
                GetVideoLibrary();
                BuildList();
            }, "Refreshes the list");

            if (videoList.Count <= 9)
            {
                previousButton.setIntractable(false);
                nextButton.setIntractable(false);
            }

            BuildList();
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

        public void DeleteButtons()
        {
            for (int i = 0; i < videoList.Count; i++)
            {
                var vid = videoList[i];

                vid.DestroyButton();
            }
        }

        public void ClearButtons()
        {
            videoList.Clear();
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

        public void BuildList()
        {
            for (int i = 0; i < videoList.Count; i++)
            {
                ModVideo video = videoList[i];

                switch (video.VideoNumber)
                {
                    case 0:
                        {
                            var vidButton = new QMSingleButton(videoLibrary, 1, 0, video.VideoName, () =>
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
                            var vidButton = new QMSingleButton(videoLibrary, 2, 0, video.VideoName, () =>
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
                            var vidButton = new QMSingleButton(videoLibrary, 3, 0, video.VideoName, () =>
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
                            var vidButton = new QMSingleButton(videoLibrary, 1, 1, video.VideoName, () =>
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
                            var vidButton = new QMSingleButton(videoLibrary, 2, 1, video.VideoName, () =>
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
                            var vidButton = new QMSingleButton(videoLibrary, 3, 1, video.VideoName, () =>
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
                            var vidButton = new QMSingleButton(videoLibrary, 1, 2, video.VideoName, () =>
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
                            var vidButton = new QMSingleButton(videoLibrary, 2, 2, video.VideoName, () =>
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
                            var vidButton = new QMSingleButton(videoLibrary, 3, 2, video.VideoName, () =>
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

            indexButton.setButtonText("Page:\n" + (currentMenuIndex + 1).ToString() + " of " + (indexNumber + 1).ToString());
        }

        /// <summary>
        /// Turns a QMSingleButton into an arrow button.
        /// </summary>
        /// <param name="qmSingleButton">QMSingleButton you want to turn into an arrow.</param>
        /// <param name="arrowDirection">Direction you want the arrow to point.</param>
        public static void MakeArrowButton(QMSingleButton qmSingleButton, ArrowDirection arrowDirection)
        {
            var arrowSprite = QuickMenu.prop_QuickMenu_0.transform.Find("QuickMenu_NewElements/_CONTEXT/QM_Context_User_Selected/NextArrow_Button").GetComponentInChildren<Image>().sprite;
            if (arrowDirection == ArrowDirection.Up)
            {
                qmSingleButton.getGameObject().GetComponentInChildren<Image>().sprite = arrowSprite;
                qmSingleButton.getGameObject().GetComponent<RectTransform>().localRotation = Quaternion.Euler(new Vector3(0, 0, 90));
                qmSingleButton.getGameObject().GetComponentInChildren<Text>().GetComponent<RectTransform>().localRotation = Quaternion.Euler(new Vector3(0, 0, -90));
            }

            else if (arrowDirection == ArrowDirection.Down)
            {
                qmSingleButton.getGameObject().GetComponentInChildren<Image>().sprite = arrowSprite;
                qmSingleButton.getGameObject().GetComponent<RectTransform>().localRotation = Quaternion.Euler(new Vector3(0, 0, -90));
                qmSingleButton.getGameObject().GetComponentInChildren<Text>().GetComponent<RectTransform>().localRotation = Quaternion.Euler(new Vector3(0, 0, 90));
            }

            else if (arrowDirection == ArrowDirection.Left)
            {
                qmSingleButton.getGameObject().GetComponentInChildren<Image>().sprite = arrowSprite;
                qmSingleButton.getGameObject().GetComponent<RectTransform>().localRotation = Quaternion.Euler(new Vector3(0, 0, 180));
                qmSingleButton.getGameObject().GetComponentInChildren<Text>().GetComponent<RectTransform>().localRotation = Quaternion.Euler(new Vector3(0, 0, -180));
            }

            else if (arrowDirection == ArrowDirection.Right)
            {
                qmSingleButton.getGameObject().GetComponentInChildren<Image>().sprite = arrowSprite;
            }
        }

        public enum ArrowDirection
        {
            Up,
            Down,
            Left,
            Right
        }
    }
}
