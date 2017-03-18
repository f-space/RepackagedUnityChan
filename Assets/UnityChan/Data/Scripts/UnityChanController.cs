using UnityEngine;

namespace UnityChan
{
	// ユニティちゃんの操作スクリプト
	[RequireComponent(typeof(Animator))]
	public class UnityChanController : MonoBehaviour
	{
		// 地面との接触状態を表す構造体
		private struct GroundCollision
		{
			// 接触しているかどうか
			public bool Hit;

			// 接点の法線
			public Vector3 Normal;
		}

		// 各入力の名前
		private const string HorizontalAxis = "Horizontal";
		private const string VerticalAxis = "Vertical";
		private const string JumpButton = "Jump";

		// 各アニメーターパラメータの名前
		private const string SpeedParamName = "Speed";
		private const string DirectionParamName = "Direction";
		private const string GroundedParamName = "Grounded";
		private const string JumpParamName = "Jump";

		[Tooltip("物理特性マテリアル。")]
		public PhysicMaterial Material;

		[Tooltip("カプセルの中心位置。")]
		public Vector3 Center = Vector3.zero;

		[Tooltip("カプセルの半径。")]
		public float Radius = 1.0f;

		[Tooltip("カプセルの高さ。")]
		public float Height = 2.0f;

		[Tooltip("質量。")]
		public float Mass = 50.0f;

		[Tooltip("抵抗係数。")]
		public float Drag = 0.25f;

		[Tooltip("前進速度。")]
		public float ForwardSpeed = 7.5f;

		[Tooltip("後退速度。")]
		public float BackwardSpeed = 2.5f;

		[Tooltip("移動の加減速力。")]
		public float MoveGrip = 5.0f;

		[Tooltip("回転速度。")]
		public float TurnSpeed = 45.0f;

		[Tooltip("回転の加減速力。")]
		public float TurnGrip = 10.0f;

		[Tooltip("ジャンプの初速。")]
		public float JumpSpeed = 5.0f;

		[Tooltip("着地可能な坂の最大傾斜。")]
		public float SlopeLimit = 60.0f;

		// アタッチされているアニメーター
		private Animator animator;

		// アタッチされているコライダー
		private CapsuleCollider colliderCache;

		// アタッチされている剛体
		private Rigidbody rigidbodyCache;

		// 軸入力
		private Vector2 axes;

		// 着地しているかどうか
		private bool grounded;

		// 地面の法線
		private Vector3 normal;

		// 地面との接触状態
		private GroundCollision groundCollision;

		// 各アニメーターパラメータのハッシュ(ID)
		private static readonly int SpeedParam = Animator.StringToHash(SpeedParamName);
		private static readonly int DirectionParam = Animator.StringToHash(DirectionParamName);
		private static readonly int GroundedParam = Animator.StringToHash(GroundedParamName);
		private static readonly int JumpParam = Animator.StringToHash(JumpParamName);

		// メンバーの初期化
		private void Awake()
		{
			this.animator = GetComponent<Animator>();
			this.colliderCache = SetUpCollider();
			this.rigidbodyCache = SetUpRigidbody();
		}

		// 有効化時
		private void OnEnable()
		{
			EnableRigidbody(rigidbodyCache);

			ResetMotion();
			ResetGroundCollision();
			ResetGround();
		}

		// 無効化時
		private void OnDisable()
		{
			DisableRigidbody(rigidbodyCache);
		}

		// 状態更新
		private void Update()
		{
			HandleInput();

			UpdateAnimatorParams();
		}

		// 物理演算の状態更新
		private void FixedUpdate()
		{
			UpdateGround();

			UpdateRotation();
			AddGravity();
			AddMovementForce();
			LimitUpwardVelocity();

			ResetGroundCollision();
		}

		// 衝突時
		private void OnCollisionStay(Collision collision)
		{
			DetectGroundCollision(collision);
		}

		// 踏み切り時
		private void OnTakeOff()
		{
			TakeOff();
		}

		// ジャンプ
		public void Jump()
		{
			if (grounded)
			{
				animator.SetTrigger(JumpParam);
			}
		}

