using System.IO;
using NUnit.Framework;
using StrangeToolkit.Repair;

namespace StrangeToolkit.Tests
{
    public class ToolkitFilesTests
    {
        [Test]
        public void Parse_ReadsTabSeparatedLines_AndSkipsBlankOrBroken()
        {
            var entries = ToolkitFiles.Parse("abc\tScripts/A.cs\r\n\nnotab\n\t\ndef\tEditor/Sub Folder/B.cs\n");
            Assert.AreEqual(2, entries.Count);
            Assert.AreEqual("abc", entries[0].Guid);
            Assert.AreEqual("Scripts/A.cs", entries[0].RelativePath);
            Assert.AreEqual("Editor/Sub Folder/B.cs", entries[1].RelativePath);
        }

        // Fails when a file was added, moved or re-IDed without running .github/scripts/make-file-list.sh
        [Test]
        public void FileList_MatchesTheFilesOnDisk()
        {
            var files = ToolkitFiles.Load();
            Assert.IsNotNull(files, "StrangeToolkitFiles.txt not found");
            foreach (var entry in files.Entries)
            {
                string physical = ToolkitFiles.PhysicalPath(files.AssetPath(entry));
                Assert.IsTrue(File.Exists(physical), $"Listed file is missing: {entry.RelativePath}");
                Assert.AreEqual(entry.Guid, ToolkitFiles.ReadMetaGuid(physical), $"GUID differs for {entry.RelativePath}");
            }
        }
    }
}
