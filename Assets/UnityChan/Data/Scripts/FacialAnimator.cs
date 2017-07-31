using System;
using UnityEngine;

namespace UnityChan
{
	// フェイスアニメーション一覧
	public enum FacialAnimation
	{
		Default,       // デフォルト
		Smiling,       // 微笑み
		Beaming,       // 笑顔
		Grumpy,        // ムッ！
		Angry,         // 怒り
		Confounded,    // 困惑
		Moody,         // 面白くない
		Smirking,      // ニヤニヤ
		Embarrassed,   // 恥ずかしい
		Surprised,     // えっ？
		Astonished,    // 驚き
		EyeClosed,     // 目を閉じる
		MouthA,        // 「あ」の口
		MouthI,        // 「い」の口
		MouthU,        // 「う」の口
		MouthE,        // 「え」の口
		MouthO,        // 「お」の口
	}

	// フェイスアニメーションのためのスクリプト
	[RequireComponent(typeof(Animator))]
	public class FacialAnimator : MonoBehaviour
	{
		[Tooltip("対象となるアニメーターのレイヤー名。")]
		public string LayerName = "Face";

		[Tooltip("フェードにかける時間(s)。")]
		public float FadeDuration = 0.1f;

		[Tooltip("フェイスアニメーション継続時間(s)のデフォルト値。")]
		public float DefaultDuration = Single.PositiveInfinity;

		[Tooltip("フェイスアニメーションチェック用のGUIを表示するかどうか。")]
		public bool ShowGUI = true;

		// アタッチされているアニメーター
		private Animator animator;

		// フェイスアニメーションのレイヤー番号
		private int layerIndex;

		// アニメーションの終了時刻
		private float endTime;

		// フェイスアニメーションの名前一覧
		private static readonly string[] Names = Enum.GetNames(typeof(FacialAnimation));

		// メンバーの初期化
		private void Awake()
		{
			this.animator = GetComponent<Animator>();
			this.layerIndex = animator.GetLayerIndex(LayerName);
			this.endTime = Single.PositiveInfinity;

			if (this.layerIndex < 0)
			{
				// 指定された名前のレイヤーがアニメーターに存在しなければ破棄して終了
				Debug.LogErrorFormat(gameObject, "'{0}' layer not found.", LayerName);

				Destroy(this);
			}
		}

		// 有効化時
		private void OnEnable()
		{
			if (animator.isActiveAndEnabled)
			{
				animator.SetLayerWeight(layerIndex, 1.0f);
			}
		}

		// 無効化時
		private void OnDisable()
		{
			if (animator.isActiveAndEnabled)
			{
				animator.SetLayerWeight(layerIndex, 0.0f);
			}
		}

		// 状態更新
		private void Update()
		{
			UpdateFace();
			UpdateLayerWeight();
		}

		// GUIの描画（旧形式・デバッグ用）
		private void OnGUI()
		{
			if (ShowGUI)
			{
				Rect area = new Rect(10, 10, 150, 30 + 25 * Names.Length);
				GUILayout.BeginArea(area, GUI.skin.box);
				GUILayout.Label("Face Animation");
				for (int i = 0; i < Names.Length; i++)
				{
					if (GUILayout.Button(Names[i]))
					{
						SetFace((FacialAnimation)i);
					}
				}
				GUILayout.EndArea();
			}
		}

		// フェイスアニメーションを設定
		public void SetFace(FacialAnimation face)
		{
			SetFace(face, DefaultDuration);
		}

		// フェイスアニメーションを設定
		public void SetFace(FacialAnimation face, float duration)
		{
			animator.CrossFade(Names[(int)face], FadeDuration, this.layerIndex);

			this.endTime = Time.time + Math.Max(duration - FadeDuration, 0.0f);
		}

		// 名前を指定してフェイスアニメーションを設定
		public void SetFace(string face)
		{
			SetFace(face, DefaultDuration);
		}

		// 名前を指定してフェイスアニメーションを設定
		public void SetFace(string face, float duration)
		{
			SetFace((FacialAnimation)Enum.Parse(typeof(FacialAnimation), face), duration);
		}

		// フェイスアニメーションの状態を更新
		private void UpdateFace()
		{
			if (Time.time > this.endTime)
			{
				SetFace(FacialAnimation.Default, Single.PositiveInfinity);
			}
		}

		// フェイスアニメーションのブレンド比率を更新
		private void UpdateLayerWeight()
		{
			AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(this.layerIndex);
			bool inTransition = animator.IsInTransition(this.layerIndex);
			bool isDefault = stateInfo.IsName(Names[(int)FacialAnimation.Default]);
			if (!inTransition && isDefault)
			{
				animator.SetLayerWeight(this.layerIndex, 0.0f);
			}
			else
			{
				animator.SetLayerWeight(this.layerIndex, 1.0f);
			}
		}

#if UNITY_EDITOR // エディタでのみ有効

		// エディタ編集時
		private void OnValidate()
		{
			this.FadeDuration = Mathf.Max(this.FadeDuration, 0.0f);
			this.DefaultDuration = Mathf.Max(this.DefaultDuration, 0.0f);
		}

#endif

	}
}
