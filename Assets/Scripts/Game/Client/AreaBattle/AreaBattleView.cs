//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: rongaihua
// Date: 2026-10-22
// Desc: 选面积界面预制上的引用
//---------------------------------------------------------------------------------------

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LargeScaleBattle
{

    /// <summary>
    /// 选面积界面预制上的节点。显示逻辑在 <see cref="AreaBattleDriver"/>。
    /// </summary>
    public sealed class AreaBattleView : MonoBehaviour
    {

        /// <summary>
        /// 状态文案。
        /// </summary>
        public TextMeshProUGUI Status;

        /// <summary>
        /// 倒计时。
        /// </summary>
        public TextMeshProUGUI Countdown;

        /// <summary>
        /// 房间名单。
        /// </summary>
        public TextMeshProUGUI Players;

        /// <summary>
        /// 揭晓文案。
        /// </summary>
        public TextMeshProUGUI Result;

        /// <summary>
        /// 本机名字输入。
        /// </summary>
        public TMP_InputField NameInput;

        /// <summary>
        /// 房主地址输入。
        /// </summary>
        public TMP_InputField HostInput;

        /// <summary>
        /// 菜单页。
        /// </summary>
        public GameObject Menu;

        /// <summary>
        /// 房间页。
        /// </summary>
        public GameObject Room;

        /// <summary>
        /// 选择和揭晓页。
        /// </summary>
        public GameObject Play;

        /// <summary>
        /// 开始按钮，只给房主显示。
        /// </summary>
        public GameObject StartButton;

        /// <summary>
        /// 再来一局按钮，只给房主在揭晓后显示。
        /// </summary>
        public GameObject AgainButton;

        /// <summary>
        /// 创建房间。
        /// </summary>
        public Button HostButton;

        /// <summary>
        /// 加入房间。
        /// </summary>
        public Button JoinButton;

        /// <summary>
        /// 开始。
        /// </summary>
        public Button Start;

        /// <summary>
        /// 房间里的离开。
        /// </summary>
        public Button LeaveRoom;

        /// <summary>
        /// 再来一局。
        /// </summary>
        public Button Again;

        /// <summary>
        /// 对局里的离开。
        /// </summary>
        public Button LeavePlay;

        /// <summary>
        /// 棋盘格子，下标与地盘一致。
        /// </summary>
        public Image[] Cells;
    }
}
