using UnityEngine;

namespace UnityChan
{
	// ユニティちゃんの状態遷移を補助するスクリプト
	public class UnityChanStateController : StateMachineBehaviour
	{
		// 踏み切り時と着地時のメッセージ
		private const string TakeOffMessage = "OnTakeOff";
		private const string LandingMessage = "OnLanding";

		// ステート名とパラメータ名
		private const string RisingStateName = "Rising";
		private const string LandingStateName = "Landing";
		private const string TimeParamName = "Time";
		private const string JumpParamName = "Jump";

		// ステートとパラメータのハッシュ(ID)
		private static readonly int RisingState = Animator.StringToHash(RisingStateName);
		private static readonly int LandingState = Animator.StringToHash(LandingStateName);
		private static readonly int TimeParam = Animator.StringToHash(TimeParamName);
		private static readonly int JumpParam = Animator.StringToHash(JumpParamName);

		// 現ステートの開始時刻
		private float startTime;

		// ステートへの遷移時
		public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			if (stateInfo.shortNameHash == RisingState)
			{
				OnRisingEnter(animator, stateInfo);
			}
			else if (stateInfo.shortNameHash == LandingState)
			{
				OnLandingEnter(animator, stateInfo);
			}

			ResetTime(animator);
		}

		// ステートの状態更新
		public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			ResetInvalidJumpTrigger(animator, layerIndex);

			UpdateTime(animator);
		}

		// Risingステートへの遷移時
		private void OnRisingEnter(Animator animator, AnimatorStateInfo stateInfo)
		{
			animator.SendMessage(TakeOffMessage, SendMessageOptions.DontRequireReceiver);
		}

		// Landingステートへの遷移時
		private void OnLandingEnter(Animator animator, AnimatorStateInfo stateInfo)
		{
			animator.SendMessage(LandingMessage, SendMessageOptions.DontRequireReceiver);
		}

		// ジャンプできない時にセットされたJumpトリガーをリセット
		private static void ResetInvalidJumpTrigger(Animator animator, int layerIndex)
		{
			if (!animator.IsInTransition(layerIndex))
			{
				animator.ResetTrigger(JumpParam);
			}
		}

		// ステートの経過時間をリセット
		private void ResetTime(Animator animator)
		{
			this.startTime = GetTime(animator);

			animator.SetFloat(TimeParam, 0.0f);
		}

		// ステートの経過時間を更新
		private void UpdateTime(Animator animator)
		{
			float elapsedTime = GetTime(animator) - this.startTime;

			animator.SetFloat(TimeParam, elapsedTime);
		}

		// アニメーターの更新方法に対応した時刻を取得
		private static float GetTime(Animator animator)
		{
			switch (animator.updateMode)
			{
				case AnimatorUpdateMode.Normal: return Time.time;
				case AnimatorUpdateMode.AnimatePhysics: return Time.fixedTime;
				case AnimatorUpdateMode.UnscaledTime: return Time.unscaledTime;
				default: return 0.0f;
			}
		}
	}
}
