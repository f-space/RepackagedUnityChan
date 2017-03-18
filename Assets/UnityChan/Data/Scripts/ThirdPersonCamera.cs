using System;
using UnityEngine;

namespace UnityChan
{
	// 被写体に追従するためのカメラ操作スクリプト
	[RequireComponent(typeof(Camera))]
	public class ThirdPersonCamera : MonoBehaviour
	{
		// 視点モードの設定
		[Serializable]
		public class ViewSettings
		{
			[Tooltip("カメラの視点。")]
			public Vector3 LookFrom;

			[Tooltip("カメラの注視点。")]
			public Vector3 LookAt;

			[Tooltip("滑らかな移動にかける時間(s)。")]
			public float SmoothTime;
		}

		[Tooltip("カメラの被写体。")]
		public Transform Target;

		[Tooltip("通常モードのカメラ設定。")]
		public ViewSettings NormalSettings;

		[Tooltip("煽りモードのカメラ設定。")]
		public ViewSettings LowAngleSettings;

		[Tooltip("正面モードのカメラ設定。")]
		public ViewSettings FrontSettings;

		[Tooltip("通常モードと煽りモードを切り替える際の遷移時間(s)。")]
		public float TransitionTime;

		// カメラの速度
		private Vector3 velocity;

		// 通常モードと煽りモードの間の遷移状態（0:通常モード 1:煽りモード）
		private float transition;

		// 正面モードかどうか
		private bool frontMode;

		// 有効化時
		private void OnEnable()
		{
			ResetMotion();
		}

		// 状態更新
		private void Update()
		{
			if (Target)
			{
				// 入力に応じてモード切替
				bool frontMode = Input.GetKey(KeyCode.LeftControl);
				bool lowAngleMode = Input.GetKey(KeyCode.LeftAlt);

				SetFrontMode(frontMode);
				MakeTransition(lowAngleMode, Time.deltaTime);
			}
		}

		// 遅めの状態更新
		private void LateUpdate()
		{
			if (Target)
			{
				UpdateMotion(Time.deltaTime);
				LookAtTarget();
			}
		}

		// 正面モードかどうかの設定
		private void SetFrontMode(bool value)
		{
			if (this.frontMode ^ value)
			{
				this.frontMode = value;

				ResetMotion();
			}
		}

		// 通常モードあるいは煽りモードへ遷移
		private void MakeTransition(bool toLowAngle, float deltaTime)
		{
			float direction = (toLowAngle ? 1.0f : -1.0f);
			float delta = (deltaTime / TransitionTime) * direction;

			this.transition = Mathf.Clamp01(this.transition + delta);
		}

		// カメラの位置をリセット
		private void ResetMotion()
		{
			transform.position = GetCurrentLookFromPoint();
			velocity = Vector3.zero;
		}

		// カメラの移動状態を更新
		private void UpdateMotion(float deltaTime)
		{
			Vector3 current = transform.position;
			Vector3 target = GetCurrentLookFromPoint();
			float smoothTime = GetCurrentSmoothTime();
			float maxSpeed = Single.PositiveInfinity;

			transform.position = Vector3.SmoothDamp(current, target, ref velocity, smoothTime, maxSpeed, deltaTime);
		}

		// カメラを注視点の方へ向ける
		private void LookAtTarget()
		{
			Vector3 from = transform.position;
			Vector3 to = GetCurrentLookAtPoint();
			Vector3 forward = to - from;

			if (forward != Vector3.zero)
			{
				transform.rotation = Quaternion.LookRotation(forward);
			}
		}

		// 現在モードにおける滑らかな移動にかける時間を取得
		private float GetCurrentSmoothTime()
		{
			if (frontMode)
			{
				return FrontSettings.SmoothTime;
			}
			else
			{
				return Mathf.Lerp(NormalSettings.SmoothTime, LowAngleSettings.SmoothTime, transition);
			}
		}

		// 現在モードにおける視点を取得
		private Vector3 GetCurrentLookFromPoint()
		{
			if (frontMode)
			{
				return GetLookFromPoint(FrontSettings);
			}
			else
			{
				Vector3 p0 = GetLookFromPoint(NormalSettings);
				Vector3 p1 = GetLookFromPoint(LowAngleSettings);

				return Vector3.Lerp(p0, p1, transition);
			}
		}

		// 現在モードにおける注視点を取得
		private Vector3 GetCurrentLookAtPoint()
		{
			if (frontMode)
			{
				return GetLookAtPoint(FrontSettings);
			}
			else
			{
				Vector3 p0 = GetLookAtPoint(NormalSettings);
				Vector3 p1 = GetLookAtPoint(LowAngleSettings);

				return Vector3.Lerp(p0, p1, transition);
			}
		}

		// 指定したモード設定から視点を取得
		private Vector3 GetLookFromPoint(ViewSettings settings)
		{
			return Target.TransformPoint(settings.LookFrom);
		}

		// 指定したモード設定から注視点を取得
		private Vector3 GetLookAtPoint(ViewSettings settings)
		{
			return Target.TransformPoint(settings.LookAt);
		}

#if UNITY_EDITOR // エディタでのみ有効

		// エディタ編集時
		private void OnValidate()
		{
			this.NormalSettings.SmoothTime = Mathf.Max(this.NormalSettings.SmoothTime, 0.0f);
			this.LowAngleSettings.SmoothTime = Mathf.Max(this.LowAngleSettings.SmoothTime, 0.0f);
			this.FrontSettings.SmoothTime = Mathf.Max(this.FrontSettings.SmoothTime, 0.0f);
			this.TransitionTime = Mathf.Max(this.TransitionTime, 0.0f);
		}

#endif

	}
}


