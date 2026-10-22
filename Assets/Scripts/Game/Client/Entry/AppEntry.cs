//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: rongaihua
// Date: 2026-10-22
// Desc: 入口场景。创建应用、绑定 UI 相机并打开玩法界面
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
        /// 创建应用，绑定 UI 相机，打开玩法界面。
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
            if (!App.Init() || !ui.Bind(UICamera, UIRoot))
            {
                return;
            }

            ui.Open<AreaBattleView>(AreaBattlePrefab);
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
