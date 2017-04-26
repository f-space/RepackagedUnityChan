using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityChan
{
	// 揺れもの専用のコライダを表すスクリプト
	public class DynamicsCollider : MonoBehaviour
	{
		// オイラー角による回転を表すパラメータを示す属性
		[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
		private class EulerAttribute : PropertyAttribute { }

		[Tooltip("カプセルの半径。")]
		public float Radius;

		[Tooltip("カプセルの高さ。")]
		public float Height;

		[Tooltip("カプセルの中心位置。")]
		public Vector3 Center;

		[Tooltip("カプセルの回転。")]
		[Euler]
		public Quaternion Rotation;

		[Tooltip("物理特性マテリアル。")]
		public PhysicMaterial Material;

		// 対応する揺れものマネージャー
		private DynamicsManager manager;

		// 物理演算用オブジェクトにアタッチされている剛体
		private Rigidbody dynamicsRigidbody;

		// 速度
		private Vector3 velocity;

		// 角速度
		private Quaternion angularVelocity;

		// 各物理演算コンポーネントのパラメータ更新が必要かどうか
		private bool dirty;

		// 物理演算用オブジェクトに必要なコンポーネント一覧
		private static readonly Type[] Components = { typeof(CapsuleCollider), typeof(Rigidbody) };

		// メンバーの初期化
		private void Awake()
		{
			this.manager = GetComponentInParent<DynamicsManager>();

			// 正しい構造でスクリプトがアタッチされていなければ破棄して終了
			if (ValidateStructure())
			{
				Destroy(this);
			}
		}

		// 有効化時
		private void OnEnable()
		{
			this.velocity = Vector3.zero;
			this.angularVelocity = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);

			Refresh();
		}

		// 状態の初期化
		private void Start()
		{
			GameObject dynamicsObject = CreateDynamicsObject();

			this.dynamicsRigidbody = dynamicsObject.GetComponent<Rigidbody>();
			this.dynamicsRigidbody.isKinematic = true;
			this.dynamicsRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

			UpdateLayer();
		}

		// 状態更新
		private void Update()
		{
			UpdateLayer();
		}

		// 物理演算の状態更新
		private void FixedUpdate()
		{
			RefreshParameters();

			UpdateRigidbody();
		}

		// 物理演算コンポーネントのパラメータ更新を要求
		public void Refresh()
		{
			this.dirty = true;
		}

		// 不正な構造の確認
		private bool ValidateStructure()
		{
			if (!manager)
			{
				Debug.LogErrorFormat(gameObject, "{0} not found.", typeof(DynamicsManager).Name);

				return true;
			}

			return false;
		}

		// 物理演算用オブジェクトの生成
		private GameObject CreateDynamicsObject()
		{
			GameObject dynamicsObject = manager.CreateDynamicsObject(name, Components);
			dynamicsObject.transform.position = transform.position;
			dynamicsObject.transform.rotation = transform.rotation * this.Rotation;

			return dynamicsObject;
		}

		// 物理演算コンポーネントのパラメータの更新
		private void RefreshParameters()
		{
			if (dirty)
			{
				SetColliderParameters();

				this.dirty = false;
			}
		}

		// コライダーのパラメータの更新
		private void SetColliderParameters()
		{
			CapsuleCollider collider = dynamicsRigidbody.GetComponent<CapsuleCollider>();

			collider.isTrigger = false;
			collider.sharedMaterial = this.Material;
			collider.center = Quaternion.Inverse(this.Rotation) * this.Center;
			collider.radius = this.Radius;
			collider.height = this.Height;
		}

		// 物理演算用オブジェクトのレイヤーの更新
		private void UpdateLayer()
		{
			dynamicsRigidbody.gameObject.layer = manager.ColliderLayer;
		}

		// 剛体の位置および回転の更新
		private void UpdateRigidbody()
		{
			Vector3 position = transform.position;
			Quaternion rotation = transform.rotation * this.Rotation;

			float smoothTime = manager.SmoothTime;
			if (!Mathf.Approximately(smoothTime, 0.0f))
			{
				position = Vector3.SmoothDamp(dynamicsRigidbody.position, position, ref velocity, smoothTime);
				rotation = SmoothDamp(dynamicsRigidbody.rotation, rotation, ref angularVelocity, smoothTime);
			}

			dynamicsRigidbody.MovePosition(position);
			dynamicsRigidbody.MoveRotation(rotation);
		}

		// クォータニオンの滑らかに変化させる
		private static Quaternion SmoothDamp(Quaternion current, Quaternion target, ref Quaternion velocity, float smoothTime)
		{
			float dot = Quaternion.Dot(current, target);
			float sign = Mathf.Sign(dot);

			Vector4 result;
			result.x = Mathf.SmoothDamp(current.x, target.x * sign, ref velocity.x, smoothTime);
			result.y = Mathf.SmoothDamp(current.y, target.y * sign, ref velocity.y, smoothTime);
			result.z = Mathf.SmoothDamp(current.z, target.z * sign, ref velocity.z, smoothTime);
			result.w = Mathf.SmoothDamp(current.w, target.w * sign, ref velocity.w, smoothTime);
			result.Normalize();

			return new Quaternion(result.x, result.y, result.z, result.w);
		}

#if UNITY_EDITOR // エディタでのみ有効

		// リセット時
		private void Reset()
		{
			this.Radius = 1.0f;
			this.Height = 2.0f;
			this.Center = Vector3.zero;
			this.Rotation = Quaternion.identity;
			this.Material = null;

			if (EditorApplication.isPlaying) Refresh();
		}

		// エディタ編集時
		private void OnValidate()
		{
			this.Radius = Mathf.Max(this.Radius, 0.0f);
			this.Height = Mathf.Max(this.Height, this.Radius * 2.0f);

			if (EditorApplication.isPlaying) Refresh();
		}

		// 選択時のギズモの描画
		private void OnDrawGizmosSelected()
		{
			if (enabled)
			{
				float halfLength = Height * 0.5f - Radius;
				Vector3 upperCenter = Vector3.up * halfLength;
				Vector3 lowerCenter = Vector3.down * halfLength;
				Vector3 left = Vector3.left * Radius;
				Vector3 right = Vector3.right * Radius;
				Vector3 forward = Vector3.forward * Radius;
				Vector3 back = Vector3.back * Radius;

				Gizmos.color = Color.red;
				Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.TRS(Center, Rotation, Vector3.one);
				Gizmos.DrawWireSphere(upperCenter, Radius);
				Gizmos.DrawWireSphere(lowerCenter, Radius);
				Gizmos.DrawLine(upperCenter + left, lowerCenter + left);
				Gizmos.DrawLine(upperCenter + right, lowerCenter + right);
				Gizmos.DrawLine(upperCenter + forward, lowerCenter + forward);
				Gizmos.DrawLine(upperCenter + back, lowerCenter + back);
			}
		}

		// クォータニオンをオイラー角で指定できるようにするエディタ拡張
		[CustomPropertyDrawer(typeof(EulerAttribute))]
		private class EulerPropertyDrawer : PropertyDrawer
		{
			public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
			{
				return EditorGUIUtility.singleLineHeight;
			}

			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				label = EditorGUI.BeginProperty(position, label, property);

				EditorGUI.BeginChangeCheck();
				Vector3 value = EditorGUI.Vector3Field(position, label, property.quaternionValue.eulerAngles);
				if (EditorGUI.EndChangeCheck())
				{
					property.quaternionValue = Quaternion.Euler(value);
				}

				EditorGUI.EndProperty();
			}
		}

#endif

	}
}


