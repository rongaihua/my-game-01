//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: rongaihua
// Date: 2026-10-22
// Desc: 一局选面积的房主与加入者流程
//---------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LargeScaleBattle
{

    /// <summary>
    /// 一局选面积所处的阶段。
    /// </summary>
    public enum AreaPhase
    {

        /// <summary>
        /// 还没进房间。
        /// </summary>
        Menu,

        /// <summary>
        /// 正在连接房主。
        /// </summary>
        Connecting,

        /// <summary>
        /// 在房间里等开始。
        /// </summary>
        Room,

        /// <summary>
        /// 倒计时内选一块。
        /// </summary>
        Picking,

        /// <summary>
        /// 已揭晓。
        /// </summary>
        Result
    }

    /// <summary>
    /// 阶段或状态文案变了。
    /// </summary>
    public struct AreaPhaseChanged
    {

        /// <summary>
        /// 当前阶段。
        /// </summary>
        public AreaPhase Phase;

        /// <summary>
        /// 给界面看的状态文案。
        /// </summary>
        public string Status;
    }

    /// <summary>
    /// 棋盘已经按种子生成。
    /// </summary>
    public struct AreaBoardReady
    {
    }

    /// <summary>
    /// 本机已经选定一块。
    /// </summary>
    public struct AreaPickLocked
    {
    }

    /// <summary>
    /// 倒计时显示的秒数变了。
    /// </summary>
    public struct AreaCountdown
    {

        /// <summary>
        /// 剩余秒数。
        /// </summary>
        public int Seconds;
    }

    /// <summary>
    /// 房间名单变了。
    /// </summary>
    public struct AreaRosterChanged
    {
    }

    /// <summary>
    /// 本局揭晓。
    /// </summary>
    public struct AreaResultReady
    {
    }

    /// <summary>
    /// 一局选面积。房主生成棋盘并在倒计时结束时揭晓，其他人只提交自己的选择。
    /// </summary>
    public sealed class AreaMatch : MyGame.Module
    {

        /// <summary>
        /// 房间端口。
        /// </summary>
        public const int Port = 29000;

        /// <summary>
        /// 一局选择时间，毫秒。
        /// </summary>
        private const int RoundMs = 12000;

        /// <summary>
        /// 名单包。
        /// </summary>
        private const byte Roster = 4;

        /// <summary>
        /// 开局包。
        /// </summary>
        private const byte Round = 5;

        /// <summary>
        /// 选择包。
        /// </summary>
        private const byte Pick = 6;

        /// <summary>
        /// 揭晓包。
        /// </summary>
        private const byte Reveal = 7;

        /// <summary>
        /// 房主确认加入，或告知房间已满。
        /// </summary>
        private const byte Welcome = 8;

        /// <summary>
        /// 本局棋盘。
        /// </summary>
        private readonly MyGame.AreaGrid mGrid = new MyGame.AreaGrid();

        /// <summary>
        /// 玩家 id 到所选颜色。
        /// </summary>
        private readonly Dictionary<int, int> mPicks = new Dictionary<int, int>();

        /// <summary>
        /// 抄名单时用的 id 缓冲。
        /// </summary>
        private readonly List<int> mIds = new List<int>(4);

        /// <summary>
        /// 组包缓冲。
        /// </summary>
        private readonly byte[] mBody = new byte[128];

        /// <summary>
        /// 本机是房主。
        /// </summary>
        private bool mIsHost;

        /// <summary>
        /// 选择阶段剩余毫秒。
        /// </summary>
        private int mLeftMs;

        /// <summary>
        /// 已经派发出去的剩余秒，避免同一秒重复派发。
        /// </summary>
        private int mShownSecond = -1;

        /// <summary>
        /// 本机所选颜色。未选为 -1。
        /// </summary>
        private int mLocalPick = -1;

        /// <summary>
        /// 当前状态文案。
        /// </summary>
        private string mStatus = "创建房间，或输入房主地址加入";

        /// <summary>
        /// 当前阶段。
        /// </summary>
        public AreaPhase Phase { get; private set; } = AreaPhase.Menu;

        /// <summary>
        /// 本机是否房主。
        /// </summary>
        public bool IsHost { get { return mIsHost; } }

        /// <summary>
        /// 本局棋盘。
        /// </summary>
        public MyGame.AreaGrid Grid { get { return mGrid; } }

        /// <summary>
        /// 本机所选颜色。未选为 -1。
        /// </summary>
        public int LocalPick { get { return mLocalPick; } }

        /// <summary>
        /// 状态文案。
        /// </summary>
        public string Status { get { return mStatus; } }

        /// <summary>
        /// 剩余秒数，向上取整。
        /// </summary>
        public int SecondsLeft { get { return mLeftMs <= 0 ? 0 : (mLeftMs + 999) / 1000; } }

        /// <summary>
        /// 读取某名玩家选的颜色。
        /// </summary>
        /// <param name="playerId">玩家 id。</param>
        /// <param name="region">所选颜色。</param>
        /// <returns>该玩家已选择时为 true。</returns>
        public bool TryPick(int playerId, out int region)
        {
            return mPicks.TryGetValue(playerId, out region);
        }

        /// <summary>
        /// 四种颜色里最大的格子数。
        /// </summary>
        /// <returns>最大配额。</returns>
        public int BestQuota()
        {
            var best = 0;
            for (var i = 0; i < MyGame.AreaGrid.RegionCount; i++)
            {
                var quota = mGrid.Quota(i);
                if (quota > best)
                {
                    best = quota;
                }
            }

            return best;
        }

        /// <summary>
        /// 本机开房并成为房主。
        /// </summary>
        /// <param name="name">本机名字。</param>
        public void Host(string name)
        {
            try
            {
                App.Net.StartServer(Port);
            }
            catch (Exception exception)
            {
                SetPhase(AreaPhase.Menu, exception.Message);
                return;
            }

            mIsHost = true;
            mPicks.Clear();
            mLocalPick = -1;
            App.Room.Clear();
            App.Room.SetLocalPlayer(0, ClipName(name));
            SetPhase(AreaPhase.Room, "房间已开。" + LocalAddress() + ":" + Port);
            EmitRoster();
        }

        /// <summary>
        /// 连接房主。
        /// </summary>
        /// <param name="name">本机名字。</param>
        /// <param name="host">房主地址，可带端口。</param>
        public void Join(string name, string host)
        {
            host = ClipHost(host);
            if (host.Length == 0)
            {
                SetPhase(AreaPhase.Menu, "先填写房主地址");
                return;
            }

            mIsHost = false;
            mPendingName = ClipName(name);
            mPicks.Clear();
            mLocalPick = -1;
            App.Room.Clear();
            App.Room.SetLocalPlayerId(-1);
            SetPhase(AreaPhase.Connecting, "正在连接 " + host);
            try
            {
                App.Net.Connect(host, Port);
            }
            catch (Exception exception)
            {
                App.Net.Stop();
                SetPhase(AreaPhase.Menu, exception.Message);
            }
        }

        /// <summary>
        /// 房主开始一局，并把种子和时长发给其他人。
        /// </summary>
        public void StartRound()
        {
            if (!mIsHost || (Phase != AreaPhase.Room && Phase != AreaPhase.Result))
            {
                return;
            }

            BeginRound(Environment.TickCount, RoundMs);
            WriteInt(mBody, 0, mSeed);
            WriteInt(mBody, 4, RoundMs);
            App.Net.Send(Round, mBody, 8);
        }

        /// <summary>
        /// 选择一格所在的颜色。房主直接入账，其他人发给房主。
        /// </summary>
        /// <param name="index">格子下标。</param>
        public void PickCell(int index)
        {
            if (Phase != AreaPhase.Picking || mLeftMs <= 0 || mLocalPick >= 0)
            {
                return;
            }

            if (index < 0 || index >= MyGame.AreaGrid.CellCount)
            {
                return;
            }

            var region = mGrid.Get(index);
            if (mIsHost)
            {
                AcceptPick(App.Room.LocalPlayerId, region);
                return;
            }

            mLocalPick = region;
            mBody[0] = region;
            App.Net.Send(Pick, mBody, 1);
            App.Events.Emit(new AreaPickLocked());
        }

        /// <summary>
        /// 离开房间并回到菜单。
        /// </summary>
        public void Leave()
        {
            if (App.Net.IsServer || Phase == AreaPhase.Connecting || Phase == AreaPhase.Room || Phase == AreaPhase.Picking || Phase == AreaPhase.Result)
            {
                if (!mIsHost && Phase != AreaPhase.Menu && Phase != AreaPhase.Connecting)
                {
                    App.Net.Send(MyGame.NetMgr.Leave, mBody, 0);
                }
            }

            App.Net.Stop();
            ResetRoom(AreaPhase.Menu, "已离开房间");
        }

        /// <summary>
        /// 监听网络事件。
        /// </summary>
        protected override void OnCreate()
        {
            App.Events.On<MyGame.NetPacket>(OnPacket);
            App.Events.On<MyGame.NetConnected>(OnConnected);
            App.Events.On<MyGame.NetFailed>(OnFailed);
            App.Events.On<MyGame.NetPeerClosed>(OnPeerClosed);
        }

        /// <summary>
        /// 取消监听并停掉网络。
        /// </summary>
        protected override void OnDestroy()
        {
            App.Events.Off<MyGame.NetPacket>(OnPacket);
            App.Events.Off<MyGame.NetConnected>(OnConnected);
            App.Events.Off<MyGame.NetFailed>(OnFailed);
            App.Events.Off<MyGame.NetPeerClosed>(OnPeerClosed);
            App.Net.Stop();
        }

        /// <summary>
        /// 选择阶段扣减剩余时间。房主到时揭晓。
        /// </summary>
        /// <param name="deltaMs">距上一帧的毫秒数。</param>
        protected override void OnUpdate(int deltaMs)
        {
            if (Phase != AreaPhase.Picking || mLeftMs <= 0)
            {
                return;
            }

            mLeftMs -= deltaMs;
            if (mLeftMs < 0)
            {
                mLeftMs = 0;
            }

            PublishCountdown();
            if (mLeftMs == 0 && mIsHost)
            {
                FinishRound();
            }
        }

        /// <summary>
        /// 本局棋盘种子。
        /// </summary>
        private int mSeed;

        /// <summary>
        /// 连上之后发给房主的名字。
        /// </summary>
        private string mPendingName = "玩家";

        /// <summary>
        /// 连上房主后送出名字。
        /// </summary>
        /// <param name="message">连接成功。</param>
        private void OnConnected(MyGame.NetConnected message)
        {
            if (Phase != AreaPhase.Connecting)
            {
                return;
            }

            var count = Encoding.UTF8.GetBytes(mPendingName, 0, mPendingName.Length, mBody, 0);
            App.Net.Send(MyGame.NetMgr.Hello, mBody, count);
        }

        /// <summary>
        /// 连接失败或断开时回到菜单。
        /// </summary>
        /// <param name="message">失败原因。</param>
        private void OnFailed(MyGame.NetFailed message)
        {
            if (Phase == AreaPhase.Menu)
            {
                return;
            }

            App.Net.Stop();
            ResetRoom(AreaPhase.Menu, string.IsNullOrEmpty(message.Reason) ? "连接断开" : message.Reason);
        }

        /// <summary>
        /// 房主去掉一名断开的玩家并重发名单。
        /// </summary>
        /// <param name="message">关闭的连接。</param>
        private void OnPeerClosed(MyGame.NetPeerClosed message)
        {
            if (!mIsHost)
            {
                return;
            }

            if (!App.Room.TryGetName(message.PeerId, out _))
            {
                return;
            }

            App.Room.Leave(message.PeerId);
            mPicks.Remove(message.PeerId);
            BroadcastRoster();
            EmitRoster();
        }

        /// <summary>
        /// 按包类型分发。
        /// </summary>
        /// <param name="packet">收到的包。</param>
        private void OnPacket(MyGame.NetPacket packet)
        {
            if (packet.Type == MyGame.NetMgr.Hello)
            {
                OnHello(packet);
            }
            else if (packet.Type == MyGame.NetMgr.Leave)
            {
                OnPeerClosed(new MyGame.NetPeerClosed { PeerId = packet.PeerId });
            }
            else if (packet.Type == Welcome)
            {
                OnWelcome(packet);
            }
            else if (packet.Type == Roster)
            {
                OnRoster(packet);
            }
            else if (packet.Type == Round)
            {
                OnRound(packet);
            }
            else if (packet.Type == Pick)
            {
                OnPick(packet);
            }
            else if (packet.Type == Reveal)
            {
                OnReveal(packet);
            }
        }

        /// <summary>
        /// 房主接受一名玩家，满员则回绝。
        /// </summary>
        /// <param name="packet">加入包。</param>
        private void OnHello(MyGame.NetPacket packet)
        {
            if (!mIsHost || packet.PeerId <= 0)
            {
                return;
            }

            if (App.Room.TryGetName(packet.PeerId, out _))
            {
                return;
            }

            if (App.Room.PlayerCount >= MyGame.AreaGrid.RegionCount)
            {
                mBody[0] = 255;
                App.Net.SendTo(packet.PeerId, Welcome, mBody, 1);
                return;
            }

            var name = packet.Body == null || packet.Body.Length == 0
                ? "玩家"
                : ClipName(Encoding.UTF8.GetString(packet.Body));
            App.Room.Join(packet.PeerId, name);
            mBody[0] = (byte)packet.PeerId;
            App.Net.SendTo(packet.PeerId, Welcome, mBody, 1);
            BroadcastRoster();
            EmitRoster();
        }

        /// <summary>
        /// 记下房主分配的本机 id，或因满员回菜单。
        /// </summary>
        /// <param name="packet">欢迎包。</param>
        private void OnWelcome(MyGame.NetPacket packet)
        {
            if (mIsHost || packet.Body == null || packet.Body.Length < 1)
            {
                return;
            }

            if (packet.Body[0] == 255)
            {
                App.Net.Stop();
                ResetRoom(AreaPhase.Menu, "房间已满");
                return;
            }

            App.Room.SetLocalPlayerId(packet.Body[0]);
        }

        /// <summary>
        /// 用房主发来的名单替换本地名单。
        /// </summary>
        /// <param name="packet">名单包。</param>
        private void OnRoster(MyGame.NetPacket packet)
        {
            if (mIsHost || packet.Body == null || packet.Body.Length < 1)
            {
                return;
            }

            var count = packet.Body[0];
            var cursor = 1;
            var localId = App.Room.LocalPlayerId;
            App.Room.Clear();
            for (var i = 0; i < count; i++)
            {
                if (cursor + 2 > packet.Body.Length)
                {
                    break;
                }

                var id = packet.Body[cursor++];
                var length = packet.Body[cursor++];
                if (cursor + length > packet.Body.Length)
                {
                    break;
                }

                var name = Encoding.UTF8.GetString(packet.Body, cursor, length);
                cursor += length;
                App.Room.Join(id, name);
            }

            App.Room.SetLocalPlayerId(localId);
            if (Phase == AreaPhase.Connecting || Phase == AreaPhase.Menu)
            {
                SetPhase(AreaPhase.Room, "已加入，等待房主开始");
            }

            EmitRoster();
        }

        /// <summary>
        /// 按房主的种子和时长开始选择。
        /// </summary>
        /// <param name="packet">开局包。</param>
        private void OnRound(MyGame.NetPacket packet)
        {
            if (mIsHost || packet.Body == null || packet.Body.Length < 8)
            {
                return;
            }

            var duration = ReadInt(packet.Body, 4);
            if (duration < 1000 || duration > 60000)
            {
                return;
            }

            BeginRound(ReadInt(packet.Body, 0), duration);
        }

        /// <summary>
        /// 房主记下一名玩家的选择。
        /// </summary>
        /// <param name="packet">选择包。</param>
        private void OnPick(MyGame.NetPacket packet)
        {
            if (!mIsHost || packet.Body == null || packet.Body.Length < 1)
            {
                return;
            }

            AcceptPick(packet.PeerId, packet.Body[0]);
        }

        /// <summary>
        /// 按揭晓包进入结果。
        /// </summary>
        /// <param name="packet">揭晓包。</param>
        private void OnReveal(MyGame.NetPacket packet)
        {
            if (mIsHost || Phase != AreaPhase.Picking || packet.Body == null)
            {
                return;
            }

            mPicks.Clear();
            var count = packet.Body.Length > 0 ? packet.Body[0] : 0;
            var cursor = 1;
            for (var i = 0; i < count; i++)
            {
                if (cursor + 2 > packet.Body.Length)
                {
                    break;
                }

                var playerId = packet.Body[cursor++];
                var region = packet.Body[cursor++];
                if (region >= MyGame.AreaGrid.RegionCount)
                {
                    continue;
                }

                mPicks[playerId] = region;
            }

            mLocalPick = -1;
            int localRegion;
            if (mPicks.TryGetValue(App.Room.LocalPlayerId, out localRegion))
            {
                mLocalPick = localRegion;
            }

            mLeftMs = 0;
            SetPhase(AreaPhase.Result, "揭晓");
            App.Events.Emit(new AreaResultReady());
        }

        /// <summary>
        /// 在选择阶段记下一次尚未重复的选择。
        /// </summary>
        /// <param name="playerId">玩家 id。</param>
        /// <param name="region">颜色。</param>
        private void AcceptPick(int playerId, int region)
        {
            if (Phase != AreaPhase.Picking || mLeftMs <= 0)
            {
                return;
            }

            if (region < 0 || region >= MyGame.AreaGrid.RegionCount)
            {
                return;
            }

            if (!App.Room.TryGetName(playerId, out _) || mPicks.ContainsKey(playerId))
            {
                return;
            }

            mPicks.Add(playerId, region);
            if (playerId == App.Room.LocalPlayerId)
            {
                mLocalPick = region;
                App.Events.Emit(new AreaPickLocked());
            }
        }

        /// <summary>
        /// 用种子生成棋盘并进入选择阶段。
        /// </summary>
        /// <param name="seed">棋盘种子。</param>
        /// <param name="durationMs">选择时长，毫秒。</param>
        private void BeginRound(int seed, int durationMs)
        {
            mSeed = seed;
            mPicks.Clear();
            mLocalPick = -1;
            mGrid.Generate(seed, 80);
            mLeftMs = durationMs;
            mShownSecond = -1;
            SetPhase(AreaPhase.Picking, "点一块你觉得最大的地盘");
            App.Events.Emit(new AreaBoardReady());
            PublishCountdown();
        }

        /// <summary>
        /// 房主把已选结果发给所有人并进入揭晓。
        /// </summary>
        private void FinishRound()
        {
            if (!mIsHost || Phase != AreaPhase.Picking)
            {
                return;
            }

            App.Room.CopyPlayerIds(mIds);
            var cursor = 1;
            var count = 0;
            for (var i = 0; i < mIds.Count; i++)
            {
                int region;
                if (mIds[i] < 0 || mIds[i] > 255 || !mPicks.TryGetValue(mIds[i], out region) || cursor + 2 > mBody.Length)
                {
                    continue;
                }

                mBody[cursor++] = (byte)mIds[i];
                mBody[cursor++] = (byte)region;
                count++;
            }

            mBody[0] = (byte)count;
            App.Net.Send(Reveal, mBody, cursor);
            mLeftMs = 0;
            SetPhase(AreaPhase.Result, "揭晓");
            App.Events.Emit(new AreaResultReady());
        }

        /// <summary>
        /// 把当前名单发给所有连接。
        /// </summary>
        private void BroadcastRoster()
        {
            App.Room.CopyPlayerIds(mIds);
            var cursor = 1;
            var count = 0;
            for (var i = 0; i < mIds.Count; i++)
            {
                var id = mIds[i];
                if (id < 0 || id > 255)
                {
                    continue;
                }

                string name;
                if (!App.Room.TryGetName(id, out name))
                {
                    continue;
                }

                var bytes = Encoding.UTF8.GetByteCount(name);
                if (cursor + 2 + bytes > mBody.Length)
                {
                    break;
                }

                mBody[cursor++] = (byte)id;
                mBody[cursor++] = (byte)bytes;
                Encoding.UTF8.GetBytes(name, 0, name.Length, mBody, cursor);
                cursor += bytes;
                count++;
            }

            mBody[0] = (byte)count;
            App.Net.Send(Roster, mBody, cursor);
        }

        /// <summary>
        /// 剩余秒变化时派发倒计时。
        /// </summary>
        private void PublishCountdown()
        {
            var second = (mLeftMs + 999) / 1000;
            if (second == mShownSecond)
            {
                return;
            }

            mShownSecond = second;
            App.Events.Emit(new AreaCountdown { Seconds = second });
        }

        /// <summary>
        /// 清空本局并切到指定阶段。
        /// </summary>
        /// <param name="phase">目标阶段。</param>
        /// <param name="status">状态文案。</param>
        private void ResetRoom(AreaPhase phase, string status)
        {
            mIsHost = false;
            mPicks.Clear();
            mLocalPick = -1;
            mLeftMs = 0;
            App.Room.Clear();
            App.Room.SetLocalPlayerId(-1);
            SetPhase(phase, status);
            EmitRoster();
        }

        /// <summary>
        /// 写入阶段和文案并派发。
        /// </summary>
        /// <param name="phase">阶段。</param>
        /// <param name="status">状态文案。</param>
        private void SetPhase(AreaPhase phase, string status)
        {
            Phase = phase;
            mStatus = status ?? string.Empty;
            App.Events.Emit(new AreaPhaseChanged { Phase = phase, Status = mStatus });
        }

        /// <summary>
        /// 通知界面刷新名单。
        /// </summary>
        private void EmitRoster()
        {
            App.Events.Emit(new AreaRosterChanged());
        }

        /// <summary>
        /// 按小端读取 4 字节整数。
        /// </summary>
        /// <param name="body">缓冲。</param>
        /// <param name="offset">起始下标。</param>
        /// <returns>整数值。</returns>
        private static int ReadInt(byte[] body, int offset)
        {
            return body[offset]
                | (body[offset + 1] << 8)
                | (body[offset + 2] << 16)
                | (body[offset + 3] << 24);
        }

        /// <summary>
        /// 按小端写入 4 字节整数。
        /// </summary>
        /// <param name="body">缓冲。</param>
        /// <param name="offset">起始下标。</param>
        /// <param name="value">整数值。</param>
        private static void WriteInt(byte[] body, int offset, int value)
        {
            body[offset] = (byte)value;
            body[offset + 1] = (byte)(value >> 8);
            body[offset + 2] = (byte)(value >> 16);
            body[offset + 3] = (byte)(value >> 24);
        }

        /// <summary>
        /// 把名字收成不超过 8 个字符、UTF-8 不超过 24 字节。
        /// </summary>
        /// <param name="name">原名字。</param>
        /// <returns>可用名字。</returns>
        private static string ClipName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "玩家";
            }

            name = name.Trim();
            if (name.Length == 0)
            {
                return "玩家";
            }

            if (name.Length > 8)
            {
                name = name.Substring(0, 8);
            }

            while (Encoding.UTF8.GetByteCount(name) > 24)
            {
                name = name.Substring(0, name.Length - 1);
            }

            return name;
        }

        /// <summary>
        /// 去掉地址里的端口。
        /// </summary>
        /// <param name="host">用户输入的地址。</param>
        /// <returns>主机名或空字符串。</returns>
        private static string ClipHost(string host)
        {
            if (string.IsNullOrEmpty(host))
            {
                return string.Empty;
            }

            host = host.Trim();
            var colon = host.LastIndexOf(':');
            if (colon > 0)
            {
                host = host.Substring(0, colon);
            }

            return host;
        }

        /// <summary>
        /// 取本机一个非回环 IPv4，失败时用 127.0.0.1。
        /// </summary>
        /// <returns>地址文本。</returns>
        private static string LocalAddress()
        {
            try
            {
                var entry = Dns.GetHostEntry(Dns.GetHostName());
                for (var i = 0; i < entry.AddressList.Length; i++)
                {
                    var address = entry.AddressList[i];
                    if (address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(address))
                    {
                        continue;
                    }

                    return address.ToString();
                }
            }
            catch (Exception)
            {
            }

            return "127.0.0.1";
        }
    }
}
