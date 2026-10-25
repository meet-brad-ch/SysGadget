namespace SysGadget.Core
{
    /// <summary>Where the gadget sits on its monitor.</summary>
    public enum DockEdge
    {
        /// <summary>Docked to the left edge; reserves screen space.</summary>
        Left,

        /// <summary>Docked to the right edge; reserves screen space.</summary>
        Right,

        /// <summary>Docked to the top edge; reserves screen space.</summary>
        Top,

        /// <summary>Docked to the bottom edge; reserves screen space.</summary>
        Bottom,

        /// <summary>A free window that does not reserve screen space.</summary>
        Floating
    }
}
