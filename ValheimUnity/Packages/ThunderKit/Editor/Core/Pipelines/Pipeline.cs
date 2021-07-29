using System;
using System.Collections.Generic;
using System.Linq;
using ThunderKit.Common;
using ThunderKit.Core.Attributes;
using ThunderKit.Core.Editor;
using ThunderKit.Core.Manifests;
using UnityEditor;
using UnityEngine;

namespace ThunderKit.Core.Pipelines
{
    public class Pipeline : ComposableObject
    {
        [MenuItem(Constants.ThunderKitContextRoot + nameof(Pipeline), false, priority = Constants.ThunderKitMenuPriority)]
        public static void Create() => ScriptableHelper.SelectNewAsset<Pipeline>();

        public Manifest manifest;

        public Manifest[] Manifests { get; private set; }
        public IEnumerable<ManifestDatum> Datums => Manifests.SelectMany(manifest => manifest.Data.OfType<ManifestDatum>());

        public IEnumerable<PipelineJob> Jobs => Data.OfType<PipelineJob>();

        public string OutputRoot => "ThunderKit";

        public override string ElementTemplate =>
@"using ThunderKit.Core.Pipelines;

namespace {0}
{{
    [PipelineSupport(typeof(Pipeline))]
    public class {1} : PipelineJob
    {{
        public override void Execute(Pipeline pipeline)
        {{
        }}
    }}
}}
";

        private PipelineJob[] currentJobs;
        public int JobIndex { get; protected set; }
        public int ManifestIndex { get; set; }
        public Manifest Manifest => Manifests[ManifestIndex];

        public virtual void Execute()
        {
            Manifests = manifest.EnumerateManifests().Distinct().ToArray();
            currentJobs = Jobs.Where(SupportsType).ToArray();
            for (JobIndex = 0; JobIndex < currentJobs.Length; JobIndex++)
            {
                Job().Errored = false;
                Job().ErrorMessage = string.Empty;
            }
            for (JobIndex = 0; JobIndex < currentJobs.Length; JobIndex++)
                try
                {
                    if (!Job().Active) continue;
                    if (JobIsManifestProcessor())
                        ExecuteManifestLoop();
                    else
                        ExecuteJob();
                }
                catch (Exception e)
                {
                    Job().Errored = true;
                    Job().ErrorMessage = e.Message;
                    EditorGUIUtility.PingObject(Job());
                    Debug.LogError($"Error Invoking {Job().name} Job on Pipeline {name}\r\n{e}", this);
                    JobIndex = currentJobs.Length;
                    break;
                }

            JobIndex = -1;
        }

        PipelineJob Job() => currentJobs[JobIndex];

        void ExecuteJob() => Job().Execute(this);

        bool JobIsManifestProcessor() => Job().GetType().GetCustomAttributes(true).OfType<ManifestProcessorAttribute>().Any();

        bool CanProcessManifest(RequiresManifestDatumTypeAttribute attribute) => attribute.CanProcessManifest(Manifest);

        bool JobCanProcessManifest() => Job().GetType().GetCustomAttributes(true).OfType<RequiresManifestDatumTypeAttribute>().All(CanProcessManifest);

        void ExecuteManifestLoop()
        {
            for (ManifestIndex = 0; ManifestIndex < Manifests.Length; ManifestIndex++)
                if (Manifest && JobCanProcessManifest())
                    ExecuteJob();

            ManifestIndex = -1;
        }
        public bool SupportsType(PipelineJob job) => SupportsType(job.GetType());
        public override bool SupportsType(Type type)
        {
            if (ElementType.IsAssignableFrom(type))
            {
                var customAttributes = type.GetCustomAttributes(true);
                var pipelineSupportAttributes = customAttributes.OfType<PipelineSupportAttribute>();
                if (pipelineSupportAttributes.Any(psa => psa.HandlesPipeline(GetType())))
                    return true;
            }
            return false;
        }

        public override Type ElementType => typeof(PipelineJob);
    }
}