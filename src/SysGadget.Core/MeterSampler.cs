using System;
using System.Collections.Generic;
using System.Linq;

namespace SysGadget.Core
{
    /// <summary>
    /// Samples what the settings make visible, driven by <see cref="MeterCatalog"/> and
    /// <see cref="MeterRegistry"/>. A source is opened when the first visible item needs it and
    /// released when none does, so hidden items cost nothing. Each source is read once per sample.
    /// </summary>
    public sealed class MeterSampler : IDisposable
    {
        private readonly MeterRegistry registry;
        private readonly Dictionary<string, SampleSource> open = new Dictionary<string, SampleSource>();

        /// <summary>Creates a sampler.</summary>
        /// <param name="registry">The behaviour behind the catalogue ids.</param>
        public MeterSampler(MeterRegistry registry)
        {
            this.registry = registry;
        }

        /// <summary>Samples every visible group.</summary>
        /// <param name="settings">The current settings.</param>
        /// <param name="context">The clock values.</param>
        /// <returns>One section per visible group that has a visible item, in display order.</returns>
        public IReadOnlyList<GroupSection> Sample(GadgetSettings settings, SampleContext context)
        {
            List<GroupSetting> groups = settings.Groups.Where(g => g.Visible && g.Items.Any(i => i.Visible)).ToList();
            HashSet<string> needed = new HashSet<string>(
                groups.SelectMany(g => ShownItems(g, settings))
                    .Select(i => MeterCatalog.GetItem(i.Id).SourceId)
                    .Where(sourceId => registry.UnavailableReason(sourceId) == null));

            Maintain(needed);
            Dictionary<string, object> snapshots = needed.ToDictionary(id => id, id => open[id].Read(context));
            return groups.Select(g => BuildSection(g, settings, snapshots)).ToList();
        }

        /// <summary>Releases every open source.</summary>
        public void Dispose()
        {
            foreach (SampleSource source in open.Values)
            {
                source.Dispose();
            }

            open.Clear();
        }

        private void Maintain(HashSet<string> needed)
        {
            foreach (string sourceId in open.Keys.Where(id => !needed.Contains(id)).ToList())
            {
                open[sourceId].Dispose();
                open.Remove(sourceId);
            }

            foreach (string sourceId in needed.Where(id => !open.ContainsKey(id)))
            {
                open[sourceId] = registry.Open(sourceId);
            }
        }

        private GroupSection BuildSection(GroupSetting group, GadgetSettings settings, Dictionary<string, object> snapshots)
        {
            List<MeterRow> rows = new List<MeterRow>();
            foreach (ItemSetting item in ShownItems(group, settings))
            {
                IReadOnlyList<MeterRow> itemRows = ItemRows(item.Id, settings, snapshots);
                if (item.Instance.Length > 0)
                {
                    // one entry per disk: only that disk's rows (a disk that is gone shows nothing)
                    itemRows = itemRows.Where(r => r.Instance == item.Instance).ToList();
                }

                rows.AddRange(settings.ThinBar ? FirstLine(itemRows) : itemRows);
            }

            return new GroupSection(group.Id, MeterCatalog.GetGroup(group.Id).Title, rows);
        }

        /// <summary>
        /// The thin bar keeps one line per card: the first row, or the whole strip when the item is
        /// drawn as a strip (the strip is one line of cells).
        /// </summary>
        private static IEnumerable<MeterRow> FirstLine(IReadOnlyList<MeterRow> rows)
        {
            return rows.Count > 0 && rows[0].Compact ? rows : rows.Take(1);
        }

        /// <summary>The visible items of a group; in the thin bar only the first of them.</summary>
        private static IEnumerable<ItemSetting> ShownItems(GroupSetting group, GadgetSettings settings)
        {
            IEnumerable<ItemSetting> visible = group.Items.Where(i => i.Visible);
            return settings.ThinBar ? visible.Take(1) : visible;
        }

        private IReadOnlyList<MeterRow> ItemRows(string itemId, GadgetSettings settings, Dictionary<string, object> snapshots)
        {
            ItemDefinition item = MeterCatalog.GetItem(itemId);
            if (snapshots.TryGetValue(item.SourceId, out object? snapshot))
            {
                return registry.Rows(itemId, snapshot, settings);
            }

            string reason = registry.UnavailableReason(item.SourceId)
                ?? throw new InvalidOperationException("Source '" + item.SourceId + "' was not read.");
            return new[] { new MeterRow(string.Empty, reason, 0.0, false) };
        }
    }
}
