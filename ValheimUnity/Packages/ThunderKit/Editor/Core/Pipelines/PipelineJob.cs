using UnityEngine;

namespace ThunderKit.Core.Pipelines
{
    public abstract class PipelineJob : ComposableElement
    {
        [HideInInspector]
        public bool Active = true;
        public abstract void Execute(Pipeline pipeline);
    }
}