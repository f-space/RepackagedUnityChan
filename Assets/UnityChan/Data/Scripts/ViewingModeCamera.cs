using UnityEngine;

namespace UnityChan
{
	// 被写体をあらゆる角度から眺めるためのカメラ操作スクリプト
	[RequireComponent(typeof(Camera))]
	public class ViewingModeCamera : MonoBehaviour
	{
		// ドラッグ操作の種類
		private enum DragOperation
		{
			None,      // 操作なし
			Tumble,    // 回転
			Track,     // 平行移動
		}

		// 各マウスボタンの番号
		private const int LeftMouseButton = 0;
		private const int RightMouseButton = 1;
		private const int MiddleMouseButton = 2;

		// カメラの回転速度(deg/vmax)
		private const float RotationSpeed = 180.0f;

		// カメラの平行移動速度(m/vmax)
		private const float TranslationSpeed = 5.0f;

		// カメラの接近速度(m/notch)
		private const float ApproachSpeed = 0.1f;

		[Tooltip("カメラの被写体。")]
		public Transform Target;

		[Tooltip("カメラの被写体上の注視位置。")]
		public Vector3 TargetOffset;

		[Tooltip("ヘルプを表示するかどうか。")]
		public bool ShowHelp = true;

		// アタッチされているカメラ
		private Camera cameraCache;

		// カメラの平行移動量
		private Vector3 translation;

		// カメラの回転量
		private Quaternion rotation;

		// 注視点からの距離
		private float distance;

		// 現在のドラッグ操作
		private DragOperation dragOperation;

		// ドラッグ操作中のボタン
		private int dragButton;

		// ドラッグ操作中の前フレームのマウス位置
		private Vector2 previousPosition;

		// メンバーの初期化
		private void Awake()
		{
			this.cameraCache = GetComponent<Camera>();
		}

		// 有効化時
		private void OnEnable()
		{
			ResetCamera();
		}

		// 状態更新
		private void Update()
		{
			UpdateDragState();
			UpdateRelativePosition();
		}

		// 遅めの状態更新
		private void LateUpdate()
		{
			UpdateTransform();
		}

		// GUIの描画（旧形式・デバッグ用）
		private void OnGUI()
		{
			if (ShowHelp)
			{
				// ヘルプの表示
				Rect area = new Rect(Screen.width - 210, Screen.height - 110, 200, 100);
				GUILayout.BeginArea(area, GUI.skin.box);
				GUILayout.Label("Camera Operations:");
				GUILayout.Label("RMB / Alt+LMB: Tumble");
				GUILayout.Label("MMB / Alt+Cmd+LMB: Track");
				GUILayout.Label("Wheel / 2 Fingers Swipe: Dolly");
				GUILayout.EndArea();
			}
		}

		// ビューポートの長辺の長さ(px)[=1vmax]を取得
		private float GetViewportMax()
		{
			return Mathf.Max(this.cameraCache.pixelWidth, this.cameraCache.pixelHeight);
		}

		// 注視点の位置を取得
		private Vector3 GetTargetPosition()
		{
			return (Target ? Target.position : Vector3.zero) + TargetOffset;
		}

		// 現在の配置でカメラの相対位置をリセット
		private void ResetCamera()
		{
			// 注視点、視点、回転量を取得
			Vector3 targetPosition = GetTargetPosition();
			Vector3 position = transform.position;
			Quaternion rotation = transform.rotation;

			// 注視点からの距離と平行移動量を決定
			Vector3 toTarget = targetPosition - position;
			Vector3 forward = rotation * Vector3.forward;
			float distance = Mathf.Max(Vector3.Dot(toTarget, forward), 0.0f);
			Vector3 translation = forward * distance - toTarget;

			// 相対位置をリセット
			this.rotation = rotation;
			this.distance = distance;
			this.translation = translation;
		}

