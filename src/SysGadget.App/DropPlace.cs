namespace SysGadget.App
{
    /// <summary>A possible drop in the settings tree: which node, onto which, above or below it.</summary>
    internal sealed class DropPlace
    {
        /// <summary>Creates a drop place.</summary>
        /// <param name="dragged">The node being dragged.</param>
        /// <param name="target">The node under the pointer.</param>
        /// <param name="after">True for below the target, false for above.</param>
        public DropPlace(SettingsTreeNode dragged, SettingsTreeNode target, bool after)
        {
            Dragged = dragged;
            Target = target;
            After = after;
        }

        /// <summary>The node being dragged.</summary>
        public SettingsTreeNode Dragged { get; }

        /// <summary>The node under the pointer.</summary>
        public SettingsTreeNode Target { get; }

        /// <summary>True for below the target, false for above.</summary>
        public bool After { get; }
    }
}
