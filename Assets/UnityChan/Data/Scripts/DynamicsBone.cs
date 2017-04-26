using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityChan
{
	// 揺れものの節およびボーンを表すスクリプト
	[DisallowMultipleComponent]
	public class DynamicsBone : MonoBehaviour
	{
		// バネ-ダンパ系のパラメータを保持する構造体
		[Serializable]
		public struct SpringParameter
		{
			[Tooltip("固有振動数。")]
			[Range(0.0f, 10.0f)]
			public float Frequency;

			[Tooltip("減衰比。")]
			[Range(0.0f, 1.0f)]
			public float DampingRatio;
		}

		[Tooltip("球の半径。")]
		public float Radius;

		[Tooltip("質量。")]
		public float Mass;

		[Tooltip("抵抗係数。")]
		public float Drag;

		[Tooltip("回転抵抗係数。")]
		public float AngularDrag;

		[Tooltip("重力の影響度。")]
		public float GravityScale;

		[Tooltip("座標軸の回転角度。")]
		public float AxesAngle;

		[Tooltip("主回転軸（赤）のバネパラメータ。")]
		public SpringParameter MainSpring;

		[Tooltip("副回転軸（緑）のバネパラメータ。")]
		public SpringParameter SubSpring;

		[Tooltip("主回転軸（赤）の最小回転角度(deg)。")]
		public float MainLowAngularLimit;

		[Tooltip("主回転軸（赤）の最大回転角度(deg)。")]
		public float MainHighAngularLimit;

		[Tooltip("副回転軸（緑）の最小・最大回転角度(deg)。")]
		public float SubAngularLimit;

		[Tooltip("回転制限に対する反発係数。")]
		[Range(0.0f, 1.0f)]
		public float LimitBounciness;

		[Tooltip("衝突検出をするかどうか。")]
		public bool DetectCollisions;

		[Tooltip("衝突検出モード。")]
		public CollisionDetectionMode CollisionDetectionMode;

		[Tooltip("物理特性マテリアル。")]
		public PhysicMaterial Material;

		// デフォルトの回転軸の向き
		[HideInInspector]
		public Quaternion DefaultAxesRotation;

		// 対応する揺れものマネージャー
		private DynamicsManager manager;

		// 対応する揺れものルート
		private DynamicsRoot root;

		// 親ボーン
		private DynamicsBone parent;

		// 物理演算用オブジェクトにアタッチされている剛体
		private Rigidbody dynamicsRigidbody;

		// コライダーの中心位置
		private Vector3 center;

		// バネの力が働かない自然な状態のローカル回転
		private Quaternion naturalRotation;

		// 各物理演算コンポーネントのパラメータ更新が必要かどうか
		private bool dirty;

		// 物理演算用オブジェクトに必要なコンポーネント一覧
		private static readonly Type[] Components = { typeof(SphereCollider), typeof(Rigidbody), typeof(ConfigurableJoint) };

		// メンバーの初期化
		private void Awake()
		{
			Transform parentTransform = transform.parent;
			if (parentTransform)
			{
				this.manager = parentTransform.GetComponentInParent<DynamicsManager>();
				this.root = parentTransform.GetComponentInParent<DynamicsRoot>();
				this.parent = parentTransform.GetComponent<DynamicsBone>();
				this.center = transform.localPosition;
				this.naturalRotation = parentTransform.localRotation;
			}

			// 正しい構造でスクリプトがアタッチされていなければ破棄して終了
			if (ValidateStructure())
			{
				Destroy(this);
			}
		}

		// 有効化時
		private void OnEnable()
		{
			Refresh();
		}

		// 状態の初期化
		private void Start()
		{
			this.dynamicsRigidbody = CreateDynamicsObject().GetComponent<Rigidbody>();

			UpdateLayer();
		}

		// 状態更新
		private void Update()
		{
			UpdateLayer();
		}

		// 遅めの状態更新
		private void LateUpdate()
		{
			TransferTransform();
		}

		// 物理演算の状態更新
		private void FixedUpdate()
		{
			RefreshParameters();

			AddGravity();
			AddExternalForce();
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

			if (!root)
			{
				Debug.LogErrorFormat(gameObject, "{0} not found.", typeof(DynamicsRoot).Name);

				return true;
			}

			if (!parent && root.transform != transform.parent)
			{
				Debug.LogErrorFormat(gameObject, "Not connected to a parent or a root.");

				return true;
			}

			return false;
		}

		// 物理演算用オブジェクトの生成
		private GameObject CreateDynamicsObject()
		{
			GameObject dynamicsObject = manager.CreateDynamicsObject(name, Components);
			dynamicsObject.transform.position = transform.parent.position;
			dynamicsObject.transform.rotation = transform.parent.rotation;

			return dynamicsObject;
		}

		// 親ボーンの剛体の取得
		private Rigidbody GetParentRigidbody()
		{
			return (parent ? parent.dynamicsRigidbody : root.GetDynamicsRigidbody());
		}

		// 回転軸の向きの取得
		private Quaternion GetAxesRotation()
		{
			return (DefaultAxesRotation * Quaternion.AngleAxis(AxesAngle, Vector3.forward));
		}

		// 物理演算コンポーネントのパラメータの更新
		private void RefreshParameters()
		{
			if (dirty)
			{
				if (parent) parent.RefreshParameters();

				ResetRotation();
				SetColliderParameters();
				SetRigidbodyParameters();
				SetJointParameters();

				this.dirty = false;
			}
		}

		// 自然な回転へとリセット
		private void ResetRotation()
		{
			Rigidbody parentRigidbody = GetParentRigidbody();

			dynamicsRigidbody.transform.rotation = parentRigidbody.transform.rotation * naturalRotation;
		}

		// コライダーのパラメータの更新
		private void SetColliderParameters()
		{
			SphereCollider collider = dynamicsRigidbody.GetComponent<SphereCollider>();

			collider.isTrigger = false;
			collider.sharedMaterial = this.Material;
			collider.center = this.center;
			collider.radius = this.Radius;
		}

		// 剛体のパラメータの更新
		private void SetRigidbodyParameters()
		{
			Rigidbody rigidbody = dynamicsRigidbody;

			rigidbody.mass = this.Mass;
			rigidbody.drag = this.Drag;
			rigidbody.angularDrag = this.AngularDrag;
			rigidbody.useGravity = false;
			rigidbody.isKinematic = false;
			rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
			rigidbody.collisionDetectionMode = this.CollisionDetectionMode;
			rigidbody.constraints = RigidbodyConstraints.None;
			rigidbody.detectCollisions = this.DetectCollisions;
		}

		// ジョイントのパラメータの更新
		private void SetJointParameters()
		{
			ConfigurableJoint joint = dynamicsRigidbody.GetComponent<ConfigurableJoint>();

			Quaternion axesRotation = GetAxesRotation();
			Vector3 primaryAxis = axesRotation * Vector3.right;
			Vector3 secondaryAxis = axesRotation * Vector3.up;

			joint.connectedBody = GetParentRigidbody();
			joint.anchor = Vector3.zero;
			joint.axis = primaryAxis;
			joint.secondaryAxis = secondaryAxis;
			joint.xMotion = ConfigurableJointMotion.Locked;
			joint.yMotion = ConfigurableJointMotion.Locked;
			joint.zMotion = ConfigurableJointMotion.Locked;
			joint.angularXMotion = ConfigurableJointMotion.Limited;
			joint.angularYMotion = ConfigurableJointMotion.Limited;
			joint.angularZMotion = ConfigurableJointMotion.Locked;
			joint.lowAngularXLimit = NewJointLimit(-this.MainHighAngularLimit);
			joint.highAngularXLimit = NewJointLimit(-this.MainLowAngularLimit);
			joint.angularYLimit = NewJointLimit(this.SubAngularLimit);
			joint.targetRotation = Quaternion.identity;
			joint.targetAngularVelocity = Vector3.zero;
			joint.rotationDriveMode = RotationDriveMode.XYAndZ;
			joint.angularXDrive = NewJointDrive(this.MainSpring, primaryAxis);
			joint.angularYZDrive = NewJointDrive(this.SubSpring, secondaryAxis);
			joint.projectionMode = JointProjectionMode.PositionAndRotation;
			joint.projectionDistance = manager.ProjectionDistance;
			joint.projectionAngle = manager.ProjectionAngle;
		}

		// ジョイントの回転制限の生成
		private SoftJointLimit NewJointLimit(float limit)
		{
			SoftJointLimit value = new SoftJointLimit();
			value.limit = limit;
			value.bounciness = LimitBounciness;
			value.contactDistance = 0.0f;

			return value;
		}

		// ジョイントのバネとダンパによる回転動力の生成
		private JointDrive NewJointDrive(SpringParameter parameters, Vector3 axis)
		{
			float angularFrequency = parameters.Frequency * (Mathf.PI * 2.0f);
			float dampingRatio = parameters.DampingRatio;
			float inertia = CalculateMomentOfInertia(axis);

			JointDrive value = new JointDrive();
			value.positionSpring = angularFrequency * angularFrequency * inertia;
			value.positionDamper = dampingRatio * angularFrequency * inertia * 2.0f;
			value.maximumForce = Single.PositiveInfinity;

			return value;
		}

		// 軸周りの慣性モーメントの計算
		private float CalculateMomentOfInertia(Vector3 axis)
		{
			float mass = dynamicsRigidbody.mass;
			Vector3 center = dynamicsRigidbody.centerOfMass;
			Vector3 tensor = dynamicsRigidbody.inertiaTensor;
			Quaternion rotation = dynamicsRigidbody.inertiaTensorRotation;

			return (Vector3.Dot(rotation * axis, tensor) + mass * center.sqrMagnitude);
		}

		// 物理演算用オブジェクトのレイヤーの更新
		private void UpdateLayer()
		{
			dynamicsRigidbody.gameObject.layer = manager.Layer;
		}

		// 重力の加算
		private void AddGravity()
		{
			dynamicsRigidbody.AddForce(Physics.gravity * this.GravityScale, ForceMode.Acceleration);
		}

		// 外力の加算
		private void AddExternalForce()
		{
			dynamicsRigidbody.AddForce(manager.ExternalForce, ForceMode.Force);
		}

		// 物理演算用オブジェクトの回転値をボーンの回転値へ転写
		private void TransferTransform()
		{
			Rigidbody parentRigidbody = (parent ? parent.dynamicsRigidbody : root.GetDynamicsRigidbody());
			Quaternion selfRotation = dynamicsRigidbody.transform.rotation;
			Quaternion parentRotation = parentRigidbody.transform.rotation;

			transform.parent.localRotation = Quaternion.Inverse(parentRotation) * selfRotation;
		}

#if UNITY_EDITOR // エディタでのみ有効

		// リセット時
		private void Reset()
		{
			this.Radius = 1.0f;
			this.Mass = 1.0f;
			this.Drag = 0.0f;
			this.AngularDrag = 0.05f;
			this.GravityScale = 1.0f;
			this.AxesAngle = 0.0f;
			this.MainSpring.Frequency = 0.0f;
			this.MainSpring.DampingRatio = 0.0f;
			this.SubSpring.Frequency = 0.0f;
			this.SubSpring.DampingRatio = 0.0f;
			this.MainLowAngularLimit = -180.0f;
			this.MainHighAngularLimit = +180.0f;
			this.SubAngularLimit = 180.0f;
			this.LimitBounciness = 0.0f;
			this.Material = null;
			this.DetectCollisions = true;
			this.CollisionDetectionMode = CollisionDetectionMode.Discrete;

			Invalidate();
		}

		// エディタ編集時
		private void OnValidate()
		{
			this.Radius = Mathf.Max(this.Radius, 0.0f);
			this.Mass = Mathf.Max(this.Mass, 0.0f);
			this.Drag = Mathf.Max(this.Drag, 0.0f);
			this.AngularDrag = Mathf.Max(this.AngularDrag, 0.0f);
			this.AxesAngle = Mathf.Clamp(this.AxesAngle, -180.0f, 180.0f);
			this.MainLowAngularLimit = Mathf.Clamp(this.MainLowAngularLimit, -180.0f, 180.0f);
			this.MainHighAngularLimit = Mathf.Clamp(this.MainHighAngularLimit, -180.0f, 180.0f);
			this.SubAngularLimit = Mathf.Clamp(this.SubAngularLimit, 0.0f, 180.0f);

			Invalidate();
		}

		// 選択時のギズモの描画
		private void OnDrawGizmosSelected()
		{
			if (enabled)
			{
				if (transform.parent)
				{
					Vector3 p0 = transform.position;
					Vector3 p1 = transform.parent.position;

					Quaternion rotation = transform.parent.rotation * GetAxesRotation();
					Vector3 right = rotation * Vector3.right;
					Vector3 up = rotation * Vector3.up;
					Vector3 forward = rotation * Vector3.forward;

					Gizmos.color = Color.Lerp(Color.yellow, Color.red, 0.5f);
					Gizmos.DrawLine(p0, p1);
					Gizmos.color = Color.yellow;
					Gizmos.DrawWireSphere(p0, Radius);
					Gizmos.color = Color.red;
					Gizmos.DrawLine(p0, p0 + right * Radius);
					Gizmos.color = Color.green;
					Gizmos.DrawLine(p0, p0 + up * Radius);
					Gizmos.color = Color.blue;
					Gizmos.DrawLine(p0, p0 + forward * Radius);
				}
			}
		}

		// 前処理するデータの更新
		[ContextMenu("Refresh")]
		private void RefreshPreprocessedData()
		{
			Undo.RecordObject(this, String.Format("Refresh {0}", typeof(DynamicsBone).Name));

			CalculateDefaultAxesRotation();
		}

		// データの更新を通知
		private void Invalidate()
		{
			if (EditorApplication.isPlaying)
			{
				Refresh();
			}
			else
			{
				CalculateDefaultAxesRotation();
			}
		}

		// デフォルトの回転軸の向きを計算
		private void CalculateDefaultAxesRotation()
		{
			if (transform.parent)
			{
				Vector3 p0 = transform.position;
				Vector3 p1 = transform.parent.position;
				Vector3 p2 = FindThirdPoint(transform.parent.parent, p0, p1);

				Vector3 delta0 = p0 - p1;
				Vector3 delta1 = p1 - p2;
				Vector3 forward = (delta0 != Vector3.zero ? delta0.normalized : Vector3.forward);
				Vector3 up = (delta1 != Vector3.zero ? delta1.normalized : Vector3.up);

				Quaternion toLocal = Quaternion.Inverse(transform.parent.rotation);
				Quaternion towardChild = Quaternion.LookRotation(toLocal * forward, toLocal * up);

				this.DefaultAxesRotation = towardChild;
			}
		}

		// 三角形を構成するための三つ目の頂点を構造を遡って検索
		private static Vector3 FindThirdPoint(Transform current, Vector3 p0, Vector3 p1)
		{
			const float Threshold = 5.0f * Mathf.Deg2Rad;

			if (current)
			{
				Vector3 p2 = current.position;
				if (!current.GetComponent<DynamicsManager>())
				{
					Vector3 d0 = Vector3.Normalize(p0 - p1);
					Vector3 d1 = Vector3.Normalize(p1 - p2);
					float angle = Mathf.Acos(Vector3.Dot(d0, d1));

					if (angle < Threshold) return FindThirdPoint(current.parent, p0, p1);
				}

				return p2;
			}

			return Vector3.zero;
		}

#endif

	}
}



