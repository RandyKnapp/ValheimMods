// Magica Cloth 2.
// Copyright (c) 2025 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 初期化データ
    /// </summary>
    [System.Serializable]
    public class ClothInitSerializeData : ITransform
    {
        public const int InitDataVersion = 2;

        public int initVersion;
        public int localHash;
        public int globalHash;
        public ClothProcess.ClothType clothType;
        public TransformRecordSerializeData clothTransformRecord;
        public TransformRecordSerializeData normalAdjustmentTransformRecord;
        public List<TransformRecordSerializeData> customSkinningBoneRecords;
        public List<RenderSetupSerializeData> clothSetupDataList;

        public bool HasData()
        {
            if (initVersion == 0)
                return false;
            if (localHash == 0 || globalHash == 0)
                return false;

            return true;
        }

        public void Clear()
        {
            initVersion = 0;
            localHash = 0;
            globalHash = 0;
            clothType = ClothProcess.ClothType.MeshCloth;
            clothTransformRecord = new TransformRecordSerializeData();
            normalAdjustmentTransformRecord = new TransformRecordSerializeData();
            customSkinningBoneRecords = new List<TransformRecordSerializeData>();
            clothSetupDataList = new List<RenderSetupSerializeData>();
        }

        public ResultCode DataValidate(ClothProcess cprocess)
        {
            if (localHash == 0 || globalHash == 0)
                return new ResultCode(Define.Result.InitSerializeData_InvalidHash);
            if (initVersion == 0)
                return new ResultCode(Define.Result.InitSerializeData_InvalidVersion);

            if (clothSetupDataList == null || clothSetupDataList.Count == 0)
                return new ResultCode(Define.Result.InitSerializeData_InvalidSetupData);

            var cloth = cprocess.cloth;
            var sdata = cloth.SerializeData;

            if (clothType != sdata.clothType)
                return new ResultCode(Define.Result.InitSerializeData_ClothTypeMismatch);

            if (clothType == ClothProcess.ClothType.MeshCloth)
            {
                int rendererCount = sdata.sourceRenderers.Count;
                if (clothSetupDataList.Count != rendererCount)
                    return new ResultCode(Define.Result.InitSerializeData_SetupCountMismatch);

                // 各レンダラー情報の検証
                for (int i = 0; i < rendererCount; i++)
                {
                    if (clothSetupDataList[i].DataValidateMeshCloth(sdata.sourceRenderers[i]) == false)
                        return new ResultCode(Define.Result.InitSerializeData_MeshClothSetupValidationError);
                }
            }
            else if (clothType == ClothProcess.ClothType.BoneCloth)
            {
                if (clothSetupDataList.Count != 1)
                    return new ResultCode(Define.Result.InitSerializeData_SetupCountMismatch);

                if (clothSetupDataList[0].DataValidateBoneCloth(sdata, RenderSetupData.SetupType.BoneCloth) == false)
                    return new ResultCode(Define.Result.InitSerializeData_BoneClothSetupValidationError);
            }
            else if (clothType == ClothProcess.ClothType.BoneSpring)
            {
                if (clothSetupDataList.Count != 1)
                    return new ResultCode(Define.Result.InitSerializeData_SetupCountMismatch);

                if (clothSetupDataList[0].DataValidateBoneCloth(sdata, RenderSetupData.SetupType.BoneSpring) == false)
                    return new ResultCode(Define.Result.InitSerializeData_BoneSpringSetupValidationError);
            }

            // カスタムスキニングボーン
            if (sdata.customSkinningSetting.skinningBones.Count != customSkinningBoneRecords.Count)
                return new ResultCode(Define.Result.InitSerializeData_CustomSkinningBoneCountMismatch);

            // V1かつMeshClothかつSkinnedMeshRendererの場合のみ、(Clone)メッシュ利用時は無効とする
            // これは(Clone)メッシュを再度加工することによりボーンウエイトなどのデータがおかしくなりエラーが発生するため
            if (initVersion <= 1 && clothType == ClothProcess.ClothType.MeshCloth)
            {
                for (int i = 0; i < sdata.sourceRenderers.Count; i++)
                {
                    SkinnedMeshRenderer sren = sdata.sourceRenderers[i] as SkinnedMeshRenderer;
                    if (sren && sren.sharedMesh && sren.sharedMesh.name.Contains("(Clone)"))
                    {
                        return new ResultCode(Define.Result.InitSerializeData_InvalidCloneMesh);
                    }
                }
            }

            return ResultCode.Success;
        }

        public bool Serialize(
            ClothSerializeData sdata,
            TransformRecord clothTransformRecord,
            TransformRecord normalAdjustmentTransformRecord,
            List<RenderSetupData> setupList
            )
        {
            initVersion = InitDataVersion; // version

            clothType = sdata.clothType;

            this.clothTransformRecord = new TransformRecordSerializeData();
            this.clothTransformRecord.Serialize(clothTransformRecord);

            this.normalAdjustmentTransformRecord = new TransformRecordSerializeData();
            this.normalAdjustmentTransformRecord.Serialize(normalAdjustmentTransformRecord);

            // カスタムスキニングボーン
            customSkinningBoneRecords = new List<TransformRecordSerializeData>();
            int bcnt = sdata.customSkinningSetting.skinningBones.Count;
            for (int i = 0; i < bcnt; i++)
            {
                var tr = new TransformRecord(sdata.customSkinningSetting.skinningBones[i], read: true);
                var trs = new TransformRecordSerializeData();
                trs.Serialize(tr);
                customSkinningBoneRecords.Add(trs);
            }

            // setup data
            clothSetupDataList = new List<RenderSetupSerializeData>();
            if (setupList != null && setupList.Count > 0)
            {
                foreach (var setup in setupList)
                {
                    var meshSetupData = new RenderSetupSerializeData();
                    meshSetupData.Serialize(setup);
                    clothSetupDataList.Add(meshSetupData);
                }
            }

            localHash = GetLocalHash();
            globalHash = GetGlobalHash();

            return true;
        }


        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
            clothTransformRecord?.GetUsedTransform(transformSet);
            normalAdjustmentTransformRecord?.GetUsedTransform(transformSet);
            customSkinningBoneRecords?.ForEach(x => x.GetUsedTransform(transformSet));
            clothSetupDataList?.ForEach(x => x.GetUsedTransform(transformSet));
        }

        public void ReplaceTransform(Dictionary<MagicaObjectId, Transform> replaceDict)
        {
            clothTransformRecord?.ReplaceTransform(replaceDict);
            normalAdjustmentTransformRecord?.ReplaceTransform(replaceDict);
            customSkinningBoneRecords?.ForEach(x => x.ReplaceTransform(replaceDict));
            clothSetupDataList?.ForEach(x => x.ReplaceTransform(replaceDict));
        }

        int GetLocalHash()
        {
            // ローカルハッシュ
            // ・編集用メッシュが再構築されるたびにこのハッシュで保存チェックされる
            // ・各種カウント＋配列数
            // ・Transformの姿勢は無視する。ただし階層構造の変更は見る
            int hash = 0;
            hash += initVersion * 9876;
            hash += (int)clothType * 5656;
            hash += clothTransformRecord?.GetLocalHash() ?? 0;
            hash += normalAdjustmentTransformRecord?.GetLocalHash() ?? 0;
            customSkinningBoneRecords?.ForEach(x => hash += x?.GetLocalHash() ?? 0);
            clothSetupDataList?.ForEach(x => hash += x?.GetLocalHash() ?? 0);

            return hash;
        }

        int GetGlobalHash()
        {
            // グローバルハッシュ
            // ・頂点ペイント終了時にこのハッシュで保存チェックされる
            // ・保存チェックにはローカルハッシュも含まれる
            // ・Transformのローカル姿勢を見る(localPosition/localRotation/localScale)
            // ・ただしSetupDataのinitRenderScaleのみワールドスケールをチェックする
            int hash = 0;
            hash += clothTransformRecord?.GetGlobalHash() ?? 0;
            hash += normalAdjustmentTransformRecord?.GetGlobalHash() ?? 0;
            customSkinningBoneRecords?.ForEach(x => hash += x?.GetGlobalHash() ?? 0);
            clothSetupDataList?.ForEach(x => hash += x?.GetGlobalHash() ?? 0);

            return hash;
        }
    }
}
