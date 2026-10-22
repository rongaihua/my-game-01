//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: rongaihua
// Date: 2026-10-22
// Desc: 选面积大作战的界面
//---------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LargeScaleBattle
{

    /// <summary>
    /// 选面积大作战的界面。棋盘和联网在 <see cref="AreaMatch"/>，这里只负责显示和点击。
    /// </summary>
    public sealed class AreaBattleDriver : MonoBehaviour
    {

        /// <summary>
        /// 红、蓝、黄、绿的底色。
        /// </summary>
        private static readonly Color[] RegionColors =
        {
            new Color(0.90f, 0.29f, 0.32f),
            new Color(0.29f, 0.56f, 0.90f),
            new Color(0.95f, 0.76f, 0.22f),
            new Color(0.30f, 0.75f, 0.42f)
        };

        /// <summary>
        /// 四种颜色的名字。
        /// </summary>
        private static readonly string[] RegionNames = { "红", "蓝", "黄", "绿" };

        /// <summary>
        /// 拼名单和结果文案。
        /// </summary>
        private readonly StringBuilder mBuilder = new StringBuilder(256);

        /// <summary>
        /// 抄房间名单用的 id 缓冲。
        /// </summary>
        private readonly List<int> mIds = new List<int>(4);

        /// <summary>
        /// 本局用的管理器容器。
        /// </summary>
        private MyGame.App mApp;

        /// <summary>
        /// 对局。
        /// </summary>
        private AreaMatch mMatch;

        /// <summary>
        /// 预制上的节点。
        /// </summary>
        private AreaBattleView mView;

        /// <summary>
        /// 监听事件并接上预制里的按钮。
        /// </summary>
        private void Awake()
        {
            mView = GetComponent<AreaBattleView>();
            mApp = AppEntry.Current.App;
            mMatch = mApp.Find<AreaMatch>();
            mApp.Events.On<AreaPhaseChanged>(OnPhase);
            mApp.Events.On<AreaBoardReady>(OnBoard);
            mApp.Events.On<AreaPickLocked>(OnPick);
            mApp.Events.On<AreaCountdown>(OnCountdown);
            mApp.Events.On<AreaRosterChanged>(OnRoster);
            mApp.Events.On<AreaResultReady>(OnResult);
            mView.HostButton.onClick.AddListener(OnHost);
            mView.JoinButton.onClick.AddListener(OnJoin);
            mView.Start.onClick.AddListener(OnStart);
            mView.LeaveRoom.onClick.AddListener(OnLeave);
            mView.Again.onClick.AddListener(OnStart);
            mView.LeavePlay.onClick.AddListener(OnLeave);
            for (var i = 0; i < mView.Cells.Length; i++)
            {
                var captured = i;
                mView.Cells[i].GetComponent<Button>().onClick.AddListener(() => mMatch.PickCell(captured));
            }

            ApplyPhase();
        }

        /// <summary>
        /// 取消监听。应用由入口关掉。
        /// </summary>
        private void OnDestroy()
        {
            if (mApp == null || mApp.Events == null)
            {
                return;
            }

            mApp.Events.Off<AreaPhaseChanged>(OnPhase);
            mApp.Events.Off<AreaBoardReady>(OnBoard);
            mApp.Events.Off<AreaPickLocked>(OnPick);
            mApp.Events.Off<AreaCountdown>(OnCountdown);
            mApp.Events.Off<AreaRosterChanged>(OnRoster);
            mApp.Events.Off<AreaResultReady>(OnResult);
            mApp = null;
            mMatch = null;
            mView = null;
        }

        /// <summary>
        /// 用输入的名字创建房间。
        /// </summary>
        private void OnHost()
        {
            mMatch.Host(mView.NameInput.text);
        }

        /// <summary>
        /// 用输入的名字和地址加入房间。
        /// </summary>
        private void OnJoin()
        {
            mMatch.Join(mView.NameInput.text, mView.HostInput.text);
        }

        /// <summary>
        /// 开始一局。
        /// </summary>
        private void OnStart()
        {
            mMatch.StartRound();
        }

        /// <summary>
        /// 离开房间。
        /// </summary>
        private void OnLeave()
        {
            mMatch.Leave();
        }

        /// <summary>
        /// 阶段变化时切换页面。
        /// </summary>
        /// <param name="message">阶段。</param>
        private void OnPhase(AreaPhaseChanged message)
        {
            ApplyPhase();
        }

        /// <summary>
        /// 棋盘生成后重画格子并清空结果。
        /// </summary>
        /// <param name="message">棋盘就绪。</param>
        private void OnBoard(AreaBoardReady message)
        {
            PaintBoard();
            mView.Result.text = string.Empty;
        }

        /// <summary>
        /// 本机选定后重画并刷新倒计时。
        /// </summary>
        /// <param name="message">已选定。</param>
        private void OnPick(AreaPickLocked message)
        {
            PaintBoard();
            ShowClock(mMatch.SecondsLeft);
        }

        /// <summary>
        /// 刷新倒计时文字。
        /// </summary>
        /// <param name="message">剩余秒。</param>
        private void OnCountdown(AreaCountdown message)
        {
            ShowClock(message.Seconds);
        }

        /// <summary>
        /// 重画房间名单。
        /// </summary>
        /// <param name="message">名单变化。</param>
        private void OnRoster(AreaRosterChanged message)
        {
            mApp.Room.CopyPlayerIds(mIds);
            mBuilder.Clear();
            for (var i = 0; i < mIds.Count; i++)
            {
                string name;
                if (!mApp.Room.TryGetName(mIds[i], out name))
                {
                    continue;
                }

                if (mBuilder.Length > 0)
                {
                    mBuilder.Append('\n');
                }

                mBuilder.Append(name);
                if (mIds[i] == mApp.Room.LocalPlayerId)
                {
                    mBuilder.Append("（我）");
                }
            }

            mView.Players.text = mBuilder.ToString();
        }

        /// <summary>
        /// 揭晓各色格数和获胜者。
        /// </summary>
        /// <param name="message">揭晓。</param>
        private void OnResult(AreaResultReady message)
        {
            PaintBoard();
            var best = mMatch.BestQuota();
            mBuilder.Clear();
            for (var region = 0; region < MyGame.AreaGrid.RegionCount; region++)
            {
                if (region > 0)
                {
                    mBuilder.Append("    ");
                }

                mBuilder.Append(RegionNames[region]);
                mBuilder.Append(' ');
                mBuilder.Append(mMatch.Grid.Quota(region));
                mBuilder.Append(" 格");
            }

            mBuilder.Append('\n');
            if (mMatch.LocalPick < 0)
            {
                mBuilder.Append("你没有选择");
            }
            else
            {
                mBuilder.Append("你选了");
                mBuilder.Append(RegionNames[mMatch.LocalPick]);
            }

            mBuilder.Append("\n获胜：");
            mApp.Room.CopyPlayerIds(mIds);
            var winners = 0;
            for (var i = 0; i < mIds.Count; i++)
            {
                int region;
                if (!mMatch.TryPick(mIds[i], out region) || mMatch.Grid.Quota(region) != best)
                {
                    continue;
                }

                string name;
                if (!mApp.Room.TryGetName(mIds[i], out name))
                {
                    continue;
                }

                if (winners > 0)
                {
                    mBuilder.Append('、');
                }

                mBuilder.Append(name);
                winners++;
            }

            if (winners == 0)
            {
                mBuilder.Append("无人");
            }

            mView.Result.text = mBuilder.ToString();
            mView.Countdown.text = "揭晓";
        }

        /// <summary>
        /// 显示剩余秒。已选时带上颜色名。
        /// </summary>
        /// <param name="seconds">剩余秒。</param>
        private void ShowClock(int seconds)
        {
            if (mMatch.LocalPick >= 0)
            {
                mView.Countdown.text = "已选" + RegionNames[mMatch.LocalPick] + "  " + seconds + " 秒";
                return;
            }

            mView.Countdown.text = seconds + " 秒";
        }

        /// <summary>
        /// 按阶段显示菜单、房间或棋盘。
        /// </summary>
        private void ApplyPhase()
        {
            var phase = mMatch.Phase;
            mView.Status.text = mMatch.Status;
            mView.Menu.SetActive(phase == AreaPhase.Menu || phase == AreaPhase.Connecting);
            mView.Room.SetActive(phase == AreaPhase.Room);
            mView.Play.SetActive(phase == AreaPhase.Picking || phase == AreaPhase.Result);
            mView.StartButton.SetActive(mMatch.IsHost);
            mView.AgainButton.SetActive(mMatch.IsHost && phase == AreaPhase.Result);
            if (phase == AreaPhase.Picking)
            {
                mView.Result.text = string.Empty;
            }
        }

        /// <summary>
        /// 按颜色涂格子。揭晓时把最大块和自己选的块提亮。
        /// </summary>
        private void PaintBoard()
        {
            var reveal = mMatch.Phase == AreaPhase.Result;
            var best = reveal ? mMatch.BestQuota() : -1;
            var cells = mView.Cells;
            for (var i = 0; i < cells.Length; i++)
            {
                var region = mMatch.Grid.Get(i);
                var color = RegionColors[region];
                var mine = mMatch.LocalPick == region;
                var winning = reveal && mMatch.Grid.Quota(region) == best;
                if (mine || winning)
                {
                    color = Color.Lerp(color, Color.white, 0.42f);
                }

                cells[i].color = color;
            }
        }
    }
}