		// コライダーの設定
		private CapsuleCollider SetUpCollider()
		{
			CapsuleCollider collider = gameObject.AddComponent<CapsuleCollider>();
			collider.isTrigger = false;
			collider.sharedMaterial = Material;
			collider.center = Center;
			collider.radius = Radius;
			collider.height = Height;
			collider.direction = 1;
			collider.hideFlags = HideFlags.HideInInspector;

			return collider;
		}

		// 剛体の設定
		private Rigidbody SetUpRigidbody()
		{
			Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
			rigidbody.mass = Mass;
			rigidbody.drag = Drag;
			rigidbody.angularDrag = 0.0f;
			rigidbody.useGravity = false;
			rigidbody.isKinematic = false;
			rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
			rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
			rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
			rigidbody.hideFlags = HideFlags.HideInInspector;

			return rigidbody;
		}

		// 踏み切る
		private void TakeOff()
		{
			rigidbodyCache.velocity = new Vector3(rigidbodyCache.velocity.x, JumpSpeed, rigidbodyCache.velocity.z);

			ResetGroundCollision();
			ResetGround();
		}

		// 入力の処理
		private void HandleInput()
		{
			// 軸の状態を記憶
			float horizontal = Input.GetAxis(HorizontalAxis);
			float vertical = Input.GetAxis(VerticalAxis);
			this.axes = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1.0f);

			// ジャンプボタンが押されていればジャンプ
			if (Input.GetButtonDown(JumpButton))
			{
				Jump();
			}
		}

		// アニメーターのパラメータを更新
		private void UpdateAnimatorParams()
		{
			float speed = Vector3.Dot(rigidbodyCache.velocity, transform.forward);
			float direction = axes.x;

			animator.SetFloat(SpeedParam, speed);
			animator.SetFloat(DirectionParam, direction);
			animator.SetBool(GroundedParam, grounded);
		}

		// 向きの更新
		private void UpdateRotation()
		{
			// 回転の強さを入力から決定
			float intention = Mathf.Clamp(axes.x / Mathf.Cos(Mathf.PI * 0.25f), -1.0f, 1.0f);
			float power = grounded ? intention : 0.0f;

			// 角加速度を計算
			float angularVelocity = rigidbodyCache.angularVelocity.y;
			float targetAngularVelocity = (TurnSpeed * Mathf.Deg2Rad) * power;
			float angularAcceleration = (targetAngularVelocity - angularVelocity) * TurnGrip;

			// 剛体に角加速度を加える
			// 剛体は回転制約(RigidbodyConstraints.FreezeRotation)により回転しないよう制限されているが、
			// これは回転そのものを止めているのではなく、慣性モーメント（回転のしづらさ）を無限大にしている
			// トルク(ForceMode.Force)ではなく角加速度(ForceMode.Acceleration)を与えれば慣性モーメントは無視できる
			rigidbodyCache.AddTorque(Vector3.up * angularAcceleration, ForceMode.Acceleration);
		}

		// 重力の加算
		private void AddGravity()
		{
			// 地面との接触時に重力を加えると坂でずり落ちるため、接触時は重力を適用しない
			// 現実的には足裏の摩擦で留まるが、コライダーに摩擦係数を設定すると足元以外の摩擦にも値が適用されてしまう
			// 複数のコライダーを使用すれば正しい挙動をさせることもできるが、複雑になるためここでは重力を切ることでごまかす
			if (!groundCollision.Hit)
			{
				rigidbodyCache.AddForce(Physics.gravity, ForceMode.Acceleration);
			}
		}

