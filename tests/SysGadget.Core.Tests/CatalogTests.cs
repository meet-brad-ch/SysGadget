using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SysGadget.Core;
using Xunit;

namespace SysGadget.Core.Tests
{
    public class CatalogTests
    {
        [Fact]
        public void Ids_AreUnique_AcrossGroupsAndItems()
        {
            List<string> ids = MeterCatalog.Groups.Select(g => g.Id)
                .Concat(MeterCatalog.Groups.SelectMany(g => g.Items).Select(i => i.Id))
                .ToList();

            Assert.Equal(ids.Count, ids.Distinct().Count());
        }

        [Fact]
        public void Accents_AreHexColours_AndDifferPerGroup()
        {
            Regex colour = new Regex("^#[0-9A-F]{6}$");

            Assert.All(MeterCatalog.Groups, g => Assert.Matches(colour, g.Accent));
            Assert.Equal(MeterCatalog.Groups.Count, MeterCatalog.Groups.Select(g => g.Accent).Distinct().Count());
        }

        [Fact]
        public void ItemSources_AreKnownSourceIds()
        {
            foreach (ItemDefinition item in MeterCatalog.Groups.SelectMany(g => g.Items))
            {
                Assert.Contains(item.SourceId, MeterCatalog.Sources.All);
            }
        }

        [Fact]
        public void Lookups_FindEntries_AndThrowOnUnknownIds()
        {
            Assert.Equal("Per core", MeterCatalog.GetItem(MeterCatalog.CpuCores).Title);
            Assert.Equal(MeterCatalog.MemoryGroup, MeterCatalog.GroupOf(MeterCatalog.Commit).Id);
            Assert.Equal("GPU", MeterCatalog.GetGroup(MeterCatalog.GpuGroup).Title);
            Assert.Throws<KeyNotFoundException>(() => MeterCatalog.GetItem("nope"));
            Assert.Throws<KeyNotFoundException>(() => MeterCatalog.GroupOf("nope"));
            Assert.Throws<KeyNotFoundException>(() => MeterCatalog.GetGroup("nope"));
        }
    }
}
