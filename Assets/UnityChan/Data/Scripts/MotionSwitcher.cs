using UnityEngine;

namespace UnityChan
{
	// モーションを切り替えるためのスクリプト
	[RequireComponent(typeof(Animator))]
	public class MotionSwitcher : MonoBehaviour
	{
		[Tooltip("GUIを表示するかどうか。")]
		public bool ShowGUI = true;

		// アタッチされているアニメーター
		private Animator animator;

		// メンバーの初期化
		private void Awake()
		{
			animator = GetComponent<Animator>();
		}

		// 状態更新
		private void Update()
		{
			// ←が押されたら前のモーションへ
			if (Input.GetKeyDown(KeyCode.LeftArrow))
			{
				Back();
			}

			// →かスペースが押されたら次のモーションへ
			if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Space))
			{
				Next();
			}
		}

		// GUIの描画（旧形式・デバッグ用）
		private void OnGUI()
		{
			if (ShowGUI)
			{
				Rect area = new Rect(Screen.width - 160, 10, 150, 60);
				GUILayout.BeginArea(area, GUI.skin.box);
				GUILayout.Label("Motion");
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("Back")) Back();
				if (GUILayout.Button("Next")) Next();
				GUILayout.EndHorizontal();
				GUILayout.EndArea();
			}
		}

		// 前のモーションへ
		public void Back()
		{
			animator.SetTrigger("Back");
		}

		// 次のモーションへ
		public void Next()
		{
			animator.SetTrigger("Next");
		}
	}
}