		// 移動に関する力の加算
		private void AddMovementForce()
		{
			// 地上でのみ移動可能
			if (grounded)
			{
				// 足元の傾斜に沿った前後方向および左右方向の算出
				Vector3 right = Vector3.Cross(normal, transform.forward);
				Vector3 forward = Vector3.Cross(right, normal);

				// 前後移動の強さを入力から決定
				float intention = Mathf.Clamp(axes.y / Mathf.Sin(Mathf.PI * 0.25f), -1.0f, 1.0f);

				// 前後方向の加速度を計算
				float forwardVelocity = Vector3.Dot(rigidbodyCache.velocity, forward);
				float forwardTargetVelocity = (intention > 0.0f ? ForwardSpeed : BackwardSpeed) * intention;
				float forwardAcceleration = (forwardTargetVelocity - forwardVelocity) * MoveGrip;

				// 左右方向の加速度を計算
				float rightVelocity = Vector3.Dot(rigidbodyCache.velocity, right);
				float rightTargetVelocity = 0.0f;
				float rightAcceleration = (rightTargetVelocity - rightVelocity) * MoveGrip;

				// 傾斜に沿った加速度を計算
				Vector3 acceleration = forward * forwardAcceleration + right * rightAcceleration;

				// 剛体に加速度を加える
				rigidbodyCache.AddForce(acceleration, ForceMode.Acceleration);
			}
		}

		// 上向きの速度を制限する
		private void LimitUpwardVelocity()
		{
			// 地上でのみ制限
			if (grounded)
			{
				Vector3 velocity = rigidbodyCache.velocity;

				rigidbodyCache.velocity = velocity - normal * Mathf.Max(Vector3.Dot(normal, velocity), 0.0f);
			}
		}

		// 位置と回転をリセット
		private void ResetMotion()
		{
			rigidbodyCache.position = transform.position;
			rigidbodyCache.velocity = Vector3.zero;
			rigidbodyCache.rotation = transform.rotation;
			rigidbodyCache.angularVelocity = Vector3.zero;
		}

		// 地面との接触状態をリセット
		private void ResetGroundCollision()
		{
			this.groundCollision.Hit = false;
			this.groundCollision.Normal = Vector3.zero;
		}

		// 地面との接触を検出
		private void DetectGroundCollision(Collision collision)
		{
			if (!this.groundCollision.Hit)
			{
				// 足元の着地可能な地面に向かって衝突している接触点が存在するか確認
				int count = 0;
				Vector3 normal = Vector3.zero;
				foreach (var contact in collision.contacts)
				{
					if (OnFoot(contact, colliderCache) && IsGround(contact.normal, SlopeLimit) && !IsLeaving(contact, colliderCache.radius))
					{
						count++;
						normal += contact.normal;
					}
				}

				// 存在していれば地面との接触として検出
				if (count != 0)
				{
					this.groundCollision.Hit = true;
					this.groundCollision.Normal = normal / count;
				}
			}
		}

		// 着地状態の更新
		private void UpdateGround()
		{
			if (groundCollision.Hit)
			{
				// 地面と接触しているのであれば着地
				SetGround(groundCollision.Normal);
			}
			else if (grounded)
			{
				// もともと着地していた場合には着地状態を継続するかどうかを判断
				// 山なりの地形を通過した場合、剛体は上向きの勢いを維持したまま空中へ射出してしまう
				// しかしこれは人間の挙動らしくないため、このような地形でも着地状態を維持する必要がある

				float maxDistance = colliderCache.radius;
				int layerMask = MakeLayerMask(gameObject.layer);

				RaycastHit hitInfo;
				if (SphereCastDownward(colliderCache, out hitInfo, maxDistance, layerMask) && IsGround(hitInfo.normal, SlopeLimit))
				{
					// 着地可能な地面がすぐ足元にあるのであれば着地状態を維持
					SetGround(hitInfo.normal);
				}
				else
				{
					// 崖のように地面がなくなっている場合には落下
					ResetGround();
				}
			}
		}

		// 着地状態をリセット
		private void ResetGround()
		{
			this.grounded = false;
			this.normal = Vector3.zero;
		}

		// 着地状態を設定
		private void SetGround(Vector3 normal)
		{
			this.grounded = true;
			this.normal = normal;
		}

		// 接触点がコライダーの足元であるかどうか
		private static bool OnFoot(ContactPoint contact, CapsuleCollider collider)
		{
			if (contact.thisCollider == collider)
			{
				Vector3 point = collider.transform.InverseTransformPoint(contact.point);
				Vector3 center = collider.center;
				float footLevel = collider.radius - collider.height * 0.5f;

				return (point.y - center.y < footLevel);
			}

			return false;
		}

