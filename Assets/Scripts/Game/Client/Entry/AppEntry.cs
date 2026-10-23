//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: rongaihua
// Date: 2026-10-22
// Desc: 入口场景。创建应用、绑定 UI 相机、播放音乐并打开玩法界面
//---------------------------------------------------------------------------------------

using UnityEngine;

namespace LargeScaleBattle
{

    /// <summary>
    /// 入口。应用在这里创建，在本对象销毁时关掉。
    /// </summary>
    public sealed class AppEntry : MonoBehaviour
    {

        /// <summary>
        /// 玩法界面预制，放在 Libra 的 Res/ui 下。
        /// </summary>
        public const string AreaBattlePrefab = MyGame.ResMgr.PackageRoot + "Res/ui/AreaBattle.prefab";

        /// <summary>
        /// 背景音乐目录。开场从 music_01 到 music_14 里随机一首。
        /// </summary>
        private const string MusicDirectory = "Assets/Res/audio/music/";

        /// <summary>
        /// 背景音乐编号下限，含。
        /// </summary>
        private const int MusicMin = 1;

        /// <summary>
        /// 背景音乐编号上限，含。
        /// </summary>
        private const int MusicMax = 14;

        /// <summary>
        /// 当前入口。
        /// </summary>
        public static AppEntry Current { get; private set; }

        /// <summary>
        /// UI 相机。
        /// </summary>
        public Camera UICamera;

        /// <summary>
        /// UI 根节点。
        /// </summary>
        public RectTransform UIRoot;

        /// <summary>
        /// 入口界面画布。玩法界面不挂在这里。
        /// </summary>
        public Canvas EntryUICanvas;

        /// <summary>
        /// 本局应用。
        /// </summary>
        public MyGame.App App { get; private set; }

        /// <summary>
        /// 还没交给逻辑层的毫秒余数。
        /// </summary>
        private float mTimeMs;

        /// <summary>
        /// 创建应用，绑定 UI 相机，播放开场音乐，打开玩法界面。
        /// </summary>
        private void Awake()
        {
            Current = this;
            DontDestroyOnLoad(gameObject);
            if (UICamera != null)
            {
                DontDestroyOnLoad(UICamera.gameObject);
            }

            App = new MyGame.App();
            App.Add<MyGame.ResMgr>();
            var ui = App.Add<MyGame.UIMgr>();
            App.Add<AreaMatch>();
            var music = App.Add<MyGame.MusicMgr>();
            App.Add<MyGame.SoundMgr>();
            if (!App.Init() || !ui.Bind(UICamera, UIRoot))
            {
                return;
            }

            music.Play(PickMusic(null));
            ui.Open<AreaBattleView>(AreaBattlePrefab);
        }

        /// <summary>
        /// 换一首背景音乐，不重复当前这首。
        /// </summary>
        public void SwitchMusic()
        {
            if (App == null)
            {
                return;
            }

            var music = App.Find<MyGame.MusicMgr>();
            if (music == null)
            {
                return;
            }

            music.Play(PickMusic(music.PlayingPath));
        }

        /// <summary>
        /// 在 music_01 到 music_14 里随机一首。
        /// </summary>
        /// <param name="avoid">要避开的曲目路径。为空时不避开。</param>
        /// <returns>曲目路径。</returns>
        private static string PickMusic(string avoid)
        {
            var index = Random.Range(MusicMin, MusicMax + 1);
            var path = MusicPath(index);
            if (path != avoid)
            {
                return path;
            }

            index = index >= MusicMax ? MusicMin : index + 1;
            return MusicPath(index);
        }

        /// <summary>
        /// 拼出一首背景音乐的路径。
        /// </summary>
        /// <param name="index">曲目编号，从 1 起。</param>
        /// <returns>曲目路径。</returns>
        private static string MusicPath(int index)
        {
            return MusicDirectory + "music_" + index.ToString("00") + ".mp3";
        }

        /// <summary>
        /// 把帧间隔换成毫秒交给应用。
        /// </summary>
        private void Update()
        {
            if (App == null)
            {
                return;
            }

            mTimeMs += Time.deltaTime * 1000f;
            var delta = (int)mTimeMs;
            if (delta <= 0)
            {
                return;
            }

            mTimeMs -= delta;
            App.Update(delta);
        }

        /// <summary>
        /// 关掉应用。界面在界面管理器销毁时关掉。
        /// </summary>
        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }

            if (App != null)
            {
                App.Shutdown();
                App = null;
            }

            if (UICamera != null)
            {
                Destroy(UICamera.gameObject);
                UICamera = null;
            }
        }
    }
}
