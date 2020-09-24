using MelonLoader;
using RubyButtonAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIExpansionKit.API;
using UnityEngine;
using UnityEngine.UI;
using VRC;
using VRC.Core;
using VRC.SDKBase;
using VRCSDK2;

namespace VideoLibrary
{
    public class VideoLib : MelonMod
    {
        protected List<ModVideo> videoList;
 
        private int indexNumber = 0;
        private int currentMenuIndex;

        private QMNestedButton videoLibrary;

        public override void OnApplicationStart()
        {
            videoList = new List<ModVideo>();
            InitializeLibrary();
        }

        public override void VRChat_OnUiManagerInit()
        {
            ExpansionKitApi.RegisterSimpleMenuButton(ExpandedMenu.QuickMenu, "Video\nLibrary", delegate
            {
                videoLibrary.getMainButton().getGameObject().GetComponent<Button>().Press();
            });

            videoLibrary = new QMNestedButton("ShortcutMenu", -10, 0, "", "", null, null, null, null);
            videoLibrary.getMainButton().getGameObject().GetComponentInChildren<Image>().enabled = false;
            videoLibrary.getMainButton().getGameObject().GetComponentInChildren<Text>().enabled = false;

            var indexButton = new QMSingleButton(videoLibrary, 4, 1, "Page:\n" + (currentMenuIndex + 1).ToString() + " of " + (indexNumber + 1).ToString(), delegate { }, "", Color.clear, Color.yellow);
            indexButton.getGameObject().GetComponentInChildren<Button>().enabled = false;
            indexButton.getGameObject().GetComponentInChildren<Image>().enabled = false;

            var previousButton = new QMSingleButton(videoLibrary, 4, 0, "Previous\nPage", delegate
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

            var nextButton = new QMSingleButton(videoLibrary, 4, 2, "Next\nPage", delegate
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

            if (videoList.Count <= 9)
            {
                previousButton.setIntractable(false);
                nextButton.setIntractable(false);
            }

            foreach (ModVideo video in videoList)
            {
                if (video.VideoNumber == 0)
                {
                    var vidButton = new QMSingleButton(videoLibrary, 1, 0, video.VideoName, delegate
                    {
                        MelonCoroutines.Start(video.AddVideo());
                    }, "Puts " + video.VideoName + " on the video player", null, null);

                    video.VideoButton = vidButton;
                }

                else if (video.VideoNumber == 1)
                {
                    var vidButton = new QMSingleButton(videoLibrary, 2, 0, video.VideoName, delegate
                    {
                        MelonCoroutines.Start(video.AddVideo());
                    }, "Puts " + video.VideoName + " on the video player", null, null);

                    video.VideoButton = vidButton;
                }

                else if (video.VideoNumber == 2)
                {
                    var vidButton = new QMSingleButton(videoLibrary, 3, 0, video.VideoName, delegate
                    {
                        MelonCoroutines.Start(video.AddVideo());
                    }, "Puts " + video.VideoName + " on the video player", null, null);

                    video.VideoButton = vidButton;
                }

                else if (video.VideoNumber == 3)
                {
                    var vidButton = new QMSingleButton(videoLibrary, 1, 1, video.VideoName, delegate
                    {
                        MelonCoroutines.Start(video.AddVideo());
                    }, "Puts " + video.VideoName + " on the video player", null, null);

                    video.VideoButton = vidButton;
                }

                else if (video.VideoNumber == 4)
                {
                    var vidButton = new QMSingleButton(videoLibrary, 2, 1, video.VideoName, delegate
                    {
                        MelonCoroutines.Start(video.AddVideo());
                    }, "Puts " + video.VideoName + " on the video player", null, null);

                    video.VideoButton = vidButton;
                }

                else if (video.VideoNumber == 5)
                {
                    var vidButton = new QMSingleButton(videoLibrary, 3, 1, video.VideoName, delegate
                    {
                        MelonCoroutines.Start(video.AddVideo());
                    }, "Puts " + video.VideoName + " on the video player", null, null);

                    video.VideoButton = vidButton;
                }

                else if (video.VideoNumber == 6)
                {
                    var vidButton = new QMSingleButton(videoLibrary, 1, 2, video.VideoName, delegate
                    {
                        MelonCoroutines.Start(video.AddVideo());
                    }, "Puts " + video.VideoName + " on the video player", null, null);

                    video.VideoButton = vidButton;
                }

                else if (video.VideoNumber == 7)
                {
                    var vidButton = new QMSingleButton(videoLibrary, 2, 2, video.VideoName, delegate
                    {
                        MelonCoroutines.Start(video.AddVideo());
                    }, "Puts " + video.VideoName + " on the video player", null, null);

                    video.VideoButton = vidButton;
                }

                else if (video.VideoNumber == 8)
                {
                    var vidButton = new QMSingleButton(videoLibrary, 3, 2, video.VideoName, delegate
                    {
                        MelonCoroutines.Start(video.AddVideo());
                    }, "Puts " + video.VideoName + " on the video player", null, null);

                    video.VideoButton = vidButton;
                }

                if (video.IndexNumber != currentMenuIndex)
                {
                    video.VideoButton.setActive(false);
                }
            }

        }

        public void InitializeLibrary()
        {
            string exampleVideo = "Example Name|https://youtu.be/pKO9UjSeLew";

            var rootDirectory = Application.dataPath;
            rootDirectory += @"\..\";

            var subDirectory = rootDirectory + @"\UHModz\";

            var videoDirectory = subDirectory + "Videos.txt";

            if (!Directory.Exists(subDirectory))
            {
                Directory.CreateDirectory(subDirectory);
                MelonLogger.Log("Created UHModz Directory!");
            }

            if (!File.Exists(videoDirectory))
            {
                using (StreamWriter sw = File.CreateText(videoDirectory))
                {
                    sw.WriteLine(exampleVideo);
                    sw.Close();
                }
            }

            string line;
            StreamReader file = new StreamReader(videoDirectory);

            while ((line = file.ReadLine()) != null)
            {
                var lineArray = line.Split('|');
                videoList.Add(new ModVideo { VideoName = lineArray[0], VideoLink = lineArray[1] });
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

            //foreach (ModVideo video in videoList)
            //{
            //    video.VideoNumber = videoNumber;
            //    video.IndexNumber = indexNumber;

            //    videoNumber++;

            //    if (videoNumber == 9)
            //    {
            //        videoNumber = 0;
            //        indexNumber++;
            //    }
            //}
        }
    }
}