		// 法線が着地可能な地面であるかどうか
		private static bool IsGround(Vector3 normal, float limit)
		{
			return (Vector3.Dot(normal, Vector3.up) > Mathf.Cos(limit * Mathf.Deg2Rad));
		}

		// 接触点から離れようとしているかどうか
		private static bool IsLeaving(ContactPoint contact, float threshold)
		{
			Rigidbody rigidbody1 = contact.thisCollider.attachedRigidbody;
			Rigidbody rigidbody2 = contact.otherCollider.attachedRigidbody;
			Vector3 velocity1 = (rigidbody1 ? rigidbody1.velocity : Vector3.zero);
			Vector3 velocity2 = (rigidbody2 ? rigidbody2.velocity : Vector3.zero);
			Vector3 relativeVelocity = velocity1 - velocity2;

			return (Vector3.Dot(relativeVelocity, contact.normal) > threshold);
		}

		// 下向きに球を飛ばして衝突を検出
		private static bool SphereCastDownward(CapsuleCollider collider, out RaycastHit hitInfo, float maxDistance, int layerMask)
		{
			float halfCylinderHeight = collider.height * 0.5f - collider.radius;
			Vector3 bottom = collider.center + Vector3.down * halfCylinderHeight;

			Vector3 origin = collider.transform.TransformPoint(bottom);
			float radius = collider.radius;
			Vector3 direction = collider.transform.TransformDirection(Vector3.down);

			return Physics.SphereCast(origin, radius, direction, out hitInfo, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
		}

		// 衝突レイヤーマスクの作成
		private static int MakeLayerMask(int layer)
		{
			const int LayerSize = 32;

			int layerMask = 0;
			for (int i = 0; i < LayerSize; i++)
			{
				int bit = Physics.GetIgnoreLayerCollision(layer, i) ? 0 : 1;

				layerMask |= (bit << i);
			}

			return layerMask;
		}

		// 剛体を有効化
		private static void EnableRigidbody(Rigidbody rigidbody)
		{
			rigidbody.isKinematic = false;
			rigidbody.detectCollisions = true;
		}

		// 剛体を無効化
		private static void DisableRigidbody(Rigidbody rigidbody)
		{
			rigidbody.isKinematic = true;
			rigidbody.detectCollisions = false;
		}

		// 角度を-πからπの範囲に制限
		private static float WrapAngle(float angle)
		{
			const float TwoPi = Mathf.PI * 2.0f;

			return (angle - TwoPi * Mathf.Round(angle / TwoPi));
		}

#if UNITY_EDITOR // エディタでのみ有効

		// エディタ編集時
		private void OnValidate()
		{
			this.Radius = Mathf.Max(this.Radius, 0.0f);
			this.Height = Mathf.Max(this.Height, this.Radius * 2.0f);
			this.Mass = Mathf.Max(this.Mass, 0.0f);
			this.Drag = Mathf.Max(this.Drag, 0.0f);
			this.ForwardSpeed = Mathf.Max(this.ForwardSpeed, 0.0f);
			this.BackwardSpeed = Mathf.Max(this.BackwardSpeed, 0.0f);
			this.MoveGrip = Mathf.Max(this.MoveGrip, 0.0f);
			this.TurnSpeed = Mathf.Clamp(this.TurnSpeed, -180.0f, 180.0f);
			this.TurnGrip = Mathf.Max(this.TurnGrip, 0.0f);
			this.JumpSpeed = Mathf.Max(this.JumpSpeed, 0.0f);
			this.SlopeLimit = Mathf.Clamp(this.SlopeLimit, 0.0f, 90.0f);
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

				Gizmos.color = Color.green;
				Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.TRS(Center, Quaternion.identity, Vector3.one);
				Gizmos.DrawWireSphere(upperCenter, Radius);
				Gizmos.DrawWireSphere(lowerCenter, Radius);
				Gizmos.DrawLine(upperCenter + left, lowerCenter + left);
				Gizmos.DrawLine(upperCenter + right, lowerCenter + right);
				Gizmos.DrawLine(upperCenter + forward, lowerCenter + forward);
				Gizmos.DrawLine(upperCenter + back, lowerCenter + back);
			}
		}

#endif

	}
}


