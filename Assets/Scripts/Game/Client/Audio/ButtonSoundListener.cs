//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: rongaihua
// Date: 2026-10-23
// Desc: 按钮和开关点击时播放音效
//---------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;

namespace LargeScaleBattle
{

    /// <summary>
    /// 挂在按钮或开关上。点击按钮、或开关状态变化时，播放 <see cref="SoundPath"/>。
    /// </summary>
    public sealed class ButtonSoundListener : MonoBehaviour
    {

        /// <summary>
        /// 点击时播放的音效路径。为空则不播。
        /// </summary>
        public string SoundPath;

        /// <summary>
        /// 为 true 时使用通用点击声。单独配置的音效保持原路径。
        /// </summary>
        public bool IsCommon;

        /// <summary>
        /// 所在按钮。
        /// </summary>
        private Button mButton;

        /// <summary>
        /// 所在开关。
        /// </summary>
        private Toggle mToggle;

        /// <summary>
        /// 接上按钮或开关。
        /// </summary>
        private void OnEnable()
        {
            mButton = GetComponent<Button>();
            if (mButton != null)
            {
                mButton.onClick.AddListener(PlaySound);
                return;
            }

            mToggle = GetComponent<Toggle>();
            if (mToggle != null)
            {
                mToggle.onValueChanged.AddListener(OnValueChanged);
            }
        }

        /// <summary>
        /// 卸下按钮或开关。
        /// </summary>
        private void OnDisable()
        {
            if (mButton != null)
            {
                mButton.onClick.RemoveListener(PlaySound);
            }

            if (mToggle != null)
            {
                mToggle.onValueChanged.RemoveListener(OnValueChanged);
            }

            mButton = null;
            mToggle = null;
        }

        /// <summary>
        /// 开关状态变了就播一声。
        /// </summary>
        /// <param name="isOn">当前是否打开。</param>
        private void OnValueChanged(bool isOn)
        {
            PlaySound();
        }

        /// <summary>
        /// 让音效管理器播放当前路径。
        /// </summary>
        private void PlaySound()
        {
            if (string.IsNullOrEmpty(SoundPath) || AppEntry.Current == null || AppEntry.Current.App == null)
            {
                return;
            }

            var sound = AppEntry.Current.App.Find<MyGame.SoundMgr>();
            if (sound == null)
            {
                return;
            }

            sound.Play2D(SoundPath);
        }
    }
}
