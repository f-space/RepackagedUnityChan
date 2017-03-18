using System;
using UnityEngine;

namespace UnityChan
{
	// 揺れもののルート（根本）を表すスクリプト
	[DisallowMultipleComponent]
	public class DynamicsRoot : MonoBehaviour
	{
		// 対応する揺れものマネージャー
		private DynamicsManager manager;

		// 物理演算用オブジェクトにアタッチされている剛体
		private Rigidbody dynamicsRigidbody;

		// 速度
		private Vector3 velocity;

		// 角速度
		private Quaternion angularVelocity;

		// 物理演算用オブジェクトに必要なコンポーネント一覧
		private static readonly Type[] Components = { typeof(Rigidbody) };

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
		}

		// 状態の初期化
		private void Start()
		{
			GameObject dynamicsObject = CreateDynamicsObject();

			this.dynamicsRigidbody = dynamicsObject.GetComponent<Rigidbody>();
			this.dynamicsRigidbody.isKinematic = true;

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
			UpdateRigidbody();
		}

		// 剛体の取得
		internal Rigidbody GetDynamicsRigidbody()
		{
			return dynamicsRigidbody;
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
			Transform parentTransform = transform.parent;
			Vector3 position = parentTransform ? parentTransform.position : Vector3.zero;
			Quaternion rotation = parentTransform ? parentTransform.rotation : Quaternion.identity;

			GameObject dynamicsObject = manager.CreateDynamicsObject(name, Components);
			dynamicsObject.transform.position = position;
			dynamicsObject.transform.rotation = rotation;

			return dynamicsObject;
		}

		// 物理演算用オブジェクトのレイヤーの更新
		private void UpdateLayer()
		{
			dynamicsRigidbody.gameObject.layer = manager.Layer;
		}

		// 剛体の位置および回転の更新
		private void UpdateRigidbody()
		{
			Transform parentTransform = transform.parent;
			Vector3 position = parentTransform ? parentTransform.position : Vector3.zero;
			Quaternion rotation = parentTransform ? parentTransform.rotation : Quaternion.identity;

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
	}
}
