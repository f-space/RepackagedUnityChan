using UnityEngine;

namespace UnityChan
{
	// ランダムな方向に風を吹かせるスクリプト
	[RequireComponent(typeof(DynamicsManager))]
	public class RandomWind : MonoBehaviour
	{
		[Tooltip("有効かどうか。")]
		public bool IsActive = true;

		[Tooltip("風の強さ。")]
		public float Power = 1.0f;

		[Tooltip("風向きの変化の速さ。")]
		[Range(0.0f, 1.0f)]
		public float ChangeSpeed = 0.5f;

		[Tooltip("GUIを表示するかどうか。")]
		public bool ShowGUI = true;

		// アタッチされている揺れものマネージャー
		private DynamicsManager manager;

		// メンバーの初期化
		private void Awake()
		{
			this.manager = GetComponent<DynamicsManager>();
		}

		// 無効化時
		private void OnDisable()
		{
			ResetWind();
		}

		// 状態更新
		private void Update()
		{
			if (IsActive)
			{
				UpdateWind();
			}
			else
			{
				ResetWind();
			}
		}

		// GUIの描画（旧形式・デバッグ用）
		private void OnGUI()
		{
			if (ShowGUI)
			{
				Rect area = new Rect(10, Screen.height - 30, 400, 20);
				GUILayout.BeginArea(area);
				this.IsActive = GUILayout.Toggle(this.IsActive, "Random Wind");
				GUILayout.EndArea();
			}
		}

		// 風の状態をリセット
		private void ResetWind()
		{
			manager.ExternalForce = Vector3.zero;
		}

		// 風の状態を更新
		private void UpdateWind()
		{
			float t = Time.time * ChangeSpeed;
			float x = Mathf.PerlinNoise(t, 0.0f) * 2.0f - 1.0f;
			float y = Mathf.PerlinNoise(0.0f, t) * 2.0f - 1.0f;
			float z = Mathf.PerlinNoise(-t, -t) * 2.0f - 1.0f;
			Vector3 direction = Vector3.Normalize(new Vector3(x, y, z));

			manager.ExternalForce = direction * Power;
		}
	}
}


