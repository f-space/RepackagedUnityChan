using UnityEngine;

namespace UnityChan
{
	// IKの挙動を確認するためのスクリプト
	[RequireComponent(typeof(Animator))]
	public class IKTarget : MonoBehaviour
	{
		[Tooltip("有効かどうか。")]
		public bool IsActive = true;

		[Tooltip("対象とするIKゴール。")]
		public AvatarIKGoal IKGoal = AvatarIKGoal.RightHand;

		[Tooltip("IKの目的地点。")]
		public Transform Target = null;

		[Tooltip("IKのブレンド比率。")]
		[Range(0.0f, 1.0f)]
		public float Weight = 1.0f;

		[Tooltip("GUIを表示するかどうか。")]
		public bool ShowGUI = true;

		// アタッチされているアニメーター
		private Animator animator;

		// メンバーの初期化
		private void Awake()
		{
			animator = GetComponent<Animator>();
		}

		// IKの設定
		private void OnAnimatorIK(int layerIndex)
		{
			if (IsActive && Target)
			{
				animator.SetIKPositionWeight(IKGoal, Weight);
				animator.SetIKRotationWeight(IKGoal, Weight);
				animator.SetIKPosition(IKGoal, Target.position);
				animator.SetIKRotation(IKGoal, Target.rotation);
			}
		}

		// GUIの描画（旧形式・デバッグ用）
		private void OnGUI()
		{
			if (ShowGUI)
			{
				Rect area = new Rect(10, Screen.height - 60, 400, 20);
				GUILayout.BeginArea(area);
				this.IsActive = GUILayout.Toggle(this.IsActive, "IK Active");
				GUILayout.EndArea();
			}
		}

	}
}


