// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// カメラ回転
    /// </summary>
    public class CameraOrbit : MonoBehaviour
    {
        [SerializeField]
        private Transform cameraTransform;

        [Header("Camera Target")]
        public Transform cameraTarget;
        public Vector3 cameraTargetPos;
        public Vector3 cameraTargetOffset;

        [Header("Now Position")]
        [SerializeField]
        private float cameraDist = 1.5f;
        [SerializeField]
        private float cameraPitch = 21.0f;
        [SerializeField]
        private float cameraYaw = 180.0f;

        [Header("Parameter")]
        [SerializeField]
        private float cameraDistHokanTime = 0.1f;
        [SerializeField]
        private float cameraAngleHokanTime = 0.1f;

        [SerializeField]
        private float cameraDistSpeed = 0.02f;
        [SerializeField]
        private float cameraDistMax = 8.0f;
        [SerializeField]
        private float cameraDistMin = 0.1f;

        [SerializeField]
        private float cameraYawSpeed = 0.3f;
        [SerializeField]
        private float cameraPitchSpeed = 0.3f;
        [SerializeField]
        private float cameraMaxAngleSpeed = 100.0f;
        [SerializeField]
        private float cameraPitchMax = 89.0f;
        [SerializeField]
        private float cameraPitchMin = -89.0f;

        // 中ボタンドラッグによる移動
        public enum MoveMode
        {
            None,
            UpDown,
            Free,
        }
        [SerializeField]
        private MoveMode moveMode = MoveMode.Free;
        [SerializeField]
        private float moveSpeed = 0.002f;

        // 自動回転
        [Header("Auto Rotation")]
        [SerializeField]
        private bool useAutoRotation = false;
        [SerializeField]
        private float autoRotationSpeed = 90.0f;


        // 移動作業用
        private float setCameraDist;
        private float setCameraPitch;
        private float setCameraYaw;
        private float cameraDistVelocity;
        private float cameraPitchVelocity;
        private float cameraYawVelocity;
        private float offsetYaw;

        protected void Start()
        {
            if (cameraTransform == null)
            {
                var cam = GetComponent<Camera>();
                if (cam)
                    cameraTransform = cam.transform;
            }
            if (cameraTransform == null)
                enabled = false;

            setCameraDist = cameraDist;
            setCameraPitch = cameraPitch;
            setCameraYaw = cameraYaw;
        }

        protected void OnEnable()
        {
            // 入力イベント登録
            SimpleInputManager.OnTouchMove += OnTouchMove;
            SimpleInputManager.OnDoubleTouchMove += OnDoubleTouchMove;
            SimpleInputManager.OnTouchPinch += OnTouchPinch;
        }

        protected void OnDisable()
        {
            // 入力イベント解除
            SimpleInputManager.OnTouchMove -= OnTouchMove;
            SimpleInputManager.OnDoubleTouchMove -= OnDoubleTouchMove;
            SimpleInputManager.OnTouchPinch -= OnTouchPinch;
        }

        protected void LateUpdate()
        {
            // カメラ更新
            updateCamera();
        }

        // カメラ更新
        private void updateCamera()
        {
            if (cameraTransform == null)
                return;

            // カメラターゲットポジション
            cameraTargetPos = cameraTarget ? cameraTarget.position : transform.position;

            // 補間
            cameraDist = Mathf.SmoothDamp(cameraDist, setCameraDist, ref cameraDistVelocity, cameraDistHokanTime);
            cameraPitch = Mathf.SmoothDampAngle(cameraPitch, setCameraPitch, ref cameraPitchVelocity, cameraAngleHokanTime);
            cameraYaw = Mathf.SmoothDampAngle(cameraYaw, setCameraYaw, ref cameraYawVelocity, cameraAngleHokanTime);

            // 自動回転
            if (useAutoRotation)
            {
                offsetYaw += autoRotationSpeed * Time.deltaTime;
            }

            // 座標確定
            Quaternion q = Quaternion.Euler(cameraPitch, cameraYaw + offsetYaw, 0);
            q = transform.rotation * q; // コンポーネントの回転
            Vector3 v = new Vector3(0, 0, -cameraDist);
            Vector3 pos = q * v;

            // ターゲットポジション
            Vector3 tarpos = cameraTargetPos + transform.TransformVector(cameraTargetOffset);
            Vector3 fixpos = tarpos + pos;
            cameraTransform.position = fixpos;

            // 回転確定
            Vector3 relativePos = tarpos - cameraTransform.position;
            Quaternion rot = Quaternion.LookRotation(relativePos, transform.up);
            cameraTransform.rotation = rot;
        }

        // 回転操作
        private void updatePitchYaw(Vector2 speed)
        {
            // Yaw
            setCameraYaw += speed.x * cameraYawSpeed;

            // Pitch
            setCameraPitch += -speed.y * cameraPitchSpeed;
            setCameraPitch = Mathf.Clamp(setCameraPitch, cameraPitchMin, cameraPitchMax);
        }

        // 移動操作
        private void updateOffset(Vector2 speed)
        {
            if (cameraTransform == null)
            {
                return;
            }

            if (moveMode == MoveMode.UpDown)
            {
                cameraTargetOffset.y -= speed.y * moveSpeed;
            }
            else if (moveMode == MoveMode.Free)
            {
                Vector3 offset = moveSpeed * -speed.y * transform.InverseTransformDirection(cameraTransform.up);
                offset += moveSpeed * -speed.x * transform.InverseTransformDirection(cameraTransform.right);

                cameraTargetOffset += offset;
            }
        }

        // ズーム操作
        private void updateZoom(float speed)
        {
            float value = speed * cameraDistSpeed;
            float scl = Mathf.InverseLerp(cameraDistMin, cameraDistMax, setCameraDist);
            scl = Mathf.Clamp(scl, 0.1f, 1.0f);
            setCameraDist -= value * scl;
            setCameraDist = Mathf.Clamp(setCameraDist, cameraDistMin, cameraDistMax);
        }

        //=============================================================================================
        /// <summary>
        /// 入力通知：移動
        /// </summary>
        /// <param name="screenPos"></param>
        /// <param name="screenVelocity"></param>
        private void OnTouchMove(int fid, Vector2 screenPos, Vector2 screenVelocity, Vector2 cmVelocity)
        {
            screenVelocity *= SpeedAdjustment();

            if (fid == 2)
            {
                // 中ドラッグ
                updateOffset(screenVelocity);
            }
            else if (fid == 0)
            {
                // 左ドラッグ
                // 最大速度
                screenVelocity = Vector2.ClampMagnitude(screenVelocity, cameraMaxAngleSpeed);
                updatePitchYaw(screenVelocity);
            }
        }

        private void OnDoubleTouchMove(int fid, Vector2 screenPos, Vector2 screenVelocity, Vector2 cmVelocity)
        {
            screenVelocity *= SpeedAdjustment();

            if (SimpleInputManager.Instance.GetTouchCount() >= 3)
                updateOffset(screenVelocity);
        }

        /// <summary>
        /// 入力通知：ピンチイン／アウト
        /// </summary>
        /// <param name="speedscr"></param>
        /// <param name="speedcm"></param>
        private void OnTouchPinch(float speedscr, float speedcm)
        {
            speedcm *= SpeedAdjustment();

            if (SimpleInputManager.Instance.GetTouchCount() < 3)
                updateZoom(speedcm);
        }

        private float SpeedAdjustment()
        {
            return Time.deltaTime * 60.0f;
        }
    }
}
