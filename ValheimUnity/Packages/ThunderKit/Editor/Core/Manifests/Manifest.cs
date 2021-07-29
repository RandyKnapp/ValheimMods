using System;
using System.Collections.Generic;
using System.Linq;

using ThunderKit.Common;
using ThunderKit.Core.Manifests.Datum;
using UnityEditor;
using UnityEngine;
using static ThunderKit.Core.Editor.ScriptableHelper;

namespace ThunderKit.Core.Manifests
{
    public class Manifest : ComposableObject
    {
        [SerializeField, HideInInspector] private ManifestIdentity identity;

        [MenuItem(Constants.ThunderKitContextRoot + nameof(Manifest), priority = Constants.ThunderKitMenuPriority)]
        public static void Create()
        {
            SelectNewAsset(afterCreated: (Action<Manifest>)(manifest =>
            {
                manifest.Identity = CreateInstance<ManifestIdentity>();
                manifest.Identity.name = nameof(ManifestIdentity);
                manifest.InsertElement(manifest.Identity, 0);
            }));
        }

        public IEnumerable<Manifest> EnumerateManifests()
        {
            if (!Identity || Identity.Dependencies == null)
            {
                yield return this;
                yield break;
            }

            var returned = new HashSet<Manifest>(this.Identity.Dependencies.SelectMany(x => x.EnumerateManifests()));
            returned.Add(this);
            foreach (var ret in returned)
                yield return ret;
        }

        public ManifestIdentity Identity
        {
            get
            {
                if (identity == null)
                {
                    var path = AssetDatabase.GetAssetPath(this);
                    var reps = AssetDatabase.LoadAllAssetsAtPath(path);
                    identity = reps.OfType<ManifestIdentity>().FirstOrDefault();
                    EditorUtility.SetDirty(this);
                }

                return identity;
            }

            set => identity = value;
        }

        public override Type ElementType => typeof(ManifestDatum);

        public override string ElementTemplate => @"
using ThunderKit.Core.Manifests;

namespace {0}
{{
    public class {1} : ManifestDatum
    {{
    }}
}}
";
        public override bool SupportsType(Type type) => ElementType.IsAssignableFrom(type);
    }
}