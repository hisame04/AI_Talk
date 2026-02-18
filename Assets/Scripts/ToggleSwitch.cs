using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Christina.UI
{
    public class ToggleSwitch : MonoBehaviour, IPointerClickHandler
    {
        [Header("Slider setup")]
        [SerializeField, Range(0, 1f)]
        protected float sliderValue;
        public bool CurrentValue { get; private set; }

        private bool _previousValue;
        private Slider _slider;

        [Header("Animation")]
        [SerializeField, Range(0, 1f)] private float animationDuration = 0.5f;
        [SerializeField]
        private AnimationCurve slideEase =
            AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Coroutine _animateSliderCoroutine;

        [Header("Events")]
        [SerializeField] private UnityEvent onToggleOn;
        [SerializeField] private UnityEvent onToggleOff;
        [SerializeField] private Image background;
        [SerializeField] private Color onColor = Color.green;
        [SerializeField] private Color offColor = Color.gray;

        private ToggleSwitchGroupManager _toggleSwitchGroupManager;

        protected Action transitionEffect;

        // インスペクター変更時に初期化を行い、スライダー値を反映する
        protected virtual void OnValidate()
        {
            SetupToggleComponents();

            _slider.value = sliderValue;
        }

        // 必要なコンポーネントが未セットなら初期化する
        private void SetupToggleComponents()
        {
            if (_slider != null)
                return;

            SetupSliderComponent();
        }

        // Sliderコンポーネントの取得と見た目・操作設定を行う
        private void SetupSliderComponent()
        {
            _slider = GetComponent<Slider>();

            if (_slider == null)
            {
                Debug.Log("No slider found!", this);
                return;
            }

            _slider.interactable = false;
            var sliderColors = _slider.colors;
            sliderColors.disabledColor = Color.white;
            _slider.colors = sliderColors;
            _slider.transition = Selectable.Transition.None;
            ApplyBackgroundColor(_slider.value);
        }

        // グループ管理用の参照をセットする
        public void SetupForManager(ToggleSwitchGroupManager manager)
        {
            _toggleSwitchGroupManager = manager;
        }


        // 起動時にスライダー設定を確実に行う
        protected virtual void Awake()
        {
            SetupSliderComponent();
        }

        // クリックを受け取ったらトグル処理を開始する
        public void OnPointerClick(PointerEventData eventData)
        {
            Toggle();
        }


        // 単体かグループかで切り替え処理を振り分ける
        private void Toggle()
        {
            if (_toggleSwitchGroupManager != null)
                _toggleSwitchGroupManager.ToggleGroup(this);
            else
                SetStateAndStartAnimation(!CurrentValue);
        }

        // グループ管理から呼ばれる状態変更処理
        public void ToggleByGroupManager(bool valueToSetTo)
        {
            SetStateAndStartAnimation(valueToSetTo);
        }


        // 状態更新・イベント発火・アニメーション開始をまとめて行う
        private void SetStateAndStartAnimation(bool state)
        {
            _previousValue = CurrentValue;
            CurrentValue = state;

            if (_previousValue != CurrentValue)
            {
                if (CurrentValue)
                    onToggleOn?.Invoke();
                else
                    onToggleOff?.Invoke();
            }

            if (_animateSliderCoroutine != null)
                StopCoroutine(_animateSliderCoroutine);

            _animateSliderCoroutine = StartCoroutine(AnimateSlider());
        }

        //背景色を切り替える
        private void ApplyBackgroundColor(float value)
        {
            if (background == null) return;
            background.color = (value >= 0.5f) ? onColor : offColor;
        }


        // スライダーをアニメーションさせて見た目を切り替える
        private IEnumerator AnimateSlider()
        {
            float startValue = _slider.value;
            float endValue = CurrentValue ? 1 : 0;

            float time = 0;
            if (animationDuration > 0)
            {
                while (time < animationDuration)
                {
                    time += Time.deltaTime;

                    float lerpFactor = slideEase.Evaluate(time / animationDuration);
                    _slider.value = sliderValue = Mathf.Lerp(startValue, endValue, lerpFactor);

                    transitionEffect?.Invoke();

                    yield return null;
                }
            }

            _slider.value = endValue;
            ApplyBackgroundColor(endValue);
        }
    }
}
