using UnityEngine;

namespace UnityChan
{
	// 瞬き時に動く部位であることを示すスクリプト
	[RequireComponent(typeof(SkinnedMeshRenderer))]
	public class BlinkablePart : MonoBehaviour
	{
		[Tooltip("瞬きに対応するブレンドシェイプの番号。")]
		public int BlendShapeIndex;

		[Tooltip("ブレンドシェイプ比率を乗算するかどうか。")]
		public bool MultiplyWeight;

		[Tooltip("排他的な関係のブレンドシェイプの番号。")]
		public int[] ExclusiveBlendShapes;

		[Tooltip("排他的な関係のブレンドシェイプを許容する閾値。")]
		public float ExclusionThreshold;

		// アタッチされたSkinnedMeshRenderer
		private SkinnedMeshRenderer meshRenderer;

		// メンバーの初期化
		private void Awake()
		{
			this.meshRenderer = GetComponent<SkinnedMeshRenderer>();
		}

		// ブレンドシェイプ比率（目の開き具合）を設定する
		public void SetWeight(float weight)
		{
			if (enabled && CanBlink())
			{
				int index = BlendShapeIndex;
				if (index >= 0 && index < meshRenderer.sharedMesh.blendShapeCount)
				{
					if (MultiplyWeight)
					{
						weight = BlendWithCurrentWeight(weight, index);
					}

					meshRenderer.SetBlendShapeWeight(index, weight);
				}
			}
		}

		// 瞬きが可能かどうか確認する
		public bool CanBlink()
		{
			return !IsExcluded();
		}

		// 排他的な関係のブレンドシェイプが許容値を超えて適用されているかどうか
		private bool IsExcluded()
		{
			float totalWeight = 0.0f;

			int count = meshRenderer.sharedMesh.blendShapeCount;
			foreach (int index in ExclusiveBlendShapes)
			{
				if (index >= 0 && index < count)
				{
					totalWeight += meshRenderer.GetBlendShapeWeight(index);
				}
			}

			return (totalWeight > ExclusionThreshold);
		}

		// 現在のブレンド比率とかけ合わせる
		private float BlendWithCurrentWeight(float weight, int index)
		{
			float current = meshRenderer.GetBlendShapeWeight(index);
			float w0 = 1.0f - current * 0.01f;
			float w1 = 1.0f - weight * 0.01f;

			return (1.0f - w0 * w1) * 100.0f;
		}

#if UNITY_EDITOR // エディタでのみ有効

		// エディタ編集時
		private void OnValidate()
		{
			this.BlendShapeIndex = Mathf.Max(this.BlendShapeIndex, 0);
			this.ExclusionThreshold = Mathf.Max(this.ExclusionThreshold, 0.0f);

			for (int i = 0; i < this.ExclusiveBlendShapes.Length; i++)
			{
				this.ExclusiveBlendShapes[i] = Mathf.Max(this.ExclusiveBlendShapes[i], 0);
			}
		}

#endif

	}
}
