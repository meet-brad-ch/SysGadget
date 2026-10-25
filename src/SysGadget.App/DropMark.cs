namespace SysGadget.App
{
    /// <summary>Where a dragged row would land relative to the row under the pointer.</summary>
    internal enum DropMark
    {
        /// <summary>The row is not a drop target.</summary>
        None,

        /// <summary>The dragged row would go above this one.</summary>
        Before,

        /// <summary>The dragged row would go below this one.</summary>
        After,
    }
}