		// ドラッグ状態の更新
		private void UpdateDragState()
		{
			if (this.dragOperation == DragOperation.None)
			{
				// ドラッグ中でないならドラッグの開始を検出

				if (Input.GetMouseButtonDown(LeftMouseButton) && Input.GetKey(KeyCode.LeftAlt))
				{
					if (Input.GetKey(KeyCode.LeftCommand))
					{
						// Track: Alt + Cmd + LMB (for Mac)
						BeginDrag(DragOperation.Track, LeftMouseButton, Input.mousePosition);
					}
					else
					{
						// Tumble: Alt + LMB (for Mac)
						BeginDrag(DragOperation.Tumble, LeftMouseButton, Input.mousePosition);
					}
				}
				else if (Input.GetMouseButtonDown(RightMouseButton))
				{
					// Tumble: RMB
					BeginDrag(DragOperation.Tumble, RightMouseButton, Input.mousePosition);
				}
				else if (Input.GetMouseButtonDown(MiddleMouseButton))
				{
					// Track: MMB
					BeginDrag(DragOperation.Track, MiddleMouseButton, Input.mousePosition);
				}
			}
			else
			{
				// ドラッグ中ならドラッグの終了を検出

				if (Input.GetMouseButtonUp(this.dragButton))
				{
					EndDrag();
				}
			}
		}

		// ドラッグを開始
		private void BeginDrag(DragOperation operation, int button, Vector2 position)
		{
			this.dragOperation = operation;
			this.dragButton = button;
			this.previousPosition = position;
		}

		// ドラッグを終了
		private void EndDrag()
		{
			this.dragOperation = DragOperation.None;
			this.dragButton = -1;
			this.previousPosition = Vector2.zero;
		}

		// 注視点を基準とするカメラの相対位置を更新
		private void UpdateRelativePosition()
		{
			Tumble();
			Track();
			Dolly();
		}

		// カメラを回転
		private void Tumble()
		{
			if (this.dragOperation == DragOperation.Tumble)
			{
				// 前フレームからのマウス移動量(px)をもとに回転量(deg)を計算
				Vector2 position = Input.mousePosition;
				Vector2 delta = position - this.previousPosition;
				Vector2 amount = (delta * RotationSpeed) / GetViewportMax();

				if (amount.sqrMagnitude > Vector2.kEpsilon * Vector2.kEpsilon)
				{
					// 縦方向、横方向の順に回転を加える
					Quaternion pitch = Quaternion.AngleAxis(amount.y, this.rotation * Vector3.left);
					Quaternion yaw = Quaternion.AngleAxis(amount.x, Vector3.up);
					this.rotation = yaw * pitch * this.rotation;
				}

				// 次フレームのために現在フレームの位置を保存
				this.previousPosition = position;
			}
		}

		// カメラを平行移動
		private void Track()
		{
			if (this.dragOperation == DragOperation.Track)
			{
				// 前フレームからのマウス移動量(px)をもとに移動量(m)を計算
				Vector2 position = Input.mousePosition;
				Vector2 delta = position - this.previousPosition;
				Vector2 amount = (delta * TranslationSpeed) / GetViewportMax();

				if (amount.sqrMagnitude > Vector2.kEpsilon * Vector2.kEpsilon)
				{
					// カメラの向きに直交する面上を平行移動する
					this.translation -= this.rotation * amount;
				}

				// 次フレームのために現在フレームの位置を保存
				this.previousPosition = position;
			}
		}

		// カメラを前後移動
		private void Dolly()
		{
			// マウスホイール回転量をもとに移動量(m)を計算
			float delta = Input.mouseScrollDelta.y;
			float amount = delta * ApproachSpeed;

			if (Mathf.Abs(amount) > Mathf.Epsilon)
			{
				// カメラの向きに合わせて前後移動する
				this.distance = Mathf.Max(this.distance - amount, 0.0f);
			}
		}

		// カメラのTransformを更新
		private void UpdateTransform()
		{
			// 注視点の位置とそこからの相対位置を計算
			Vector3 targetPosition = GetTargetPosition();
			Vector3 displacement = (this.rotation * Vector3.back) * this.distance + this.translation;

			// Transformに反映
			transform.position = targetPosition + displacement;
			transform.rotation = this.rotation;
		}
	}
}


