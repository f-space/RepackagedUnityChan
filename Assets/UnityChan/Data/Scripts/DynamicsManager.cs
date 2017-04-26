using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityChan
{
	// 揺れもの全体の設定や制御を行う管理スクリプト
	[DisallowMultipleComponent]
	public class DynamicsManager : MonoBehaviour
	{
		// レイヤーを表すパラメータを示す属性
		[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
		private class LayerAttribute : PropertyAttribute { }

		// 物理演算用のオブジェクトをまとめるシミュレーション空間を表すゲームオブジェクトの名前
		private const string SimulationSpaceNameFormat = "Dynamics ({0})";

		// Defaultレイヤーの番号
		private const int DefaultLayer = 0;

		// ユーザー定義レイヤーの開始番号
		private const int FirstUserLayer = 8;

		// レイヤーの数
		private const int LayerSize = 32;

		[Tooltip("外部から加える力。")]
		public Vector3 ExternalForce;

		[Tooltip("制約条件を満たせなかった場合の最大許容距離(m)。")]
		public float ProjectionDistance;

		[Tooltip("制約条件を満たせなかった場合の最大許容角度(deg)。")]
		public float ProjectionAngle;

		[Tooltip("アニメーターの動きを滑らかに変化させるのにかける時間(s)。")]
		public float SmoothTime;

		[Tooltip("揺れもののレイヤー。")]
		[Layer]
		public int Layer;

		[Tooltip("揺れもの用コライダーのレイヤー。")]
		[Layer]
		public int ColliderLayer;

		[Tooltip("揺れもの及びコライダーのレイヤーを自動的に構成するかどうか。")]
		public bool AutoLayerConfiguration;

		[Tooltip("物理演算用のオブジェクトを表示するかどうか。")]
		public bool ShowSimulationSpace;

		// シミュレーション空間
		private GameObject space;

		// メンバーの初期化
		private void Awake()
		{
			this.space = CreateSimulationSpace();

			// レイヤーの自動構成
			if (AutoLayerConfiguration)
			{
				bool succeeded = ConfigureLayer();
				if (!succeeded)
				{
					// レイヤーの自動構成に失敗した場合は破棄して終了
					Debug.LogError("Failed to configure layers.", gameObject);

					Destroy(this);
				}
			}
		}

		// 破棄時
		private void OnDestroy()
		{
			if (space) Destroy(space);
		}

		// 有効化時
		private void OnEnable()
		{
			if (space) space.SetActive(true);
		}

		// 無効化時
		private void OnDisable()
		{
			if (space) space.SetActive(false);
		}

		// 状態更新
		private void Update()
		{
			if (space)
			{
				SetVisibility(space, ShowSimulationSpace);
			}
		}

		// 物理演算用オブジェクトの生成
		internal GameObject CreateDynamicsObject(string name, Type[] components)
		{
			GameObject dynamicsObject = new GameObject(name, components);
			dynamicsObject.transform.SetParent(space.transform, false);
			dynamicsObject.hideFlags = HideFlags.NotEditable;

			return dynamicsObject;
		}

		// シミュレーション空間の生成
		private GameObject CreateSimulationSpace()
		{
			GameObject gameObject = new GameObject(String.Format(SimulationSpaceNameFormat, name));
			gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable;

			return gameObject;
		}

		// レイヤーの自動構成
		private bool ConfigureLayer()
		{
			// 使用されていないレイヤーを二つ用意
			int layer1 = FindBlankLayer(FirstUserLayer, LayerSize);
			if (layer1 != -1)
			{
				int layer2 = FindBlankLayer(layer1 + 1, LayerSize);
				if (layer2 != -1)
				{
					// 二つのレイヤー間でのみ衝突するように構成
					IgnoreLayerCollisionWithAll(layer1);
					IgnoreLayerCollisionWithAll(layer2);
					Physics.IgnoreLayerCollision(layer1, layer2, false);

					this.Layer = layer1;
					this.ColliderLayer = layer2;

					return true;
				}
			}

			return false;
		}

		// ヒエラルキー上での可視性の設定
		private static void SetVisibility(GameObject gameObject, bool visibility)
		{
			if (visibility)
			{
				gameObject.hideFlags &= ~HideFlags.HideInHierarchy;
			}
			else
			{
				gameObject.hideFlags |= HideFlags.HideInHierarchy;
			}
		}

		// 使用されていないレイヤーの探索
		private static int FindBlankLayer(int start, int end)
		{
			for (int i = start; i < end; i++)
			{
				string name = LayerMask.LayerToName(i);
				if (String.IsNullOrEmpty(name))
				{
					return i;
				}
			}

			return -1;
		}

		// すべてのレイヤーと衝突しないように設定
		private static void IgnoreLayerCollisionWithAll(int layer)
		{
			for (int i = 0; i < LayerSize; i++)
			{
				Physics.IgnoreLayerCollision(layer, i, true);
			}
		}

#if UNITY_EDITOR // エディタでのみ有効

		// リセット時
		private void Reset()
		{
			this.ExternalForce = Vector3.zero;
			this.ProjectionDistance = 0.01f;
			this.ProjectionAngle = 10.0f;
			this.SmoothTime = 0.0f;
			this.Layer = DefaultLayer;
			this.ColliderLayer = DefaultLayer;
			this.AutoLayerConfiguration = true;
			this.ShowSimulationSpace = false;
		}

		// エディタ編集時
		private void OnValidate()
		{
			this.ProjectionDistance = Mathf.Max(this.ProjectionDistance, 0.0f);
			this.ProjectionAngle = Mathf.Clamp(this.ProjectionAngle, 0.0f, 180.0f);
			this.SmoothTime = Mathf.Max(this.SmoothTime, 0.0f);
		}

		// レイヤーをポップアップで選択できるようにするエディタ拡張
		[CustomPropertyDrawer(typeof(LayerAttribute))]
		private class LayerPropertyDrawer : PropertyDrawer
		{
			public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
			{
				return EditorGUIUtility.singleLineHeight;
			}

			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				label = EditorGUI.BeginProperty(position, label, property);

				EditorGUI.BeginChangeCheck();
				int value = EditorGUI.LayerField(position, label, property.intValue);
				if (EditorGUI.EndChangeCheck())
				{
					property.intValue = value;
				}

				EditorGUI.EndProperty();
			}
		}

#endif
	}
}


