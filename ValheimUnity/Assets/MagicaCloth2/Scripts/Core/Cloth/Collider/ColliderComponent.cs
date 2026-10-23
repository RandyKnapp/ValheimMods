// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MagicaCloth2
{
    public abstract class ColliderComponent : ClothBehaviour, IDataValidate, ITransform
    {
        /// <summary>
        /// トランスフォームからの中心ローカルオフセット
        /// Center local offset from transform.
        /// </summary>
        public Vector3 center;

        /// <summary>
        /// Size
        /// Sphere(x:radius)
        /// Capsule(x:start radius, y:end radius, z:length)
        /// Box(x:size x, y:size y, z:size z)
        /// </summary>
        [SerializeField]
        protected Vector3 size;

        /// <summary>
        /// シンメトリーモード
        /// Symmetry mode.
        /// </summary>
        public ColliderSymmetryMode symmetryMode = ColliderSymmetryMode.None;

        /// <summary>
        /// シンメトリーの接続対象
        /// Symmetry connection target.
        /// </summary>
        public Transform symmetryTarget = null;

        //=========================================================================================
        /// <summary>
        /// Collider type.
        /// </summary>
        /// <returns></returns>
        public abstract ColliderManager.ColliderType GetColliderType();

        /// <summary>
        /// パラメータの検証
        /// </summary>
        public abstract void DataValidate();

        //=========================================================================================
        /// <summary>
        /// 登録チーム
        /// </summary>
        private HashSet<int> teamIdSet = new HashSet<int>();

        /// <summary>
        /// 現在登録中のシンメトリーモード
        /// </summary>
        public ColliderSymmetryMode? ActiveSymmetryMode { get; private set; } = null;

        /// <summary>
        /// 現在登録中のシンメトリーターゲット
        /// </summary>
        public Transform ActiveSymmetryTarget { get; private set; }

        //=========================================================================================
        /// <summary>
        /// Get collider size.
        /// 
        /// Sphere(x:radius)
        /// Capsule(x:start radius, y:end radius, z:length)
        /// Box(x:size x, y:size y, z:size z)
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual Vector3 GetSize() => size;

        public void SetSize(Vector3 size) => this.size = size;

        public void SetSizeX(float size) => this.size.x = size;
        public void SetSizeY(float size) => this.size.y = size;
        public void SetSizeZ(float size) => this.size.z = size;

        /// <summary>
        /// スケール値を取得
        /// </summary>
        /// <returns></returns>
        public virtual float GetScale()
        {
            // X軸のみを見る
            return transform.lossyScale.x;
        }

        /// <summary>
        /// 方向の逆転（基本的にカプセルコライダー用）
        /// </summary>
        /// <returns></returns>
        public virtual bool IsReverseDirection() => false;

        /// <summary>
        /// チームへのコライダー登録通知
        /// </summary>
        /// <param name="teamId"></param>
        internal void Register(int teamId)
        {
            teamIdSet.Add(teamId);
        }

        /// <summary>
        /// チームからのコライダー解除通知
        /// </summary>
        /// <param name="teamId"></param>
        /// <returns>利用者０ならtrue</returns>
        internal bool Exit(int teamId)
        {
            teamIdSet.Remove(teamId);
            return teamIdSet.Count == 0;
        }

        /// <summary>
        /// パラメータの反映
        /// すでに実行状態の場合はこの関数を呼び出さないとプロパティの変更が反映されません。
        /// Reflection of parameters.
        /// If it is already running, property changes will not be reflected unless this function is called.
        /// </summary>
        public void UpdateParameters()
        {
            // パラメータの検証
            DataValidate();

            // Symmetry更新
            // シンメトリーは削除もしくは追加がある。またTransformが変更される場合もある
            var oldActiveSymmetryMode = ActiveSymmetryMode;
            var oldActiveSymmetryTarget = ActiveSymmetryTarget;
            SetActiveSymmetryMode(firstOnly: false); // 最新の状態に更新
            bool changeSymmetry = oldActiveSymmetryMode != ActiveSymmetryMode || oldActiveSymmetryTarget != ActiveSymmetryTarget;

            // 反映
            foreach (int teamId in teamIdSet)
            {
                MagicaManager.Collider.UpdateParameters(this, teamId, changeSymmetry);
            }
        }

        /// <summary>
        /// 現在の状態から適切なシンメトリーモードとそのターゲットTransformを計算して返す
        /// </summary>
        /// <returns></returns>
        public ColliderSymmetryMode CalcSymmetryMode(out Transform symmetryParent)
        {
            symmetryParent = symmetryTarget;

            // 親
            var parent = transform.parent;
            if (parent == null)
                return ColliderSymmetryMode.None;

            switch (symmetryMode)
            {
                case ColliderSymmetryMode.None:
                    return ColliderSymmetryMode.None;
                case ColliderSymmetryMode.AutomaticHumanBody:
                case ColliderSymmetryMode.AutomaticTarget:
                    break;
                case ColliderSymmetryMode.X_Symmetry:
                case ColliderSymmetryMode.Y_Symmetry:
                case ColliderSymmetryMode.Z_Symmetry:
                case ColliderSymmetryMode.XYZ_Symmetry:
                    // ターゲットnullの場合は親
                    if (symmetryParent == null)
                        symmetryParent = parent;
                    return symmetryMode;
                default:
                    Develop.LogError("Unknown symmetry mode.");
                    return ColliderSymmetryMode.None;
            }

            // Automatic
            Animator ani = symmetryMode == ColliderSymmetryMode.AutomaticHumanBody ? gameObject.GetComponentInParent<Animator>(true) : null;

            // 対象Transform
            // AutomaticではSymmetryTargetは無視される
            var target = symmetryMode == ColliderSymmetryMode.AutomaticTarget ? symmetryTarget : null;
            if (target == null && ani)
            {
                // 自動判定
                GetHumanoidSymmetryBone(ref target, parent, ani, HumanBodyBones.Hips, HumanBodyBones.Hips);
                GetHumanoidSymmetryBone(ref target, parent, ani, HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg);
                GetHumanoidSymmetryBone(ref target, parent, ani, HumanBodyBones.RightUpperLeg, HumanBodyBones.LeftUpperLeg);

                GetHumanoidSymmetryBone(ref target, parent, ani, HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg);
                GetHumanoidSymmetryBone(ref target, parent, ani, HumanBodyBones.RightLowerLeg, HumanBodyBones.LeftLowerLeg);

                GetHumanoidSymmetryBone(ref target, parent, ani, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot);
                GetHumanoidSymmetryBone(ref target, parent, ani, HumanBodyBones.RightFoot, HumanBodyBones.LeftFoot);

                GetHumanoidSymmetryBone(ref target, parent, ani, HumanBodyBones.Spine, HumanBodyBones.Spine);
                GetHumanoidSymmetryBone(ref target, parent, ani, HumanBodyBones.Chest, HumanBodyBones.Chest);
                GetHumanoidSymmetryBone(ref target, parent, ani, HumanBodyBones.Neck, HumanBodyBones.Neck);
                GetHumanoidSymmetryBone(ref target, parent, ani, HumanBodyBones.Head, HumanBodyBones.Head);

                GetHumanoidSymmetryBone(ref target, parent, ani, HumanBodyBones.LeftShoulder, HumanBodyBones.RightShoulder);
                GetHumanoidSymmetryBone(ref target, parent, ani, HumanBodyBones.RightShoulder, HumanBodyBones.LeftShoulder);

                GetHumanoidSymmetryBone(ref target, parent, ani, HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm);
                GetHumanoidSymmetryBone(ref target, parent, ani, HumanBodyBones.RightUpperArm, HumanBodyBones.LeftUpperArm);

                GetHumanoidSymmetryBone(ref target, parent, ani, HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm);
                GetHumanoidSymmetryBone(ref target, parent, ani, HumanBodyBones.RightLowerArm, HumanBodyBones.LeftLowerArm);

                GetHumanoidSymmetryBone(ref target, parent, ani, HumanBodyBones.LeftHand, HumanBodyBones.RightHand);
                GetHumanoidSymmetryBone(ref target, parent, ani, HumanBodyBones.RightHand, HumanBodyBones.LeftHand);

                GetHumanoidSymmetryBone(ref target, parent, ani, HumanBodyBones.LeftToes, HumanBodyBones.RightToes);
                GetHumanoidSymmetryBone(ref target, parent, ani, HumanBodyBones.RightToes, HumanBodyBones.LeftToes);

                GetHumanoidSymmetryBone(ref target, parent, ani, HumanBodyBones.Jaw, HumanBodyBones.Jaw);
                GetHumanoidSymmetryBone(ref target, parent, ani, HumanBodyBones.UpperChest, HumanBodyBones.UpperChest);
            }
            if (target == null)
                target = parent;
            symmetryParent = target;

            // 親が同一かどうか
            bool sameParent = target == parent;

            // 各軸
            var x = parent.right;
            var y = parent.up;
            var z = parent.forward;
            var sx = target.right;
            var sy = target.up;
            var sz = target.forward;

            // ベクトルの方向性情報
            // Animatorがある場合はAnimatorから、ない場合は共通の親Transformから、それでも無い場合はワールドX軸
            Vector3 H = Vector3.right;
            if (ani)
                H = ani.transform.right;
            else
            {
                var commonParent = FindCommonParent(transform, symmetryParent);
                if (commonParent)
                {
                    //Debug.Log($"Find common parent:{commonParent.name}");
                    H = commonParent.right;
                }
            }

            float xdot = Mathf.Abs(Vector3.Dot(H, x));
            float ydot = Mathf.Abs(Vector3.Dot(H, y));
            float zdot = Mathf.Abs(Vector3.Dot(H, z));

            bool xsign = Vector3.Dot(x, sx) >= 0.0f;
            bool ysign = Vector3.Dot(y, sy) >= 0.0f;
            bool zsign = Vector3.Dot(z, sz) >= 0.0f;

            if (xdot > ydot && xdot > zdot)
            {
                // (X)
                if (sameParent)
                    return ColliderSymmetryMode.X_Symmetry;
                if (Vector3.Dot(H, x) * Vector3.Dot(H, sx) > 0.0f)
                {
                    if (ysign == false && zsign == false)
                        return ColliderSymmetryMode.XYZ_Symmetry;
                    else
                        return ColliderSymmetryMode.X_Symmetry;
                }
                else if (zsign)
                    return ColliderSymmetryMode.Y_Symmetry;
                else
                    return ColliderSymmetryMode.Z_Symmetry;
            }
            else if (ydot > xdot && ydot > zdot)
            {
                // (Y)
                if (sameParent)
                    return ColliderSymmetryMode.Y_Symmetry;
                if (Vector3.Dot(H, y) * Vector3.Dot(H, sy) > 0.0f)
                {
                    if (xsign == false && zsign == false)
                        return ColliderSymmetryMode.XYZ_Symmetry;
                    else
                        return ColliderSymmetryMode.Y_Symmetry;
                }
                else if (zsign)
                    return ColliderSymmetryMode.X_Symmetry;
                else
                    return ColliderSymmetryMode.Z_Symmetry;
            }
            else
            {
                // (Z)
                if (sameParent)
                    return ColliderSymmetryMode.Z_Symmetry;
                if (Vector3.Dot(H, z) * Vector3.Dot(H, sz) > 0.0f)
                {
                    if (xsign == false && ysign == false)
                        return ColliderSymmetryMode.XYZ_Symmetry;
                    else
                        return ColliderSymmetryMode.Z_Symmetry;
                }
                else if (xsign)
                    return ColliderSymmetryMode.Y_Symmetry;
                else
                    return ColliderSymmetryMode.X_Symmetry;
            }
        }

        bool GetHumanoidSymmetryBone(ref Transform target, Transform parent, Animator ani, HumanBodyBones src, HumanBodyBones dst)
        {
            var bone = ani.GetBoneTransform(src);
            if (bone && parent == bone)
            {
                var bone2 = ani.GetBoneTransform(dst);
                if (bone2)
                {
                    target = bone2;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// at/btの共通の親を返す。無い場合はnull。
        /// </summary>
        /// <param name="at"></param>
        /// <param name="bt"></param>
        /// <returns></returns>
        Transform FindCommonParent(Transform at, Transform bt)
        {
            if (at == null || bt == null)
                return null;

            // ハッシュセットでatの親を格納
            var atParents = new HashSet<Transform>(16);
            Transform current = at;
            while (current != null)
            {
                atParents.Add(current);
                current = current.parent;
            }

            // btの親をチェック
            current = bt;
            while (current != null)
            {
                if (atParents.Contains(current))
                    return current;
                current = current.parent;
            }

            return null;
        }

        /// <summary>
        /// 現在のシンメトリー設定に基づいて、シンメトリーのモードと対象を決定する
        /// </summary>
        internal void SetActiveSymmetryMode(bool firstOnly)
        {
            if (ActiveSymmetryMode.HasValue == false || firstOnly == false)
            {
                ActiveSymmetryMode = CalcSymmetryMode(out var target);
                ActiveSymmetryTarget = target;
            }
        }

        public int UseTeamCount => teamIdSet.Count;

        //=========================================================================================
        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
            if (symmetryTarget)
                transformSet.Add(symmetryTarget);
        }

        public void ReplaceTransform(Dictionary<MagicaObjectId, Transform> replaceDict)
        {
            if (symmetryTarget)
            {
                MagicaObjectId id = symmetryTarget.GetMagicaId();
                if (id.IsValid() && replaceDict.ContainsKey(id))
                    symmetryTarget = replaceDict[id];
            }
        }

        //=========================================================================================
        protected virtual void Start()
        {
            SetActiveSymmetryMode(firstOnly: true);
        }

        protected virtual void OnValidate()
        {
            UpdateParameters();
        }

        protected virtual void OnEnable()
        {
            // コライダーを有効にする
            foreach (int teamId in teamIdSet)
            {
                MagicaManager.Collider.EnableCollider(this, teamId, true);
            }
        }

        protected virtual void OnDisable()
        {
            // コライダーを無効にする
            foreach (int teamId in teamIdSet)
            {
                MagicaManager.Collider?.EnableCollider(this, teamId, false);
            }
        }

        protected virtual void OnDestroy()
        {
            // コライダーを削除する
            if (teamIdSet.Count > 0)
            {
                var teamList = teamIdSet.ToList();
                foreach (int teamId in teamList)
                {
                    MagicaManager.Collider?.RemoveCollider(this, teamId);
                }
                teamIdSet.Clear();
            }
        }
    }
}
