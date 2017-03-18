using UnityEngine;

namespace UnityChan
{
	// 自動的に瞬きをするためのスクリプト
	public class AutoBlink : MonoBehaviour
	{
		// 目の開き具合
		private enum State
		{
			Open,       // 開いている
			HalfClosed, // 半目
			Closed,     // 閉じている
		}

		[Tooltip("目を開いた状態のブレンドシェイプ比率。")]
		[Range(0.0f, 100.0f)]
		public float WeightOnOpen = 0.0f;

		[Tooltip("半目状態のブレンドシェイプ比率。")]
		[Range(0.0f, 100.0f)]
		public float WeightOnHalfClosed = 20.0f;

		[Tooltip("目を閉じた状態のブレンドシェイプ比率。")]
		[Range(0.0f, 100.0f)]
		public float WeightOnClosed = 85.0f;

		[Tooltip("瞬きの最小間隔(s)。")]
		public float MinInterval = 2.0f;

		[Tooltip("瞬きの最大間隔(s)。")]
		public float MaxInterval = 6.0f;

		[Tooltip("瞬きにかける時間(s)")]
		public float Duration = 0.15f;

		[Tooltip("瞬きのうち目を閉じている時間の割合")]
		[Range(0.0f, 1.0f)]
		public float ClosedTimeRate = 0.75f;

		// 瞬き時に動く部位の一覧
		[SerializeField]
		[HideInInspector]
		private BlinkablePart[] parts;

		// 瞬きの開始時刻
		private float startTime;

		// 次の瞬きの時刻
		private float nextTime;

		// 状態更新
		private void Update()
		{
			UpdateTime();
		}

		// 遅めの状態更新
		private void LateUpdate()
		{
			// アニメーターによる更新結果を上書きするため状態反映を遅延
			UpdateState();
		}

		// 次の瞬きの時刻になっていないか判定
		private void UpdateTime()
		{
			float time = Time.time;
			if (time > this.nextTime)
			{
				// 瞬きを開始
				this.startTime = time;

				// 次の瞬きの時刻を決定
				this.nextTime += Mathf.Lerp(MinInterval, MaxInterval, Random.value);
			}
		}

		// 瞬きの状態を更新
		private void UpdateState()
		{
			State state = GetCurrentState();

			ReflectEyeState(state);
		}

		// 現在の瞬きの状態を決定
		private State GetCurrentState()
		{
			float elapsedTime = Time.time - this.startTime;
			float progress = elapsedTime / Duration;

			State state;
			if (progress < ClosedTimeRate)
			{
				state = State.Closed;
			}
			else if (progress < 1.0f)
			{
				state = State.HalfClosed;
			}
			else
			{
				state = State.Open;
			}

			return state;
		}

		// 目の開き具合を設定
		private void ReflectEyeState(State state)
		{
			switch (state)
			{
				case State.Open:
					SetPartsWeight(WeightOnOpen);
					break;
				case State.HalfClosed:
					SetPartsWeight(WeightOnHalfClosed);
					break;
				case State.Closed:
					SetPartsWeight(WeightOnClosed);
					break;
				default:
					goto case State.Open;
			}
		}

		// 各部位に現在の目の開き具合を知らせる
		private void SetPartsWeight(float weight)
		{
			foreach (var part in this.parts)
			{
				part.SetWeight(weight);
			}
		}

#if UNITY_EDITOR // エディタでのみ有効

		// リセット時
		private void Reset()
		{
			SaveBlinkableParts();
		}

		// エディタ編集時
		private void OnValidate()
		{
			this.MinInterval = Mathf.Max(this.MinInterval, 0.0f);
			this.MaxInterval = Mathf.Max(this.MaxInterval, 0.0f);
			this.Duration = Mathf.Max(this.Duration, 0.0f);

			SaveBlinkableParts();
		}

		// 瞬き時に動く部位を検索して記憶
		[ContextMenu("Refresh")]
		private void SaveBlinkableParts()
		{
			// partsはSerializeField属性によってシリアライズされる（ゲームデータとして保存される）
			this.parts = GetComponentsInChildren<BlinkablePart>(true);
		}

#endif

	}
}


