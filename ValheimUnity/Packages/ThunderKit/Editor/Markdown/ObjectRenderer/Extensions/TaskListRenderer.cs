using Markdig.Extensions.TaskLists;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#else
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#endif

namespace ThunderKit.Markdown.ObjectRenderers
{
    using static Helpers.VisualElementFactory;
    using static Helpers.UnityPathUtility;
    public class TaskListRenderer : UIElementObjectRenderer<TaskList>
    {
        protected override void Write(UIElementRenderer renderer, TaskList taskList)
        {
            var checkbox = GetClassedElement<Toggle>("task-list");
            checkbox.value = taskList.Checked;
            checkbox.SetEnabled(false);
            renderer.WriteInline(checkbox);
        }
    }
}
